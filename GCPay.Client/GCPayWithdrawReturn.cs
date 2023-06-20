namespace Tgs.Payment.GCPay.Client;
public record GCPayWithdrawReturn
{
    public int CustomerId { get; set; }
    public Guid FormId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public string Sign { get; set; } = default!;
    public string ThirdPartyOrderNo { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string TransactionMsg { get; set; } = default!;
}