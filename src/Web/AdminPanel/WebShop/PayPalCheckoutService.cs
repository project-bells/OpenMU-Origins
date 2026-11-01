// <copyright file="PayPalCheckoutService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.WebShop;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.Extensions.Options;

/// <summary>
/// Creates PayPal checkout orders for configured WCoin packages.
/// </summary>
public sealed class PayPalCheckoutService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<WebShopOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PayPalCheckoutService"/> class.
    /// </summary>
    public PayPalCheckoutService(HttpClient httpClient, IOptions<WebShopOptions> options)
    {
        this._httpClient = httpClient;
        this._options = options;
    }

    /// <summary>
    /// Creates an order and returns the PayPal approval URL.
    /// </summary>
    public async ValueTask<Uri> CreateOrderAsync(WebShopPackage package, string loginName, Uri returnUrl, Uri cancelUrl, CancellationToken cancellationToken)
    {
        var (baseUri, accessToken) = await this.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        using var orderRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUri}/v2/checkout/orders");
        orderRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        orderRequest.Content = JsonContent.Create(new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = package.Code,
                    custom_id = loginName,
                    description = $"{package.WCoinC} WCoinC",
                    amount = new { currency_code = "THB", value = package.PriceThb.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) },
                },
            },
            payment_source = new
            {
                paypal = new
                {
                    experience_context = new
                    {
                        user_action = "PAY_NOW",
                        return_url = returnUrl.AbsoluteUri,
                        cancel_url = cancelUrl.AbsoluteUri,
                    },
                },
            },
        });
        using var orderResponse = await this._httpClient.SendAsync(orderRequest, cancellationToken).ConfigureAwait(false);
        orderResponse.EnsureSuccessStatusCode();
        var order = await orderResponse.Content.ReadFromJsonAsync<PayPalOrder>(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PayPal returned no order.");
        return order.Links.Single(link => string.Equals(link.Rel, "payer-action", StringComparison.OrdinalIgnoreCase)).Href;
    }

    /// <summary>
    /// Captures an approved order and verifies that it belongs to the expected account and package.
    /// </summary>
    public async ValueTask<bool> CaptureAndVerifyOrderAsync(WebShopPackage package, string loginName, string orderId, CancellationToken cancellationToken)
    {
        var (baseUri, accessToken) = await this.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        var escapedOrderId = Uri.EscapeDataString(orderId);
        using var captureRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUri}/v2/checkout/orders/{escapedOrderId}/capture")
        {
            Content = JsonContent.Create(new { }),
        };
        captureRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var captureResponse = await this._httpClient.SendAsync(captureRequest, cancellationToken).ConfigureAwait(false);

        PayPalOrderDetails? order;
        if (captureResponse.IsSuccessStatusCode)
        {
            order = await captureResponse.Content.ReadFromJsonAsync<PayPalOrderDetails>(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // PayPal rejects a repeated capture. Reading the order makes the return endpoint safely retryable.
            using var orderRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseUri}/v2/checkout/orders/{escapedOrderId}");
            orderRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var orderResponse = await this._httpClient.SendAsync(orderRequest, cancellationToken).ConfigureAwait(false);
            orderResponse.EnsureSuccessStatusCode();
            order = await orderResponse.Content.ReadFromJsonAsync<PayPalOrderDetails>(cancellationToken).ConfigureAwait(false);
        }

        return order is not null && IsExpectedCompletedOrder(order, package, loginName);
    }

    private static bool IsExpectedCompletedOrder(PayPalOrderDetails order, WebShopPackage package, string loginName)
    {
        if (!string.Equals(order.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase) || order.PurchaseUnits.Count != 1)
        {
            return false;
        }

        var purchase = order.PurchaseUnits[0];
        var expectedPrice = package.PriceThb.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        return string.Equals(purchase.ReferenceId, package.Code, StringComparison.Ordinal)
            && string.Equals(purchase.CustomId, loginName, StringComparison.Ordinal)
            && purchase.Payments?.Captures.Count == 1
            && string.Equals(purchase.Payments.Captures[0].Status, "COMPLETED", StringComparison.OrdinalIgnoreCase)
            && string.Equals(purchase.Payments.Captures[0].Amount.CurrencyCode, "THB", StringComparison.Ordinal)
            && string.Equals(purchase.Payments.Captures[0].Amount.Value, expectedPrice, StringComparison.Ordinal);
    }

    private async ValueTask<(string BaseUri, string AccessToken)> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var options = this._options.Value;
        var baseUri = options.UsePayPalSandbox ? "https://api-m.sandbox.paypal.com" : "https://api-m.paypal.com";
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUri}/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" }),
        };
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{options.PayPalClientId}:{options.PayPalClientSecret}")));
        using var tokenResponse = await this._httpClient.SendAsync(tokenRequest, cancellationToken).ConfigureAwait(false);
        tokenResponse.EnsureSuccessStatusCode();
        var token = await tokenResponse.Content.ReadFromJsonAsync<PayPalToken>(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PayPal returned no access token.");
        return (baseUri, token.AccessToken);
    }

    private sealed record PayPalToken([property: JsonPropertyName("access_token")] string AccessToken);

    private sealed record PayPalOrder(IReadOnlyList<PayPalLink> Links);

    private sealed record PayPalLink(Uri Href, string Rel);

    private sealed record PayPalOrderDetails(string Status, [property: JsonPropertyName("purchase_units")] IReadOnlyList<PayPalPurchaseUnit> PurchaseUnits);

    private sealed record PayPalPurchaseUnit(
        [property: JsonPropertyName("reference_id")] string ReferenceId,
        [property: JsonPropertyName("custom_id")] string CustomId,
        PayPalPayments? Payments);

    private sealed record PayPalPayments(IReadOnlyList<PayPalCapture> Captures);

    private sealed record PayPalCapture(string Status, PayPalAmount Amount);

    private sealed record PayPalAmount([property: JsonPropertyName("currency_code")] string CurrencyCode, string Value);
}
