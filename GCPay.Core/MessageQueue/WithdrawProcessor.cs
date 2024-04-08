using System.Text.Json;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.GCPay.Client;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.GCPay.MessageQueue;

internal class WithdrawProcessor(
    GCPayClient gCPayClient,
    IPaymentClient paymentClient,
    IOptions<GCPayOptions> optionsAccessor) : IMessageProcessor<WithdrawRequest, WithdrawResponse>
{
    private readonly GCPayClient _gCPayClient = gCPayClient ?? throw new ArgumentNullException(nameof(gCPayClient));
    private readonly IPaymentClient _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
    private readonly GCPayOptions _options = optionsAccessor.Value;

    public async ValueTask<WithdrawResponse> HandleAsync(
        string subject,
        WithdrawRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<GCPayWithdrawSettings>(data.ApplyInfo, GCPayUtility.SerializerOptions)!;

        var order = new GCPayClientWithdrawOrder(
            decimal.Parse(data.Amount),
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
            withdrawResponse.ThirdPartyOrderNo,
            cancellationToken);

        return new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, GCPayUtility.SerializerOptions)
        };
    }
}
