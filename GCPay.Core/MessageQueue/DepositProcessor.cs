using System.Text.Json;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.GCPay.Client;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.GCPay.MessageQueue;

internal class DepositProcessor(
    GCPayClient gCPayClient,
    IPaymentClient paymentClient,
    IOptions<GCPayOptions> optionsAccessor)
    : IMessageProcessor<DepositRequest, DepositResponse>
{
    private readonly GCPayClient _gCPayClient = gCPayClient ?? throw new ArgumentNullException(nameof(gCPayClient));
    private readonly IPaymentClient _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
    private readonly GCPayOptions _options = optionsAccessor.Value;

    public async ValueTask<DepositResponse> HandleAsync(
        string subject,
        DepositRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<GCPayDepositSettings>(
            data.ApplyInfo,
            GCPayUtility.SerializerOptions)!;

        var order = new GCPayClientDepositOrder(
            decimal.Parse(data.Amount),
            settings.FormId,
            new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
            settings.PayChannelId);

        var depositResponse = await _gCPayClient.DepositAsync(
            settings.CustomerId,
            settings.Key,
            order,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new GCPayDepositApplyInfo(
            settings,
            new GCPayDepositResponse(
                depositResponse.Amount,
                depositResponse.FormId,
                depositResponse.RedirectUrl,
                depositResponse.ThirdPartyOrderNo));

        await _paymentClient.ScheduleAsync(
            $"tgs.payment.{_options.PaymentCode.ToLower()}.deposit.schedule.reject",
            settings.FormId,
            DateTime.UtcNow.AddMinutes(31D),
            cancellationToken).ConfigureAwait(false);

        return new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, GCPayUtility.SerializerOptions)
        };
    }
}
