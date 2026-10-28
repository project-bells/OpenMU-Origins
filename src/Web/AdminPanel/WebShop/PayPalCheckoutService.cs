// <copyright file="PayPalCheckoutService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.WebShop;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

        using var orderRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUri}/v2/checkout/orders");
        orderRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
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

    private sealed record PayPalToken([property: JsonPropertyName("access_token")] string AccessToken);

    private sealed record PayPalOrder(IReadOnlyList<PayPalLink> Links);

    private sealed record PayPalLink(Uri Href, string Rel);
}
