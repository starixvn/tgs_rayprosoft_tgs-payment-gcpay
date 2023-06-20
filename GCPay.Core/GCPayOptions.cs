namespace Tgs.Payment.GCPay;

public class GCPayOptions
{
	public required Uri CallbackBaseUrl { get; set; }

	public required Uri PaymentApiUrl { get; set; }

	public required Uri PaymentServiceBaseUrl { get; set; }
}