namespace Cane360.Domain.Farms;

using Cane360.Domain.Activities;

public sealed class Farm : BaseAuditableEntity
{
    private readonly List<Field> _fields = [];
    private readonly List<Person> _persons = [];

    private Farm() { }

    private Farm(
        Guid tenantId,
        string code,
        string name,
        string address,
        string location,
        string tenure,
        decimal declaredHectares,
        string irrigationContext)
    {
        TenantId = tenantId;
        Code = NormaliseCode(code);
        Name = name.Trim();
        Address = address.Trim();
        Location = location.Trim();
        Tenure = tenure.Trim();
        DeclaredHectares = declaredHectares;
        IrrigationContext = irrigationContext.Trim();
        Status = RecordStatus.Active;
        Store = Store.Create(Id);
    }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public string Tenure { get; private set; } = string.Empty;
    public decimal DeclaredHectares { get; private set; }
    public string IrrigationContext { get; private set; } = string.Empty;
    public RecordStatus Status { get; private set; }
    public Store Store { get; private set; } = null!;
    public IReadOnlyCollection<Field> Fields => _fields.AsReadOnly();
    public IReadOnlyCollection<Person> Persons => _persons.AsReadOnly();

    internal static Farm Create(
        Guid tenantId,
        string code,
        string name,
        string address,
        string location,
        string tenure,
        decimal declaredHectares,
        string irrigationContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenure);
        ArgumentException.ThrowIfNullOrWhiteSpace(irrigationContext);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(declaredHectares);

        return new Farm(
            tenantId,
            code,
            name,
            address,
            location,
            tenure,
            declaredHectares,
            irrigationContext);
    }

    public Field AddField(
        string code,
        string name,
        decimal declaredHectares,
        decimal? mappedHectares,
        ReportingAreaSource reportingAreaSource,
        string irrigationMethod,
        string? soilNotes)
    {
        var normalisedCode = NormaliseCode(code);
        if (_fields.Any(field => field.Status == RecordStatus.Active && field.Code == normalisedCode))
        {
            throw new InvalidOperationException($"Field code '{normalisedCode}' is already in use on this farm.");
        }

        var field = Field.Create(
            Id,
            normalisedCode,
            name,
            declaredHectares,
            mappedHectares,
            reportingAreaSource,
            irrigationMethod,
            soilNotes);
        _fields.Add(field);

        return field;
    }

    public Person AddPerson(string displayName, string? phone, DateOnly activeFrom)
    {
        var person = Person.Create(Id, displayName, phone, activeFrom);
        _persons.Add(person);
        return person;
    }

    public PersonRoleAssignment AssignRole(
        Person person,
        PersonRole role,
        bool isPrimary,
        DateOnly effectiveFrom)
    {
        if (!_persons.Contains(person))
        {
            throw new InvalidOperationException("The person does not belong to this farm.");
        }

        if (role == PersonRole.FarmManager && isPrimary && _persons.Any(candidate =>
            candidate.RoleAssignments.Any(assignment =>
                assignment.Role == PersonRole.FarmManager && assignment.IsPrimary && assignment.EffectiveTo is null)))
        {
            throw new InvalidOperationException("This farm already has a current primary farm manager.");
        }

        return person.AssignRole(role, isPrimary, effectiveFrom);
    }

    private static string NormaliseCode(string code) => code.Trim().ToUpperInvariant();
}
