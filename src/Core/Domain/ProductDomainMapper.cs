using Core.Dto;

namespace Core.Domain;

public static class ProductDomainMapper
{
    public static ImportResult<Product> FromImportResult(ImportResult<ProductDto> dtoResult)
    {
        var products = new List<Product>();
        var errors = new List<string>(dtoResult.Errors);

        foreach (ProductDto dto in dtoResult.Items)
        {
            try
            {
                products.Add(Product.FromDto(dto));
            }
            catch (Exception ex)
            {
                errors.Add($"{dto.Id}: {ex.Message}");
            }
        }

        return new ImportResult<Product>(products, errors);
    }
}