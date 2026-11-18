using System.Security.Cryptography;
using Api.Contracts;
using Api.Filters;
using Core.Abstractions;
using Core.Domain;
using Core.Reports;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProducts(this WebApplication app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", List);
        group.MapGet("/low-stock", LowStock);
        group.MapGet("/{id}", GetById);

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<SaveProductRequest>>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id}", Replace)
            .AddEndpointFilter<ValidationFilter<SaveProductRequest>>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id}", Delete);
    }

    private static Results<Ok<PagedResult<ProductDto>>, ValidationProblem> List(
        ICatalogStore store, int? page, int? pageSize, string? search)
    {
        var currentPage = page ?? 1;
        var size = pageSize ?? 20;

        var errors = new Dictionary<string, string[]>();
        if (currentPage < 1)
            errors["page"] = ["Номер сторінки має бути не менше 1."];
        if (size is < 1 or > 100)
            errors["pageSize"] = ["Розмір сторінки має бути від 1 до 100."];
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        IEnumerable<Product> query = store.List();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Sku.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query.OrderBy(p => p.Sku, StringComparer.Ordinal).ToList();
        var offset = (long)(currentPage - 1) * size;

        var items = offset >= ordered.Count
            ? new List<ProductDto>()
            : ordered.Skip((int)offset).Take(size).Select(p => p.ToContract()).ToList();

        return TypedResults.Ok(new PagedResult<ProductDto>(items, ordered.Count));
    }

    private static Results<Ok<List<LowStockDto>>, ValidationProblem> LowStock(
        ICatalogStore store, int? threshold)
    {
        var limit = threshold ?? 5;

        if (limit < 0)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["threshold"] = ["Поріг не може бути від'ємним."]
            });

        var result = ReportService.LowStock(store.List(), limit)
            .Select(line => line.ToContract())
            .ToList();

        return TypedResults.Ok(result);
    }

    private static Results<Ok<ProductDto>, NotFound> GetById(string id, ICatalogStore store) =>
        store.GetById(id) is { } found
            ? TypedResults.Ok(found.ToContract())
            : TypedResults.NotFound();

    private static Results<Created<ProductDto>, ValidationProblem> Create(
        SaveProductRequest request, ICatalogStore store)
    {
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();

        try
        {
            var created = Product.Create(id, request.Sku, request.Name, request.Unit, request.Quantity);
            store.Add(created);
            return TypedResults.Created($"/api/products/{created.Id}", created.ToContract());
        }
        catch (ArgumentException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [ex.ParamName ?? "request"] = [ex.Message]
            });
        }
    }

    private static Results<Ok<ProductDto>, NotFound, ValidationProblem> Replace(
        string id, SaveProductRequest request, ICatalogStore store)
    {
        if (store.GetById(id) is null)
            return TypedResults.NotFound();

        try
        {
            var replacement = Product.Create(id, request.Sku, request.Name, request.Unit, request.Quantity);
            store.Update(replacement);
            return TypedResults.Ok(replacement.ToContract());
        }
        catch (ArgumentException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [ex.ParamName ?? "request"] = [ex.Message]
            });
        }
    }

    private static Results<NoContent, NotFound> Delete(string id, ICatalogStore store) =>
        store.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound();
}