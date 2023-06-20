namespace Tgs.Payment.GCPay.Client;
public record GCPayDepositResponse(
    decimal Amount,
    Guid FormId,
    Uri RedirectUrl,
    string ThirdPartyOrderNo);