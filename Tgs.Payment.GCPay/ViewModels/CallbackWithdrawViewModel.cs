using System.Globalization;
using System.Text.Json.Serialization;

namespace Tgs.Payment.GCPay.ViewModels;

public class CallbackWithdrawViewModel
{
    [JsonPropertyName("customer_id")]
    public int CustomerId { get; set; }

    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = default!;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = default!;

    [JsonPropertyName("datetime")]
    public string Datetime { get; init; } = string.Empty;

    [JsonIgnore]
    public DateTime DateTime => DateTime.ParseExact(Datetime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; } = default!;

    [JsonPropertyName("transaction_code")]
    public string TransactionCode { get; set; } = default!;

    [JsonPropertyName("transaction_msg")]
    public string TransactionMsg { get; set; } = default!;

    [JsonPropertyName("sign")]
    public string Sign { get; set; } = default!;
}
