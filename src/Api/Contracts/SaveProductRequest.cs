namespace Api.Contracts;

public sealed record SaveProductRequest(string Sku, string Name, string Unit, int Quantity);