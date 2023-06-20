namespace Tgs.Payment.GCPay.Client;

public record GCPayDepositApplyInfo(
    GCPayDepositSettings Settings,
    GCPayDepositResponse Response);