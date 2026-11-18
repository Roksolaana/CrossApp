namespace Api.Contracts;

public sealed record SaveProductRequest(string Sku, string Name, string Unit, int Quantity)
{
    public bool TryValidate(out Dictionary<string, string[]> errors)
    {
        var found = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(Sku) || Sku.Length > 50)
            found[nameof(Sku)] = ["Артикул обов'язковий, до 50 символів."];

        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 200)
            found[nameof(Name)] = ["Назва обов'язкова, до 200 символів."];

        if (string.IsNullOrWhiteSpace(Unit) || Unit.Length > 20)
            found[nameof(Unit)] = ["Одиниця виміру обов'язкова, до 20 символів."];

        if (Quantity < 0)
            found[nameof(Quantity)] = ["Кількість не може бути від'ємною."];

        errors = found;
        return found.Count == 0;
    }
}