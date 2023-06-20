using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tgs.MessageQueue;
using Tgs.Payment.Clients;
using Tgs.Payment.GCPay.Client;
using Tgs.Payment.Protos;

namespace Tgs.Payment.GCPay.MessageQueue;

internal class DepositTimeoutCheckHandler : IMessageHandler
{
    private readonly IPaymentServiceClient _paymentServiceClient;
    private readonly GCPayClient _gCPayClient;
    private readonly IPaymentClient _paymentClient;
    private readonly ILogger<DepositTimeoutCheckHandler> _logger;

    public DepositTimeoutCheckHandler(
        IPaymentServiceClient paymentServiceClient,
        GCPayClient gCPayClient,
        IPaymentClient paymentClient,
        ILogger<DepositTimeoutCheckHandler> logger)
    {
        _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
        _gCPayClient = gCPayClient ?? throw new ArgumentNullException(nameof(gCPayClient));
        _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = ScheduleRequest.Parser.ParseFrom(data.Span);

        _logger.LogInformation(
            "Deposit Schedule Handler Receive request Form Id:{request.FormId.ToGuid()}, "
                + "Schedule Time:{request.ScheduleTime.ToDateTime():yyyy/MM/dd HH:mm:ss}",
            request.FormId.ToGuid(),
            request.ScheduleTime.ToDateTime());

        var formId = request.FormId.ToGuid();

        try
        {
            var depositForm = await _paymentServiceClient
                .GetDepositFormAsync(formId, cancellationToken)
                .ConfigureAwait(false);

            if (depositForm is null)
            {
                _logger.LogWarning("Deposit Form {request.FormId.ToGuid()} not found.", request.FormId.ToGuid());
                return;
            }

            var brandId = depositForm.BrandId;

            _logger.LogInformation(
                "GetDepositFormAsync Form Id:{depositForm.Id}, Brand Id:{brandId}, Status:{depositForm.Status}",
                depositForm.Id,
                brandId,
                depositForm.Status);

            if (depositForm.Status >= PaymentFlowState.Reject)
                return;

            var applyInfo = JsonSerializer.Deserialize<GCPayDepositApplyInfo>(depositForm.ApplyInfo!, GCPayUtility.SerializerOptions)!;

            var result = await _gCPayClient.QueryDepositFormStatusAsync(
                applyInfo.Settings.CustomerId,
                applyInfo.Settings.Key,
                formId,
                cancellationToken).ConfigureAwait(false);

            var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

            if (result.Status is 3 or 4 or 5 or 6)
            {
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await _paymentClient
                        .RejectDepositAsync(formId, brandId, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            else if (result?.Status is not 2)
                await _paymentClient.ScheduleAsync(
                    "tgs.payment.gcpay.deposit.schedule.reject",
                    formId,
                    DateTime.UtcNow.AddMinutes(10D),
                    cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Process deposit timeout error!");

            await _paymentClient.ScheduleAsync(
                "tgs.payment.gcpay.deposit.schedule.reject",
                formId,
                DateTime.UtcNow.AddMinutes(10D),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
