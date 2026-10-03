namespace Core.Domain;

public static class WarehouseService
{
    public static void RegisterArrival(Warehouse warehouse, Product product, int amount)
    {
        if (warehouse.Occupied + amount > warehouse.Capacity)
            throw new InvalidOperationException(
                $"Склад {warehouse.Name} переповнений: місткість {warehouse.Capacity}, зайнято {warehouse.Occupied}, спроба додати {amount}");

        product.RegisterArrival(amount);
        warehouse.IncreaseOccupied(amount);
    }
}