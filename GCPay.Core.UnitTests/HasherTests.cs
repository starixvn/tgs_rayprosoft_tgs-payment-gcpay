using Microsoft.Extensions.Logging.Abstractions;
using Tgs.Payment.GCPay;

namespace GCPay.Core.UnitTests;

public class HasherTests
{
    [Fact]
    public void Deposit_Sample()
    {
        var key = "a4f3d1b62c31e6905059a09b6bb8102047c7a604e6cb1a8a3d8837ed1986854b";
        var dict = new Dictionary<string, string?>
        {
            ["pay_order_id"] = "T12345678",
            ["pay_amount"] = "500",
            ["pay_notify_url"] = "http://test",
            ["pay_channel_id"] = "100",
            ["pay_apply_date"] = "1602659172",
            ["pay_customer_id"] = "50003"
        };
        var expected = "F8FD4D7D991A05769690C20E16747C47";

        var sut = new GCPayHasher(NullLogger<GCPayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Deposit_Paymaya()
    {
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";
        var dict = new Dictionary<string, string?>
        {
            ["pay_order_id"] = "997a590b2b3b4f2e937439f7efa32670",
            ["pay_amount"] = "10000",
            ["pay_notify_url"] = "https://dev-payment.rpgp.cc/gcpay/deposit/callback",
            ["pay_channel_id"] = "2643",
            ["pay_apply_date"] = "1687238844",
            ["pay_customer_id"] = "52587"
        };
        var expected = "53291FE292A894A78E35A2EFAE2A0AF5";

        var sut = new GCPayHasher(NullLogger<GCPayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Query_Deposit_Status()
    {
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";
        var dict = new Dictionary<string, string?>
        {
            ["pay_customer_id"] = "52587",
            ["pay_apply_date"] = "1687239720",
            ["pay_order_id"] = "997a590b2b3b4f2e937439f7efa32670",
        };
        var expected = "A026A47944805A44ED95C568DBBBE229";

        var sut = new GCPayHasher(NullLogger<GCPayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Deposit_Callback()
    {
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";
        var dict = new Dictionary<string, string?>
        {
            ["customer_id"] = "52587",
            ["order_id"] = "58270ddb0fcd4fdcb355bbd71ff3167d",
            ["transaction_id"] = "T0619165455174136567d",
            ["order_amount"] = "100",
            ["real_amount"] = "100",
            ["status"] = "30000",
            ["message"] = "\u652f\u4ed8\u6210\u529f",
            ["sign"] = "1AB26E12AD1CD5430EFED2BEB64C2B3B"
        };
        var expected = "1AB26E12AD1CD5430EFED2BEB64C2B3B";

        var sut = new GCPayHasher(NullLogger<GCPayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Withdraw_Bank()
    {
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";
        var dict = new Dictionary<string, string?>
        {
            ["pay_customer_id"] = "52587",
            ["pay_apply_date"] = "1687168270",
            ["pay_order_id"] = "ab58787d44d143ca9dd2453f1bc8741e",
            ["pay_notify_url"] = "https://dev-payment.rpgp.cc/gcpay/withdraw/callback",
            ["pay_amount"] = "100",
            ["pay_bank_name"] = "ANZBP",
            ["pay_account_name"] = "john",
            ["pay_card_no"] = "43253432342342342",
        };
        var expected = "680FB602C1A3B0B52C6C8B4FB5E0773A";

        var sut = new GCPayHasher(NullLogger<GCPayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Withdraw_Callback_Status_30000()
    {
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";
        var dict = new Dictionary<string, string?>
        {
            ["customer_id"] = "52587",
            ["order_id"] = "ab58787d44d143ca9dd2453f1bc8741e",
            ["amount"] = "100.0000",
            ["datetime"] = "2023-06-19 17:51:12",
            ["transaction_id"] = "P0619175112961319341e",
            ["transaction_code"] = "30000",
            ["transaction_msg"] = "\u652f\u4ed8\u6210\u529f",
            ["sign"] = "AB3DB06851899C79147FF2603FFC4374",
        };
        var expected = "AB3DB06851899C79147FF2603FFC4374";

        var sut = new GCPayHasher(NullLogger<GCPayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Withdraw_Callback_Status_40000()
    {
        var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";
        var dict = new Dictionary<string, string?>
        {
            ["customer_id"] = "52587",
            ["order_id"] = "ab58787d44d143ca9dd2453f1bc8741e",
            ["amount"] = "100.0000",
            ["datetime"] = "2023-06-19 17:51:12",
            ["transaction_id"] = "P0619175112961319341e",
            ["transaction_code"] = "40000",
            ["transaction_msg"] = "\u652f\u4ed8\u9a73\u56de",
            ["sign"] = "56AF26B0447455F03CD76680324E46F0",
        };
        var expected = "56AF26B0447455F03CD76680324E46F0";

        var sut = new GCPayHasher(NullLogger<GCPayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict);

        Assert.Equal(expected, actual);
    }
}
