using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Tgs.Payment.GCPay;
using Tgs.Payment.GCPay.MessageQueue;
using Tgs.Payment.Protos;

namespace GCPay.Core.UnitTests;
public class GetBalanceProcessorTests
{
    [Fact]
    public async Task GetBalanceProcessor_取得商戶餘額()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://apii.dkk888.com/api/payments/balance");

        var fakeGCClient = new GCPayClient(
            httpClient,
            new GCPayHasher(NullLogger<GCPayHasher>.Instance),
            NullLogger<GCPayClient>.Instance);

        var sut = new GetBalanceProcessor(fakeGCClient);
        

        var request = new PaymentBalanceRequest
        {
            BrandPaymentSettings = """
                {
                    "customerId": "54579",
                    "key": "ffb6c6bb7b987417c47b37efdfcb240550f3d2e5a3772b4f57bcb8d2d0061312"
                }
            """
        };

        _ = mockHttp.Expect(HttpMethod.Post, "https://apii.dkk888.com/api/payments/balance")            
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "code": 0,
                    "message": "success",
                    "data": {
                        "balance": "0.0000",
                        "balance_original": "0.0000"
                    }
                }
                """));

        // Act
        var actual = await sut.HandleAsync(
            "tgs.payment.gcpay.balance",
            request,
            null);

        // Assert
        Assert.Equal(0M, actual.Balance.ToDecimal());
    }
}
