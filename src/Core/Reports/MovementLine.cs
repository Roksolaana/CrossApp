namespace Core.Reports;

public sealed record MovementLine(string MovementId, string Sku, string Name, int Amount, string Status);