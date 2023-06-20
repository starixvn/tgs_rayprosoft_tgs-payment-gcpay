using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.Payment.GCPay.ApplicationServices;
using Tgs.Payment.GCPay.Client;
using Tgs.Payment.GCPay.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Payment.GCPay.Controllers;

[Route("[controller]")]
[ApiController]
public class WithdrawController : ControllerBase
{
    private readonly ILogger<WithdrawController> _logger;

    public WithdrawController(ILogger<WithdrawController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("callback", Name = "NotifyWithdraw")]
    public async Task<string> WithdrawCallbackAsync(
        [FromServices] GCPayHasher hasher,
        [FromServices] IGetService<GetWithdrawFormQuery, WithdrawForm?> getService,
        [FromServices] IPaymentServiceClient paymentServiceClient,
        [FromServices] IPaymentClient paymentClient,
        [FromBody] CallbackWithdrawViewModel source)
    //[FromBody] object source)
    {
        var formId = Guid.Parse(source.OrderId);

        var withdrawForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (withdrawForm == null)
        {
            _logger.LogWarning("Can't found withdraw form({id}).", formId);
            return "FAIL";
        }

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(source))!;

        var sign = source.Sign;
        var hash = hasher.ComputeHash(
            withdrawForm.ApplyInfo!.Settings.Key,
            dict.Select(kv => new KeyValuePair<string, string?>(
                kv.Key,
                kv.Value!.ToString())));

        _logger.LogInformation("Compute hash: {hash}, sign: {sign}", hash, sign);

        if (sign != hash)
        {
            _logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", sign, hash);
            return "FAIL";
        }

        var paymentReturn = new GCPayWithdrawReturn()
        {
            CustomerId = source.CustomerId,
            FormId = formId,
            Amount = decimal.Parse(source.Amount),
            DateTime = source.DateTime,
            Sign = sign,
            ThirdPartyOrderNo = source.TransactionId,
            Status = source.TransactionCode,
            TransactionMsg = source.TransactionMsg
        };

        var fsm = PaymentStateMachineFactory.CreateWithdrawStateMachine(withdrawForm.Status);

        switch (paymentReturn.Status)
        {
            case "30000":
                if (fsm.CanFire(PaymentTrigger.PaymentWithdrawCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentWithdrawCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            GCPayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.PaidWithdrawAsync(
                        withdrawForm.Id,
                        withdrawForm.BrandId).ConfigureAwait(false);
                }

                break;

            case "40000":
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            GCPayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.RejectWithdrawAsync(
                        withdrawForm.Id,
                        withdrawForm.BrandId,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;
        }

        return "OK";
    }
}
