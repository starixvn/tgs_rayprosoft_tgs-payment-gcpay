using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.MessageQueue;
using Tgs.Payment.Clients;
using Tgs.Payment.GCPay.Client;
using Tgs.Payment.Protos;

namespace Tgs.Payment.GCPay.MessageQueue;

internal class DepositProcessor : IMessageProcessor
{
    private readonly GCPayClient _gCPayClient;
    private readonly IPaymentClient _paymentClient;
    private readonly GCPayOptions _options;

    public DepositProcessor(
        GCPayClient gCPayClient,
        IPaymentClient paymentClient,
        IOptions<GCPayOptions> optionsAccessor)
    {
        _gCPayClient = gCPayClient ?? throw new ArgumentNullException(nameof(gCPayClient));
        _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
        _options = optionsAccessor.Value;
    }

    public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = DepositRequest.Parser.ParseFrom(data.Span);

        var settings = JsonSerializer.Deserialize<GCPayDepositSettings>(
            request.ApplyInfo,
            GCPayUtility.SerializerOptions)!;

        var order = new GCPayClientDepositOrder(
            decimal.Parse(request.Amount),
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

        var msg = new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, GCPayUtility.SerializerOptions)
        };

        return msg.ToByteArray();
    }
}
