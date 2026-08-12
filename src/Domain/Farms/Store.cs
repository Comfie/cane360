namespace Cane360.Domain.Farms;

public sealed class Store : BaseAuditableEntity
{
    private Store() { }

    private Store(Guid farmId)
    {
        FarmId = farmId;
        Code = "MAIN";
        Name = "Main store";
        Status = RecordStatus.Active;
    }

    public Guid FarmId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public RecordStatus Status { get; private set; }

    internal static Store Create(Guid farmId) => new(farmId);
}
