using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;
using Tgs.Payment.GCPay.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddPaymentClient()
    .AddMessageQueue()
    .AddNatsMessageQueue(natsConfig => natsConfig
        .ConfigQueueOptions((opt, _) => opt.Url = "nats://localhost:4222"))
    .AddNatsGlobPatternExchange("*");

var app = builder.Build();

await app.StartAsync().ConfigureAwait(false);

var customerId = "52587";
var key = "57c2a457b5a5bdec4e3ac959d9c5fba9d81d1802b9f45c89f5c7561d0dec8909";

var logger = app.Services.GetRequiredService<ILogger<Program>>();

var paymentClient = app.Services.GetRequiredService<IPaymentClient>();


//PAYMAYA收款只支援PAYMAYA 通道编号 2643
var depositSettings = new GCPayDepositSettings(
    customerId,
    key,
    Guid.NewGuid(),
    PayChannelId: "2643");

var depositReturn = await paymentClient.DepositAsync(
    "tgs.payment.gcpay.deposit",
    10000M,
    JsonSerializer.Serialize(depositSettings, GCPayUtility.SerializerOptions)).ConfigureAwait(false);

logger.LogInformation("DepositReturn: {depositReturn}", depositReturn);


//提款支援PAYMAYA / GCASH / 銀行
//銀行
//var withdrawSettings = new GCPayWithdrawSettings(
//    customerId,
//    key,
//    Guid.NewGuid(),
//    BankName: "ANZBP",
//    AccountName: "john",
//    CardNo: "43253432342342342");

//var withdrawReturn = await paymentClient.WithdrawAsync(
//    "tgs.payment.gcpay.withdraw",
//    100M,
//    JsonSerializer.Serialize(withdrawSettings, GCPayUtility.SerializerOptions)).ConfigureAwait(false);

//logger.LogInformation("{withdrawReturn}", withdrawReturn);

//GCash
//var withdrawSettings = new GCPayWithdrawSettings(
//    customerId,
//    key,
//    Guid.NewGuid(),
//    BankName: "GCASH",
//    AccountName: "john",
//    CardNo: "9078533090");

//var withdrawReturn = await paymentClient.WithdrawAsync(
//    "tgs.payment.gcpay.withdraw",
//    100M,
//    JsonSerializer.Serialize(withdrawSettings, GCPayUtility.SerializerOptions)).ConfigureAwait(false);

//logger.LogInformation("{withdrawReturn}", withdrawReturn);

//PayMaya
//var withdrawSettings = new GCPayWithdrawSettings(
//    customerId,
//    key,
//    Guid.NewGuid(),
//    BankName: "PAYMP",
//    AccountName: "john",
//    CardNo: "9078533090");

//var withdrawReturn = await paymentClient.WithdrawAsync(
//    "tgs.payment.gcpay.withdraw",
//    100M,
//    JsonSerializer.Serialize(withdrawSettings, GCPayUtility.SerializerOptions)).ConfigureAwait(false);

//logger.LogInformation("{withdrawReturn}", withdrawReturn);