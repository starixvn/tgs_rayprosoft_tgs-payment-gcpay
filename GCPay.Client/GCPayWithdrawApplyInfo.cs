namespace Tgs.Payment.GCPay.Client;
public record GCPayWithdrawApplyInfo(
	GCPayWithdrawSettings Settings,
	GCPayWithdrawResponse Response);