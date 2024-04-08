using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.Clients;
using Tgs.Payment.GCPay;
using Tgs.Payment.GCPay.Client;
using Tgs.Payment.GCPay.MessageQueue;
using Tgs.Payment.Protos;

namespace GCPay.Core.UnitTests;

public class DepositTimeoutCheckHandlerTests
{
    [Fact]
    public async Task 檢查已經申請十五分鐘的單子是否已經完成()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://etpay888.com");

        var fakeGCPayClient = new GCPayClient(
            httpClient,
            new GCPayHasher(NullLogger<GCPayHasher>.Instance),
            NullLogger<GCPayClient>.Instance);

        var fakePaymentClient = Substitute.For<IPaymentClient>();

        var options = new GCPayOptions
        {
            CallbackBaseUrl = new Uri("https://dev-payment.rpgp.cc/gcpay"),
            PaymentApiUrl = new Uri("https://etpay888.com"),
            PaymentServiceBaseUrl = new Uri("http://localhost:5541"),
            PaymentCode = "GCPay"
        };

        var sut = new DepositTimeoutCheckHandler(
            fakePaymentServiceClient,
            fakeGCPayClient,
            fakePaymentClient,
            NullLogger<DepositTimeoutCheckHandler>.Instance,
            new OptionsWrapper<GCPayOptions>(options));

        var formId = Guid.Parse("997a590b2b3b4f2e937439f7efa32670");

        var request = new ScheduleRequest
        {
            Subject = "tgs.payment.gcpay.deposit.schedule.reject",
            FormId = formId,
            ScheduleTime = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        var applyInfo = new GCPayDepositApplyInfo(
            new GCPayDepositSettings(
                "52587",
                "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909",
                formId,
                "2643"),
            new GCPayDepositResponse(
                10000M,
                request.FormId.ToGuid(),
                new Uri("http://abc.cc"),
                "T06201327270531929670"));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(request.FormId.ToGuid()))
            .Returns(new DepositFormInfo
            {
                Id = request.FormId.ToGuid(),
                Amount = 1000M,
                BrandId = Guid.NewGuid(),
                ApplyInfo = JsonSerializer.Serialize(applyInfo, GCPayUtility.SerializerOptions)
            });

        _ = mockHttp.When("https://etpay888.com/api/query_transaction")
            .Respond(new StringContent("""
				{
				    "code": 0,
				    "message": "success",
				    "data": {
				        "customer_id": 52587,
				        "order_id": "997a590b2b3b4f2e937439f7efa32670",
				        "transaction_id": "T06201327270531929670",
				        "status": 3,
				        "order_amount": "10000.00000000",
				        "real_amount": null,
				        "created": "2023-06-20 13:27:27",
				        "expired": "2023-06-20 13:37:27",
				        "notify_url": "https:\/\/dev-payment.rpgp.cc\/gcpay\/deposit\/callback",
				        "customer_callback": null,
				        "extra": {
				            "back_url": null,
				            "model_id": 2614,
				            "client_ip": "114.33.41.237",
				            "new_cm_id": 2614,
				            "old_cm_id": 2643,
				            "user_name": "",
				            "pay_product_name": null
				        },
				        "rc_feedback": {
				            "rate": null,
				            "display_price": null
				        }
				    }
				}
				"""));

        // Act
        await sut.HandleAsync(
            "tgs.payment.gcpay.deposit.schedule.reject",
            request,
            null);

        // Assert
        _ = fakePaymentClient.Received(1)
            .RejectDepositAsync(
                Arg.Is(request.FormId.ToGuid()),
                Arg.Any<Guid>());
    }
}
