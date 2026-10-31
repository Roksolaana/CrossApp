namespace Core.Reports;

public sealed record MovementSummary(string Sku, string Name, int MovementsCount, int TotalAmount);