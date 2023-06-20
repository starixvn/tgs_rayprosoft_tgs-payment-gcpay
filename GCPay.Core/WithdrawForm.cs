using Tgs.Payment.GCPay.Client;
using Tgs.Payment.Clients;

namespace Tgs.Payment.GCPay;
public record WithdrawForm(
	Guid Id,
	string? UserId,
	UserType UserType,
	Guid BrandId,
	Guid? BrandPaymentId,
	decimal Amount,
	PaymentFlowState Status,
	Guid BankInfoId,
	GCPayWithdrawApplyInfo? ApplyInfo,
	string? PaymentReturn = null);