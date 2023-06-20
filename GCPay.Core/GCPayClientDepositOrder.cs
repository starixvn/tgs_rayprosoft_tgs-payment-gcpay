namespace Tgs.Payment.GCPay;
public record GCPayClientDepositOrder(
    decimal Amount,
    Guid FormId,
    Uri CallbackUrl,
    string PayChannelId);