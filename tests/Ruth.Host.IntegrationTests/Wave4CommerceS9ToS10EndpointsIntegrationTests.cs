using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class Wave4CommerceS9ToS10EndpointsIntegrationTests(HostContainersFixture containers)
{
    [Fact]
    public async Task Catalog_StorefrontQuidProQuo_PunchPasses_Bookings_OpenApi_AndBlazorPages_WorkEndToEnd()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = HostWebApplicationFactory.Create(
            isolatedDb,
            containers.RedisConnectionString,
            containers.RustFsEndpoint,
            environment: "Development",
            overrides: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Bootstrap:AutoMigrateAndSeed"] = "true",
                ["OpenApi:Enabled"] = "true",
            }
        );
        using var client = factory.CreateNoRedirectClient();

        var token = await Wave4CommerceHttpSeedHelper.OnboardAndLoginAsync(client);
        var (operatingAccId, charitableAccId, gatewayId) =
            await Wave4CommerceHttpSeedHelper.LoadFinanceSeedsAsync(client, token);

        await VerifyCatalogQuidProQuoCheckoutAndPunchPassAsync(
            client,
            token,
            operatingAccId,
            charitableAccId,
            gatewayId
        );
        await VerifyBookableServicesRentalsAndSlotHoldsAsync(
            client,
            token,
            operatingAccId,
            gatewayId
        );
        await VerifyOpenApiAndBlazorPagesAsync(client);
    }

    private static async Task VerifyCatalogQuidProQuoCheckoutAndPunchPassAsync(
        HttpClient client,
        string token,
        Guid operatingAccId,
        Guid charitableAccId,
        Guid gatewayId
    )
    {
        using var ppResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/commerce/catalog/punch-pass-types",
            new
            {
                name = "5-Visit Swim Pass",
                code = "SWIM-5",
                creditUnits = 5,
                validityDays = 90,
                allowHouseholdSharing = true,
            }
        );
        Assert.Equal(HttpStatusCode.Created, ppResp.StatusCode);
        using var ppDoc = JsonDocument.Parse(await ppResp.Content.ReadAsStringAsync());
        var passTypeId = ppDoc.RootElement.GetProperty("id").GetGuid();

        using var ruleResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/commerce/catalog/member-pricing-rules",
            new
            {
                name = "Gold 10% Off",
                discountKind = "Percentage",
                discountValue = 10.0m,
                requiredMembershipTier = "Gold",
            }
        );
        Assert.Equal(HttpStatusCode.Created, ruleResp.StatusCode);

        var variantId = await CreateQuidProQuoAndPunchProductsAsync(
            client,
            token,
            operatingAccId,
            charitableAccId,
            gatewayId,
            passTypeId
        );

        await VerifyCheckoutAndPassRedemptionAsync(client, token, variantId);
    }

    private static async Task<Guid> CreateQuidProQuoAndPunchProductsAsync(
        HttpClient client,
        string token,
        Guid operatingAccId,
        Guid charitableAccId,
        Guid gatewayId,
        Guid passTypeId
    )
    {
        using var qpqResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/commerce/catalog/products",
            new
            {
                name = "Gala Benefactor Box",
                slug = "gala-benefactor-box",
                kind = "PhysicalPickupOrFlatRate",
                currencyCode = "USD",
                revenueAccountId = operatingAccId,
                isQuidProQuo = true,
                quidProQuoCharitableAccountId = charitableAccId,
                defaultFinancialGatewayId = gatewayId,
                allowLocalPickup = true,
                variants = new[]
                {
                    new
                    {
                        sku = "GALA-BOX-1",
                        name = "Standard Box",
                        unitPrice = 100.00m,
                        fairMarketValueAmount = 30.00m,
                        trackInventory = true,
                        stockQuantity = 10,
                    },
                },
            }
        );
        Assert.Equal(HttpStatusCode.Created, qpqResp.StatusCode);

        using var packProdResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/commerce/catalog/products",
            new
            {
                name = "5-Visit Swim Pack",
                slug = "swim-pack-5",
                kind = "PunchPassPack",
                currencyCode = "USD",
                revenueAccountId = operatingAccId,
                defaultFinancialGatewayId = gatewayId,
                variants = new[]
                {
                    new
                    {
                        sku = "SWIM-PACK-5",
                        name = "5 Visits",
                        unitPrice = 50.00m,
                        punchPassTypeId = passTypeId,
                    },
                },
            }
        );
        Assert.Equal(HttpStatusCode.Created, packProdResp.StatusCode);
        using var packDoc = JsonDocument.Parse(await packProdResp.Content.ReadAsStringAsync());
        return packDoc.RootElement.GetProperty("variants")[0].GetProperty("id").GetGuid();
    }

    private static async Task VerifyCheckoutAndPassRedemptionAsync(
        HttpClient client,
        string token,
        Guid packVariantId
    )
    {
        using var qpqGet = await client.GetAsync(
            new Uri("/api/commerce/storefront/products/gala-benefactor-box", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, qpqGet.StatusCode);
        using var qpqDoc = JsonDocument.Parse(await qpqGet.Content.ReadAsStringAsync());
        var qpqVariantId = qpqDoc
            .RootElement.GetProperty("variants")[0]
            .GetProperty("id")
            .GetGuid();

        using var checkoutResp = await client.PostAsJsonAsync(
            new Uri("/api/commerce/storefront/checkout", UriKind.Relative),
            new
            {
                buyerName = "Devon Brooks",
                buyerEmail = "devon.brooks@example.org",
                explicitMembershipTier = "Gold",
                fulfillmentMethod = "LocalPickup",
                oneTimeHostedPaymentToken = "tok_host_commerce_4242",
                items = new[]
                {
                    new { variantId = qpqVariantId, quantity = 1 },
                    new { variantId = packVariantId, quantity = 1 },
                },
            }
        );
        Assert.Equal(HttpStatusCode.Created, checkoutResp.StatusCode);
        using var ordDoc = JsonDocument.Parse(await checkoutResp.Content.ReadAsStringAsync());
        // Gold 10% off: $100 -> $90 (FMV $30 non-deductible, $60 charitable) + $50 -> $45 non-deductible = $135 total ($75 non-deductible, $60 charitable)
        Assert.Equal(135.00m, ordDoc.RootElement.GetProperty("totalAmount").GetDecimal());
        Assert.Equal(
            75.00m,
            ordDoc.RootElement.GetProperty("nonDeductibleRevenueAmount").GetDecimal()
        );
        Assert.Equal(
            60.00m,
            ordDoc.RootElement.GetProperty("taxDeductibleContributionAmount").GetDecimal()
        );
        var issuedPassId = ordDoc.RootElement.GetProperty("issuedPunchPassIds")[0].GetGuid();

        using var redeemResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            $"/api/commerce/punch-passes/{issuedPassId:D}/redeem",
            new { issuedPunchPassId = issuedPassId, creditsToRedeem = 2 }
        );
        Assert.Equal(HttpStatusCode.OK, redeemResp.StatusCode);
        using var redeemDoc = JsonDocument.Parse(await redeemResp.Content.ReadAsStringAsync());
        Assert.Equal(3, redeemDoc.RootElement.GetProperty("remainingCredits").GetInt32());
    }

    private static async Task VerifyBookableServicesRentalsAndSlotHoldsAsync(
        HttpClient client,
        string token,
        Guid operatingAccId,
        Guid gatewayId
    )
    {
        using var svcResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/commerce/bookings/services",
            new
            {
                name = "Host Virtual Coaching",
                slug = "host-virtual-coaching",
                deliveryMode = "Remote",
                durationMinutes = 30,
                slotIncrementMinutes = 30,
                currencyCode = "USD",
                standardPrice = 40.00m,
                isFreeForMembers = true,
                freeOrDiscountedMemberTier = "Gold",
                revenueAccountId = operatingAccId,
                defaultFinancialGatewayId = gatewayId,
            }
        );
        Assert.Equal(HttpStatusCode.Created, svcResp.StatusCode);
        using var svcDoc = JsonDocument.Parse(await svcResp.Content.ReadAsStringAsync());
        var serviceId = svcDoc.RootElement.GetProperty("id").GetGuid();

        var tomorrow = DateTimeOffset.UtcNow.AddDays(2);
        var windowStart = new DateTimeOffset(
            tomorrow.Year,
            tomorrow.Month,
            tomorrow.Day,
            14,
            0,
            0,
            TimeSpan.Zero
        );

        using var ruleResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/commerce/bookings/rules",
            new
            {
                name = "Custom Coaching Window",
                targetKind = "Service",
                ruleType = "CustomDateWindow",
                bookableServiceId = serviceId,
                customStartUtc = windowStart,
                customEndUtc = windowStart.AddHours(2),
            }
        );
        Assert.Equal(HttpStatusCode.Created, ruleResp.StatusCode);

        using var slotsResp = await client.PostAsJsonAsync(
            new Uri("/api/commerce/bookings/public/slots", UriKind.Relative),
            new
            {
                targetKind = "Service",
                targetIdOrSlug = "host-virtual-coaching",
                rangeStartUtc = windowStart.AddHours(-1),
                rangeEndUtc = windowStart.AddHours(3),
                requestedQuantity = 1,
            }
        );
        Assert.Equal(HttpStatusCode.OK, slotsResp.StatusCode);

        using var holdResp = await client.PostAsJsonAsync(
            new Uri("/api/commerce/bookings/public/holds", UriKind.Relative),
            new
            {
                targetKind = "Service",
                targetIdOrSlug = "host-virtual-coaching",
                slotStartUtc = windowStart,
                quantity = 1,
            }
        );
        Assert.Equal(HttpStatusCode.Created, holdResp.StatusCode);
        using var holdDoc = JsonDocument.Parse(await holdResp.Content.ReadAsStringAsync());
        var holdToken = holdDoc.RootElement.GetProperty("holdToken").GetString();

        using var confirmResp = await client.PostAsJsonAsync(
            new Uri("/api/commerce/bookings/public/confirm", UriKind.Relative),
            new
            {
                targetKind = "Service",
                targetIdOrSlug = "host-virtual-coaching",
                slotStartUtc = windowStart,
                customerName = "Devon Brooks",
                customerEmail = "devon.brooks@example.org",
                holdToken,
                explicitMembershipTier = "Gold",
            }
        );
        Assert.Equal(HttpStatusCode.Created, confirmResp.StatusCode);
        using var confirmDoc = JsonDocument.Parse(await confirmResp.Content.ReadAsStringAsync());
        Assert.Equal("MemberFree", confirmDoc.RootElement.GetProperty("pricingKind").GetString());
        Assert.Equal("Remote", confirmDoc.RootElement.GetProperty("deliveryMode").GetString());
    }

    private static async Task VerifyOpenApiAndBlazorPagesAsync(HttpClient client)
    {
        using var openApiResp = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, openApiResp.StatusCode);
        var openApiText = await openApiResp.Content.ReadAsStringAsync();
        Assert.Contains("/api/commerce/storefront/checkout", openApiText, StringComparison.Ordinal);
        Assert.Contains(
            "/api/commerce/bookings/public/confirm",
            openApiText,
            StringComparison.Ordinal
        );

        foreach (
            var route in new[]
            {
                "/commerce/catalog",
                "/commerce/orders",
                "/commerce/storefront",
                "/commerce/bookings",
                "/commerce/book",
                "/commerce/book/host-virtual-coaching",
            }
        )
        {
            using var response = await client.GetAsync(new Uri(route, UriKind.Relative));
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            Assert.DoesNotContain(
                "An unhandled error has occurred",
                html,
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}
