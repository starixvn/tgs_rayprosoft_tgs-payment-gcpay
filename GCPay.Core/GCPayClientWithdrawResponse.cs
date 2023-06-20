namespace Tgs.Payment.GCPay;
public record GCPayClientWithdrawResponse(
    Guid FormId,
    string ThirdPartyOrderNo,
    decimal Amount);
