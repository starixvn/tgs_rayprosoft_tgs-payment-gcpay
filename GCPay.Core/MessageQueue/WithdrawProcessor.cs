using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.MessageQueue;
using Tgs.Payment.Clients;
using Tgs.Payment.GCPay.Client;
using Tgs.Payment.Protos;

namespace Tgs.Payment.GCPay.MessageQueue;

internal class WithdrawProcessor : IMessageProcessor
{
    private readonly GCPayClient _gCPayClient;
    private readonly IPaymentClient _paymentClient;
    private readonly GCPayOptions _options;

    public WithdrawProcessor(
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
        var request = WithdrawRequest.Parser.ParseFrom(data.Span);
        var settings = JsonSerializer.Deserialize<GCPayWithdrawSettings>(request.ApplyInfo, GCPayUtility.SerializerOptions)!;

        var order = new GCPayClientWithdrawOrder(
            decimal.Parse(request.Amount),
            settings.FormId,
            settings.BankName,
            settings.AccountName,
            settings.CardNo,
            new Uri($"{_options.CallbackBaseUrl}/withdraw/callback"));

        var withdrawResponse = await _gCPayClient.WithdrawAsync(
            settings.CustomerId,
            settings.Key,
            order,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new GCPayWithdrawApplyInfo(
            settings,
            new GCPayWithdrawResponse(withdrawResponse.FormId));

        await _paymentClient.SaveWithdrawFormThirdPartyOrderNoAsync(
            settings.FormId,
            settings.FormId.ToString(),
            cancellationToken);

        var msg = new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, GCPayUtility.SerializerOptions)
        };

        return msg.ToByteArray();
    }
}