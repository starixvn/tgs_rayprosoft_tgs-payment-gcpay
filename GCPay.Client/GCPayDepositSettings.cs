namespace Tgs.Payment.GCPay.Client;
public record GCPayDepositSettings(
    string CustomerId,
    string Key,
    Guid FormId,
    string PayChannelId);