// <copyright file="PayPalCheckoutServiceTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.WebShop;

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MUnique.OpenMU.Web.AdminPanel.WebShop;

/// <summary>
/// Tests the server-side verification of captured PayPal orders.
/// </summary>
public class PayPalCheckoutServiceTests
{
    private static readonly WebShopPackage Package = new() { Code = "THB100", PriceThb = 100, WCoinC = 1100 };

    /// <summary>
    /// Verifies a completed capture with the expected account and amount.
    /// </summary>
    [Test]
    public async Task CaptureAndVerifyOrderAsyncReturnsTrueForExpectedCapture()
    {
        using var handler = new QueueHandler(TokenResponse(), OrderResponse(HttpStatusCode.Created, "buyer", "100.00"));
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        Assert.That(await service.CaptureAndVerifyOrderAsync(Package, "buyer", "PAYPAL-1", CancellationToken.None), Is.True);
    }

    /// <summary>
    /// Rejects a completed capture if PayPal reports a different amount.
    /// </summary>
    [Test]
    public async Task CaptureAndVerifyOrderAsyncReturnsFalseForWrongAmount()
    {
        using var handler = new QueueHandler(TokenResponse(), OrderResponse(HttpStatusCode.Created, "buyer", "1.00"));
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        Assert.That(await service.CaptureAndVerifyOrderAsync(Package, "buyer", "PAYPAL-2", CancellationToken.None), Is.False);
    }

    /// <summary>
    /// Reads and verifies an order after PayPal rejects a repeated capture.
    /// </summary>
    [Test]
    public async Task CaptureAndVerifyOrderAsyncVerifiesPreviouslyCapturedOrder()
    {
        using var handler = new QueueHandler(
            TokenResponse(),
            new HttpResponseMessage(HttpStatusCode.UnprocessableEntity),
            OrderResponse(HttpStatusCode.OK, "buyer", "100.00"));
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        Assert.That(await service.CaptureAndVerifyOrderAsync(Package, "buyer", "PAYPAL-3", CancellationToken.None), Is.True);
    }

    private static PayPalCheckoutService CreateService(HttpClient client)
        => new(client, Options.Create(new WebShopOptions
        {
            PayPalClientId = "client",
            PayPalClientSecret = "secret",
            UsePayPalSandbox = true,
        }));

    private static HttpResponseMessage TokenResponse()
        => JsonResponse(HttpStatusCode.OK, """{"access_token":"token"}""");

    private static HttpResponseMessage OrderResponse(HttpStatusCode statusCode, string loginName, string amount)
        => JsonResponse(statusCode, $$"""
            {
              "status": "COMPLETED",
              "purchase_units": [{
                "reference_id": "THB100",
                "custom_id": "{{loginName}}",
                "payments": { "captures": [{
                  "status": "COMPLETED",
                  "amount": { "currency_code": "THB", "value": "{{amount}}" }
                }]}
              }]
            }
            """);

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        => new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class QueueHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(this._responses.Dequeue());
    }
}
