using System.Text.Json;
using Google.Protobuf;
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

public class DepositProcessorTests
{
    [Fact]
    public async Task 儲值要求處理_Paymaya()
    {
        // Arrange
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

        var sut = new DepositProcessor(
            fakeGCPayClient,
            fakePaymentClient,
            new OptionsWrapper<GCPayOptions>(options));

        var applySettings = new GCPayDepositSettings(
            "52587",
            "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909",
            Guid.Parse("9fe8d448bdba4ad3b55e820433371e19"),
            "2643");//Paymaya PayChannelId 2643

        var request = new DepositRequest
        {
            Amount = "10000",
            ApplyInfo = JsonSerializer.Serialize(applySettings, GCPayUtility.SerializerOptions)
        };

        _ = mockHttp.When("https://etpay888.com/api/pay_order")
            .Respond(new StringContent("""
				{
				    "code": 0,
				    "message": "success",
				    "data": {
				        "order_id": "9fe8d448bdba4ad3b55e820433371e19",
				        "transaction_id": "T06201148344273145e19",
				        "view_url": "https:\/\/gcpay.galaxyweb.life\/api\/payment_direct\/redirect\/T06201148344273145e19",
				        "qr_url": "https:\/\/api.qrserver.com\/v1\/create-qr-code\/?data=https:\/\/gcpay.galaxyweb.life\/api\/payment_direct\/redirect\/T06201148344273145e19&size=200x200&ecc=M",
				        "expired": "2023-06-20 11:58:34",
				        "user_name": "",
				        "rate": null
				    }
				}
				"""));

        // Act
        var actual = await sut.HandleAsync(request.ToByteArray());
        var response = DepositResponse.Parser.ParseFrom(actual.Span);
        var applyInfo = JsonSerializer.Deserialize<GCPayDepositApplyInfo>(response.Result, GCPayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .ScheduleAsync(
                Arg.Is("tgs.payment.gcpay.deposit.schedule.reject"),
                Arg.Is(applySettings.FormId),
                Arg.Any<DateTime>());
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
        Assert.Equal(
            new GCPayDepositResponse(
                10000M,
                applySettings.FormId,
                new Uri("https://gcpay.galaxyweb.life/api/payment_direct/redirect/T06201148344273145e19"),
                "T06201148344273145e19"),
            applyInfo.Response);
    }
}
