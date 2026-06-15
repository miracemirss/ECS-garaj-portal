namespace ECS.Domain.Exceptions;

/// <summary>Raised when a stock decrease would drive a part's stock negative.</summary>
public sealed class InsufficientStockException : DomainException
{
    public InsufficientStockException(Guid partId, decimal available, decimal requested)
        : base($"Insufficient stock for part {partId}: available {available}, requested {requested}.")
    {
        PartId = partId;
        Available = available;
        Requested = requested;
    }

    public Guid PartId { get; }
    public decimal Available { get; }
    public decimal Requested { get; }
}
