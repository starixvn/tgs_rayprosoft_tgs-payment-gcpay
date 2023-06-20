namespace Tgs.Payment.GCPay.Client;
public record GCPayDepositReturn
{
    public int CustomerId { get; init; } = default!;
    public string Status { get; init; } = default!;
    public Guid FormId { get; init; } = default!;
    public string ThirdPartyOrderNo { get; init; } = default!;
    public decimal OrderAmount { get; set; }
    public decimal RealAmount { get; init; }
    public string Sign { get; init; } = default!;
    public string Message { get; set; } = default!;
}
