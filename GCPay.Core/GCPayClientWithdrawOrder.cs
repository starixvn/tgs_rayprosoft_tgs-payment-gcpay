namespace Tgs.Payment.GCPay;
public record GCPayClientWithdrawOrder(
    decimal Amount,
    Guid FormId,
    string BankName,
    string AccountName,
    string CardNo,
    Uri CallbackUrl);