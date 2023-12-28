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

public class WithdrawProcessorTests
{
    [Fact]
    public async Task 處理提款要求_銀行()
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

        var sut = new WithdrawProcessor(
            fakeGCPayClient,
            fakePaymentClient,
            new OptionsWrapper<GCPayOptions>(options));

        var applySettings = new GCPayWithdrawSettings(
            "52587",
            "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909",
            Guid.Parse("ab58787d44d143ca9dd2453f1bc8741e"),
            BankName: "ANZBP",
            AccountName: "john",
            CardNo: "43253432342342342");

        var request = new WithdrawRequest
        {
            Amount = "100",
            ApplyInfo = JsonSerializer.Serialize(applySettings, GCPayUtility.SerializerOptions)
        };

        _ = mockHttp.When("https://etpay888.com/api/payments/pay_order")
            .Respond(new StringContent("""
				{
				    "code": 0,
				    "message": "success",
				    "data": {
				        "transaction_id": "P0619175112961319341e",
				        "amount": "100.0000"
				    }
				}
				"""));

        // Act
        var actual = await sut.HandleAsync(request.ToByteArray());
        var response = WithdrawResponse.Parser.ParseFrom(actual.Span);
        var applyInfo = JsonSerializer.Deserialize<GCPayWithdrawApplyInfo>(response.Result, GCPayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .SaveWithdrawFormThirdPartyOrderNoAsync(
                Arg.Is(applySettings.FormId),
                Arg.Is("P0619175112961319341e"));
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
    }

    [Fact]
    public async Task 處理提款要求_GCash()
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

        var sut = new WithdrawProcessor(
            fakeGCPayClient,
            fakePaymentClient,
            new OptionsWrapper<GCPayOptions>(options));

        var applySettings = new GCPayWithdrawSettings(
            "52587",
            "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909",
            Guid.Parse("ab18ba9fac1b44b9af34e58e02df3c2c"),
            BankName: "GCASH",
            AccountName: "john",
            CardNo: "9078533090");

        var request = new WithdrawRequest
        {
            Amount = "100",
            ApplyInfo = JsonSerializer.Serialize(applySettings, GCPayUtility.SerializerOptions)
        };

        _ = mockHttp.When("https://etpay888.com/api/payments/pay_order")
            .Respond(new StringContent("""
				{
				    "code": 0,
				    "message": "success",
				    "data": {
				        "transaction_id": "P06201502505676501c2c",
				        "amount": "100.0000"
				    }
				}
				"""));

        // Act
        var actual = await sut.HandleAsync(request.ToByteArray());
        var response = WithdrawResponse.Parser.ParseFrom(actual.Span);
        var applyInfo = JsonSerializer.Deserialize<GCPayWithdrawApplyInfo>(response.Result, GCPayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .SaveWithdrawFormThirdPartyOrderNoAsync(
                Arg.Is(applySettings.FormId),
                Arg.Is("P06201502505676501c2c"));
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
    }

    [Fact]
    public async Task 處理提款要求_PayMaya()
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

        var sut = new WithdrawProcessor(
            fakeGCPayClient,
            fakePaymentClient,
            new OptionsWrapper<GCPayOptions>(options));

        var applySettings = new GCPayWithdrawSettings(
            "52587",
            "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909",
            Guid.Parse("0b6de67394f24f518e83cb3e55f7bad8"),
            BankName: "PAYMP",
            AccountName: "john",
            CardNo: "9078533090");

        var request = new WithdrawRequest
        {
            Amount = "100",
            ApplyInfo = JsonSerializer.Serialize(applySettings, GCPayUtility.SerializerOptions)
        };

        _ = mockHttp.When("https://etpay888.com/api/payments/pay_order")
            .Respond(new StringContent("""
				{
				    "code": 0,
				    "message": "success",
				    "data": {
				        "transaction_id": "P06201506293584383ad8",
				        "amount": "100.0000"
				    }
				}
				"""));

        // Act
        var actual = await sut.HandleAsync(request.ToByteArray());
        var response = WithdrawResponse.Parser.ParseFrom(actual.Span);
        var applyInfo = JsonSerializer.Deserialize<GCPayWithdrawApplyInfo>(response.Result, GCPayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .SaveWithdrawFormThirdPartyOrderNoAsync(
                Arg.Is(applySettings.FormId),
                Arg.Is("P06201506293584383ad8"));
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
    }
}
