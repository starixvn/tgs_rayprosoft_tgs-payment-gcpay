using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.GCPay.Client;
using Xunit.Abstractions;

namespace Tgs.Payment.GCPay.IntegrationTests;

public class WithdrawTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public WithdrawTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }

    [Fact]
    public async Task 代付回調_訂單狀態成功()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = GCPayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();

        var formId = Guid.Parse("ab58787d44d143ca9dd2453f1bc8741e");
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";

        var settings = new GCPayWithdrawSettings(
            "52587",
            key,
            formId,
            "ANZBP",
            "john",
            "43253432342342342");

        var withdrawResponse = new GCPayWithdrawResponse(
            formId);

        var applyInfo = new GCPayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = Guid.NewGuid(),
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, GCPayUtility.SerializerOptions)
            });

        var request = """
            {
                "customer_id": 52587,
                "order_id": "ab58787d44d143ca9dd2453f1bc8741e",
                "amount": "100.0000",
                "datetime": "2023-06-19 17:51:12",
                "transaction_id": "P0619175112961319341e",
                "transaction_code": "30000",
                "transaction_msg": "\u652f\u4ed8\u6210\u529f",
                "sign": "AB3DB06851899C79147FF2603FFC4374"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("OK", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .PaidWithdrawAsync(
                Arg.Is(formId),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task 代付回調_訂單狀態駁回()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = GCPayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Text.Plain));

        var formId = Guid.Parse("ab58787d44d143ca9dd2453f1bc8741e");
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";

        var settings = new GCPayWithdrawSettings(
            "2081536946682450",
            key,
            formId,
            "ANZBP",
            "john",
            "43253432342342342");

        var withdrawResponse = new GCPayWithdrawResponse(
            formId);

        var applyInfo = new GCPayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = Guid.NewGuid(),
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, GCPayUtility.SerializerOptions)
            });

        var request = """
            {
                "customer_id": 52587,
                "order_id": "ab58787d44d143ca9dd2453f1bc8741e",
                "amount": "100.0000",
                "datetime": "2023-06-19 17:51:12",
                "transaction_id": "P0619175112961319341e",
                "transaction_code": "40000",
                "transaction_msg": "\u652f\u4ed8\u9a73\u56de",
                "sign": "56AF26B0447455F03CD76680324E46F0"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("OK", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .RejectWithdrawAsync(
                Arg.Is(formId),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }
}
