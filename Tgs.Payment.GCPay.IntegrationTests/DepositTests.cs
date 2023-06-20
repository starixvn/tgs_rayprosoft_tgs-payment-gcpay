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

public class DepositTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DepositTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }

    [Fact]
    public async Task 代收回調_訂單狀態成功()
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

        var formId = Guid.Parse("58270ddb0fcd4fdcb355bbd71ff3167d");
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";

        var request = """
            {
                "customer_id": 52587,
                "order_id": "58270ddb0fcd4fdcb355bbd71ff3167d",
                "transaction_id": "T0619165455174136567d",
                "order_amount": 100,
                "real_amount": 100,
                "status": "30000",
                "message": "\u652f\u4ed8\u6210\u529f",
                "sign": "1AB26E12AD1CD5430EFED2BEB64C2B3B"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var applyInfo = new GCPayDepositApplyInfo(
            new GCPayDepositSettings(
                "52587",
                key,
                formId,
               "2643"),//PayMaya 2643
             new GCPayDepositResponse(
                100M,
                formId,
                new Uri("https://gcpay.galaxyweb.life/api/payment_direct/redirect/T0619165455174136567d"),
                "T0619165455174136567d"));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formId), Arg.Any<CancellationToken>())
            .Returns(new DepositFormInfo
            {
                Id = formId,
                BrandId = Guid.NewGuid(),
                Status = PaymentFlowState.WaitPayment,
                Amount = 100M,
                ThirdPartyOrderNo = "T0619165455174136567d",
                ApplyInfo = JsonSerializer.Serialize(applyInfo, GCPayUtility.SerializerOptions)
            });

        var response = await httpClient.PostAsync("/deposit/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("OK", actual);

        _ = fakePaymentServiceClient.Received(1)
            .SubmitDepositFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .ApproveDepositAsync(
                Arg.Is(formId),
                Arg.Any<Guid>(),
                Arg.Is(applyInfo.Response.Amount),
                Arg.Any<CancellationToken>());
    }
}
