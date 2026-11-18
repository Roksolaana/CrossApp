using System.Security.Cryptography;
using Api.Contracts;
using Core;
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
        group.MapPost("/", Create);
        group.MapPut("/{id}", Replace);
        group.MapDelete("/{id}", Delete);
    }

    private static Ok<List<ProductDto>> List(ICatalogStore store) =>
        TypedResults.Ok(store.List().Select(p => p.ToContract()).ToList());

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

    private static Results<Created<ProductDto>, ValidationProblem, ProblemHttpResult> Create(
        SaveProductRequest request, ICatalogStore store)
    {
        if (!request.TryValidate(out var errors))
            return TypedResults.ValidationProblem(errors);

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
        catch (DomainException ex)
        {
            return TypedResults.Problem(ex.Message, title: "Порушено правило домену", statusCode: 409);
        }
    }

    private static Results<Ok<ProductDto>, NotFound, ValidationProblem, ProblemHttpResult> Replace(
        string id, SaveProductRequest request, ICatalogStore store)
    {
        if (!request.TryValidate(out var errors))
            return TypedResults.ValidationProblem(errors);

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
        catch (DomainException ex)
        {
            return TypedResults.Problem(ex.Message, title: "Порушено правило домену", statusCode: 409);
        }
    }

    private static Results<NoContent, NotFound> Delete(string id, ICatalogStore store) =>
        store.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound();
}