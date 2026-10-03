namespace Core.Domain;

public sealed class Warehouse
{
    private int _occupied;

    public string Id { get; }
    public string Name { get; }
    public int Capacity { get; }
    public int Occupied => _occupied;

    private Warehouse(string id, string name, int capacity, int occupied)
    {
        Id = id;
        Name = name;
        Capacity = capacity;
        _occupied = occupied;
    }

    public static Warehouse Create(string id, string name, int capacity)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор складу обов'язковий", nameof(id));

        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity,
                "Місткість складу має бути більшою за нуль");

        return new Warehouse(id.Trim(), name.Trim(), capacity, 0);
    }

    internal void IncreaseOccupied(int amount) => _occupied += amount;
}