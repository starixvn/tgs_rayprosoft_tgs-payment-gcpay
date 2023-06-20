namespace Tgs.Payment.GCPay.Client;
public record GCPayWithdrawSettings(
    string CustomerId,
    string Key,
    Guid FormId,
    string BankName,
    string AccountName,
    string CardNo);