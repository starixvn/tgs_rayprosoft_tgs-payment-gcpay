using Tgs.Payment.Clients;
using Tgs.Payment.GCPay.Client;

namespace Tgs.Payment.GCPay;
public record DepositForm(
    Guid Id,
    string PlayerCode,
    Guid BrandId,
    Guid BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    GCPayDepositApplyInfo? ApplyInfo,
    string? PaymentReturn = null);