using System.Text.Json.Serialization;

namespace Tgs.Payment.GCPay.ViewModels;

public class CallbackDepositViewModel
{
    [JsonPropertyName("customer_id")]
    public int CustomerId { get; set; }

    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = default!;

    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; } = default!;

    [JsonPropertyName("order_amount")]
    public decimal OrderAmount { get; set; }

    [JsonPropertyName("real_amount")]
    public decimal RealAmount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("message")]
    public string Message { get; set; } = default!;

    [JsonPropertyName("sign")]
    public string Sign { get; set; } = default!;
}
