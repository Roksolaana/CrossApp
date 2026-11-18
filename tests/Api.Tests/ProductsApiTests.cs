using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

public sealed class ProductsApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static object NewProduct(string sku, string name = "Cement", int quantity = 40) =>
        new { sku, name, unit = "bag", quantity };

    [Fact]
    public async Task Post_returns_201_and_location_leads_to_the_created_product()
    {
        var post = await _client.PostAsJsonAsync("/api/products", NewProduct("T-100"));

        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        var location = post.Headers.Location;
        Assert.NotNull(location);
        Assert.StartsWith("/api/products/", location.OriginalString);

        var get = await _client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var dto = await get.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(dto);
        Assert.Equal("T-100", dto.Sku);
        Assert.Equal(40, dto.Quantity);
    }

    [Fact]
    public async Task Post_with_invalid_body_returns_400_with_field_errors()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new { sku = "", name = "X", unit = "", quantity = -1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = json.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Sku", out _));
        Assert.True(errors.TryGetProperty("Unit", out _));
        Assert.True(errors.TryGetProperty("Quantity", out _));
        Assert.False(errors.TryGetProperty("Name", out _));
    }

    [Fact]
    public async Task Get_unknown_id_returns_404()
    {
        var response = await _client.GetAsync("/api/products/no-such-id");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_then_get_returns_404()
    {
        var post = await _client.PostAsJsonAsync("/api/products", NewProduct("T-200"));
        var location = post.Headers.Location;
        Assert.NotNull(location);

        var delete = await _client.DeleteAsync(location);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var get = await _client.GetAsync(location);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task List_supports_search_and_paging()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];

        for (var i = 1; i <= 3; i++)
            await _client.PostAsJsonAsync("/api/products", NewProduct($"PG-{i}", $"Item {tag} {i}"));

        var page1 = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"/api/products?search={tag}&page=1&pageSize=2");
        var page2 = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"/api/products?search={tag}&page=2&pageSize=2");

        Assert.NotNull(page1);
        Assert.NotNull(page2);
        Assert.Equal(3, page1.Total);
        Assert.Equal(2, page1.Items.Count);
        Assert.Single(page2.Items);
    }
}