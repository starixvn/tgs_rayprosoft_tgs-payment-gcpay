namespace Tgs.Payment.GCPay;
public record GCPayClientDepositResponse(
    decimal Amount,
    Guid FormId,
    string ThirdPartyOrderNo,
    Uri RedirectUrl,
    Uri QrCodeUrl);