using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Tgs.Payment.GCPay;

namespace GCPay.Core.UnitTests;

public class GCPayClientTests
{
    [Fact]
    public async Task 存款_Paymaya_成功()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://etpay888.com");

        var signer = new GCPayHasher(NullLogger<GCPayHasher>.Instance);

        var sut = new GCPayClient(httpClient, signer, NullLogger<GCPayClient>.Instance);

        var customerId = "52587";
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";
        var formId = Guid.Parse("58270ddb0fcd4fdcb355bbd71ff3167d");

        _ = mockHttp.Expect(HttpMethod.Post, "https://etpay888.com/api/pay_order")
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "code": 0,
                    "message": "success",
                    "data": {
                        "order_id": "58270ddb0fcd4fdcb355bbd71ff3167d",
                        "transaction_id": "T0619165455174136567d",
                        "view_url": "https://gcpay.galaxyweb.life/api/payment_direct/redirect/T0619165455174136567d",
                        "qr_url": "https://api.qrserver.com/v1/create-qr-code/?data=https://gcpay.galaxyweb.life/api/payment_direct/redirect/T0619165455174136567d&size=200x200&ecc=M",
                        "expired": "2023-06-19 17:04:55",
                        "user_name": "",
                        "rate": null
                    }
                }
                """));

        var order = new GCPayClientDepositOrder(
            100M,
            formId,
            new Uri("https://dev-payment.rpgp.cc/gcpay/deposit/callback"),
            "2643");//Paymaya 2643

        var actual = await sut.DepositAsync(customerId, key, order);

        Assert.Equal(formId, actual.FormId);
        Assert.Equal("T0619165455174136567d", actual.ThirdPartyOrderNo);
    }

    [Fact]
    public void 存款_GCash_三方獲取支付渠道失敗()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var signer = new GCPayHasher(NullLogger<GCPayHasher>.Instance);

        var sut = new GCPayClient(httpClient, signer, NullLogger<GCPayClient>.Instance);

        var customerId = "52587";
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";
        var formId = Guid.Parse("f2f8ad10b3544b2cb0a801a7504ae11b");

        _ = mockHttp.Expect(HttpMethod.Post, "https://etpay888.com/api/pay_order")
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "code":20004,
                    "message":"获取支付渠道失败1"
                }
                """));

        var order = new GCPayClientDepositOrder(
            100M,
            formId,
            new Uri("https://dev-payment.rpgp.cc/gcpay/deposit/callback"),
            "1302");//GCash 1302

        _ = Assert.ThrowsAsync<ApiFailException>(async () => await sut.DepositAsync(customerId, key, order));
    }

    [Fact]
    public async Task 存款_訂單查詢_成功_狀態3_失敗_逾期失效()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://etpay888.com");

        var signer = new GCPayHasher(NullLogger<GCPayHasher>.Instance);

        var sut = new GCPayClient(httpClient, signer, NullLogger<GCPayClient>.Instance);

        var customerId = "52587";
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";
        var formId = Guid.Parse("621acca4d8aa41109a073e2c71cdf3b1");

        _ = mockHttp.Expect(HttpMethod.Post, "https://etpay888.com/api/query_transaction")
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "code": 0,
                    "message": "success",
                    "data": {
                        "customer_id": 52587,
                        "order_id": "621acca4d8aa41109a073e2c71cdf3b1",
                        "transaction_id": "T062012010335592953b1",
                        "status": 3,
                        "order_amount": "10000.00000000",
                        "real_amount": null,
                        "created": "2023-06-20 12:01:03",
                        "expired": "2023-06-20 12:11:03",
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

        var actual = await sut.QueryDepositFormStatusAsync(customerId, key, formId);

        Assert.Equal(3, actual.Status);
    }

    [Fact]
    public async Task 存款_訂單查詢_成功_狀態6_訂單異常()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://etpay888.com");

        var signer = new GCPayHasher(NullLogger<GCPayHasher>.Instance);

        var sut = new GCPayClient(httpClient, signer, NullLogger<GCPayClient>.Instance);

        var customerId = "52587";
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";
        var formId = Guid.Parse("20c04f1c94b7487aae465a46c3e62a49");

        _ = mockHttp.Expect(HttpMethod.Post, "https://etpay888.com/api/query_transaction")
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "code": 0,
                    "message": "success",
                    "data": {
                        "customer_id": 52587,
                        "order_id": "20c04f1c94b7487aae465a46c3e62a49",
                        "transaction_id": "T06191747487583145a49",
                        "status": 6,
                        "order_amount": "10000.00000000",
                        "real_amount": "10000.00000000",
                        "created": "2023-06-19 17:47:48",
                        "expired": "2023-06-19 17:57:48",
                        "notify_url": "https:\/\/dev-payment.rpgp.cc\/gcpay\/deposit\/callback",
                        "customer_callback": "OK",
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

        var actual = await sut.QueryDepositFormStatusAsync(customerId, key, formId);

        Assert.Equal(6, actual.Status);
    }

    [Fact]
    public async Task 提款_成功()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://etpay888.com");

        var signer = new GCPayHasher(NullLogger<GCPayHasher>.Instance);

        var sut = new GCPayClient(httpClient, signer, NullLogger<GCPayClient>.Instance);

        var customerId = "52587";
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";
        var formId = Guid.Parse("ab58787d44d143ca9dd2453f1bc8741e");

        _ = mockHttp.Expect(HttpMethod.Post, "https://etpay888.com/api/payments/pay_order")
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "code": 0,
                    "message": "success",
                    "data": {
                        "transaction_id": "P0619175112961319341e",
                        "amount": "100.0000"
                    }
                }
                """));

        var order = new GCPayClientWithdrawOrder(
            100M,
            formId,
            BankName: "john",
            AccountName: "ANZBP",
            CardNo: "43253432342342342",
            new Uri("https://dev-payment.rpgp.cc/gcpay/withdraw/callback"));

        var actual = await sut.WithdrawAsync(customerId, key, order);

        Assert.Equal("P0619175112961319341e", actual.ThirdPartyOrderNo);
        Assert.Equal(100M, actual.Amount);
    }

    [Fact]
    public void 提款_失敗_商戶未啟用代付()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://etpay888.com");

        var signer = new GCPayHasher(NullLogger<GCPayHasher>.Instance);

        var sut = new GCPayClient(httpClient, signer, NullLogger<GCPayClient>.Instance);

        var customerId = "52587";
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";
        var formId = Guid.Parse("8a8a630265e440ceac588be4ee73e8d3");

        _ = mockHttp.Expect(HttpMethod.Post, "https://etpay888.com/api/payments/pay_order")
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "code":20015,
                    "message":"商户未启用代付"
                }
                """));

        var order = new GCPayClientWithdrawOrder(
            100M,
            formId,
            BankName: "john",
            AccountName: "ANZBP",
            CardNo: "43253432342342342",
            new Uri("https://dev-payment.rpgp.cc/gcpay/withdraw/callback"));

        _ = Assert.ThrowsAsync<ApiFailException>(async () => await sut.WithdrawAsync(customerId, key, order));
    }

    [Fact]
    public void 提款_失敗_下單金額大於商戶餘額()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://etpay888.com");

        var signer = new GCPayHasher(NullLogger<GCPayHasher>.Instance);

        var sut = new GCPayClient(httpClient, signer, NullLogger<GCPayClient>.Instance);

        var customerId = "52587";
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";
        var formId = Guid.Parse("a50a55c26c8c45078aa93ecd7f4cee92");

        _ = mockHttp.Expect(HttpMethod.Post, "https://etpay888.com/api/payments/pay_order")
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "code": 20010,
                    "message": "下单金额大于商户餘額"
                }
                """));

        var order = new GCPayClientWithdrawOrder(
            100M,
            formId,
            BankName: "john",
            AccountName: "ANZBP",
            CardNo: "43253432342342342",
            new Uri("https://dev-payment.rpgp.cc/gcpay/withdraw/callback"));

        _ = Assert.ThrowsAsync<ApiFailException>(async () => await sut.WithdrawAsync(customerId, key, order));
    }
}
