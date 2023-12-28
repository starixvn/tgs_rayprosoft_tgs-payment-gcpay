using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Tgs.Payment.GCPay;

public class GCPayClient
{
    private readonly HttpClient _httpClient;
    private readonly GCPayHasher _hasher;
    private readonly ILogger<GCPayClient> _logger;

    public GCPayClient(
        HttpClient httpClient,
        GCPayHasher hasher,
        ILogger<GCPayClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<GCPayClientDepositResponse> DepositAsync(string customerId, string key, GCPayClientDepositOrder order, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string?>
        {
            ["pay_customer_id"] = customerId,
            ["pay_apply_date"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ["pay_order_id"] = order.FormId.ToString("N"),
            ["pay_notify_url"] = order.CallbackUrl.AbsoluteUri,
            ["pay_amount"] = order.Amount.ToString("#.##"),
            ["pay_channel_id"] = order.PayChannelId
        };

        var sign = _hasher.ComputeHash(key, dict);

        dict["pay_md5_sign"] = sign;

        var response = await _httpClient
         .PostAsJsonAsync("/api/pay_order", dict, cancellationToken)
         .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("PayClient Deposit Body:{body}", body);

        var node = JsonNode.Parse(body);
        if (node is null)
            throw new ApiFailException("Deposit response can't read.");

        if (node["code"]!.GetValue<int>() != 0)
            throw new ApiFailException(node["message"]!.ToString());

        var data = node["data"]!;

        return new GCPayClientDepositResponse(
            Amount: order.Amount,
            FormId: Guid.Parse(data["order_id"]!.ToString()),
            ThirdPartyOrderNo: data["transaction_id"]!.ToString(),
            RedirectUrl: new Uri(data["view_url"]!.ToString()),
            QrCodeUrl: new Uri(data["qr_url"]!.ToString()));
    }

    public async ValueTask<GCPayClientWithdrawResponse> WithdrawAsync(
        string customerId,
        string key,
        GCPayClientWithdrawOrder order,
        CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string?>
        {
            ["pay_customer_id"] = customerId,
            ["pay_apply_date"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ["pay_order_id"] = order.FormId.ToString("N"),
            ["pay_notify_url"] = order.CallbackUrl.ToString(),
            ["pay_amount"] = (order.Amount).ToString("#.##"),
            ["pay_bank_name"] = order.BankName,
            ["pay_account_name"] = order.AccountName,
            ["pay_card_no"] = order.CardNo,
        };
        var sign = _hasher.ComputeHash(key, dict);

        dict["pay_md5_sign"] = sign;

        var response = await _httpClient
            .PostAsJsonAsync("/api/payments/pay_order", dict, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("PayClient Withdraw Response Body:{body}", body);

        var node = JsonNode.Parse(body);

        if (node is null)
            throw new ApiFailException("Withdraw response can't read.");

        if (node["code"]!.GetValue<int>() != 0)
            throw new ApiFailException(node["message"]!.ToString());

        var data = node["data"]!;

        return new GCPayClientWithdrawResponse(
            order.FormId,
            ThirdPartyOrderNo: data["transaction_id"]!.ToString(),
            Amount: decimal.Parse(data["amount"]!.ToString()));
    }

    public async ValueTask<GCPayClientQueryDepositResponse> QueryDepositFormStatusAsync(string customerId, string key, Guid formId, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string?>
        {
            ["pay_customer_id"] = customerId,
            ["pay_apply_date"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ["pay_order_id"] = formId.ToString("N"),
        };

        var sign = _hasher.ComputeHash(key, dict);

        dict["pay_md5_sign"] = sign;

        var response = await _httpClient
         .PostAsJsonAsync("/api/query_transaction", dict, cancellationToken)
         .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("PayClient Query Deposit Form Body:{body}", body);

        var node = JsonNode.Parse(body);
        if (node is null)
            throw new ApiFailException("Deposit response can't read.");

        if (node["code"]!.GetValue<int>() != 0)
            throw new ApiFailException(node["message"]!.ToString());

        var data = node["data"]!;

        return new GCPayClientQueryDepositResponse(
            Status: data["status"]!.GetValue<int>());
    }
}
