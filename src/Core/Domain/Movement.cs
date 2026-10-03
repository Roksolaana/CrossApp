namespace Core.Domain;

public sealed class Movement
{
    public string Id { get; }
    public string ProductId { get; }
    public int Amount { get; }
    public MovementStatus Status { get; private set; }

    private Movement(string id, string productId, int amount, MovementStatus status)
    {
        Id = id;
        ProductId = productId;
        Amount = amount;
        Status = status;
    }

    public static Movement Create(string id, string productId, int amount)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор переміщення обов'язковий", nameof(id));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount,
                "Кількість переміщення має бути більшою за нуль");

        return new Movement(id.Trim(), productId.Trim(), amount, MovementStatus.Draft);
    }

    public void TransitionTo(MovementStatus target)
    {
        bool allowed = (Status, target) switch
        {
            (MovementStatus.Draft, MovementStatus.Confirmed) => true,
            (MovementStatus.Draft, MovementStatus.Cancelled) => true,
            (MovementStatus.Confirmed, MovementStatus.Cancelled) => false,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException(
                $"Перехід зі стану {Status} у {target} неможливий для переміщення {Id}");

        Status = target;
    }
}