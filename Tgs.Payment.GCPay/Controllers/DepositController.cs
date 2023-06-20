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
public class DepositController : ControllerBase
{
    private readonly ILogger<DepositController> _logger;

    public DepositController(ILogger<DepositController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("callback", Name = "NotifyDeposit")]
    public async Task<string> DepositCallbackAsync(
        [FromServices] GCPayHasher hasher,
        [FromServices] IGetService<GetDepositFormQuery, DepositForm?> getService,
        [FromServices] IPaymentServiceClient paymentServiceClient,
        [FromServices] IPaymentClient paymentClient,
        [FromBody] CallbackDepositViewModel source)
    {
        var formId = Guid.Parse(source.OrderId);

        var depositForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (depositForm == null)
        {
            _logger.LogWarning("Can't found deposit form({id}).", formId);
            return "FAIL";
        }

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(source))!;

        var sign = source.Sign;
        var hash = hasher.ComputeHash(
            depositForm.ApplyInfo!.Settings.Key,
            dict.Select(kv => new KeyValuePair<string, string?>(
                kv.Key,
                kv.Value!.ToString())));

        _logger.LogInformation("Compute hash: {hash}, sign: {sign}", hash, sign);

        if (sign != hash)
        {
            _logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", sign, hash);
            return "FAIL";
        }

        var paymentReturn = new GCPayDepositReturn()
        {
            CustomerId = source.CustomerId,
            Status = source.Status,
            FormId = formId,
            ThirdPartyOrderNo = source.TransactionId,
            RealAmount = source.RealAmount,
            OrderAmount = source.OrderAmount,
            Sign = sign,
            Message = source.Message,
        };

        var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

        switch (paymentReturn.Status)
        {
            case "30000":
                if (fsm.CanFire(PaymentTrigger.PaymentDepositCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentDepositCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            GCPayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.ApproveDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        paymentReturn.RealAmount,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;

            case "50000":
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            GCPayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.RejectDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;
        }

        return "OK";
    }
}
