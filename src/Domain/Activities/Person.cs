using Cane360.Domain.Farms;

namespace Cane360.Domain.Activities;

public sealed class Person : BaseAuditableEntity
{
    private readonly List<PersonRoleAssignment> _roleAssignments = [];

    private Person() { }

    private Person(Guid farmId, string displayName, string? phone, DateOnly activeFrom)
    {
        FarmId = farmId;
        DisplayName = displayName.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        ActiveFrom = activeFrom;
        Status = RecordStatus.Active;
    }

    public Guid FarmId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public DateOnly ActiveFrom { get; private set; }
    public DateOnly? ActiveTo { get; private set; }
    public RecordStatus Status { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<PersonRoleAssignment> RoleAssignments => _roleAssignments.AsReadOnly();

    internal static Person Create(Guid farmId, string displayName, string? phone, DateOnly activeFrom)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return new Person(farmId, displayName, phone, activeFrom);
    }

    public PersonRoleAssignment AssignRole(PersonRole role, bool isPrimary, DateOnly effectiveFrom)
    {
        if (Status != RecordStatus.Active)
        {
            throw new InvalidOperationException("Roles cannot be assigned to an inactive person.");
        }

        if (_roleAssignments.Any(assignment => assignment.Role == role && assignment.EffectiveTo is null))
        {
            throw new InvalidOperationException($"This person already has a current {FormatRole(role)} assignment.");
        }

        var assignment = PersonRoleAssignment.Create(FarmId, Id, role, isPrimary, effectiveFrom);
        _roleAssignments.Add(assignment);
        Version++;
        return assignment;
    }

    public void UpdateDetailsAndReplaceCurrentRoles(
        string displayName,
        string? phone,
        PersonRole role,
        bool isPrimary,
        DateOnly roleEffectiveFrom,
        long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status != RecordStatus.Active)
        {
            throw new InvalidOperationException("Inactive personnel records cannot be updated.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (roleEffectiveFrom < ActiveFrom)
        {
            throw new InvalidOperationException("The role effective date cannot be before the person became active.");
        }

        var currentRoles = _roleAssignments.Where(assignment => assignment.EffectiveTo is null).ToArray();
        if (currentRoles.Any(assignment => assignment.EffectiveFrom >= roleEffectiveFrom))
        {
            throw new InvalidOperationException("The role effective date must be after the start date of each current role.");
        }

        DisplayName = displayName.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        foreach (var currentRole in currentRoles)
        {
            currentRole.End(roleEffectiveFrom.AddDays(-1));
        }

        _roleAssignments.Add(PersonRoleAssignment.Create(FarmId, Id, role, isPrimary, roleEffectiveFrom));
        Version++;
    }

    public void EndRole(Guid assignmentId, DateOnly effectiveTo, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        var assignment = _roleAssignments.SingleOrDefault(item => item.Id == assignmentId)
            ?? throw new InvalidOperationException("The role assignment does not belong to this person.");
        assignment.End(effectiveTo);
        Version++;
    }

    public void Deactivate(DateOnly activeTo, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status != RecordStatus.Active)
        {
            throw new InvalidOperationException("This person is already inactive.");
        }

        if (activeTo < ActiveFrom)
        {
            throw new InvalidOperationException("The inactive date cannot be before the active date.");
        }

        foreach (var role in _roleAssignments.Where(item => item.EffectiveTo is null))
        {
            role.End(activeTo < role.EffectiveFrom ? role.EffectiveFrom : activeTo);
        }

        ActiveTo = activeTo;
        Status = RecordStatus.Archived;
        Version++;
    }

    public bool HasEffectiveRole(PersonRole role, DateOnly onDate) =>
        ActiveFrom <= onDate &&
        (ActiveTo is null || ActiveTo >= onDate) &&
        _roleAssignments.Any(assignment => assignment.Role == role && assignment.IsEffective(onDate));

    private void RequireVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new InvalidOperationException("This personnel record changed after it was loaded. Refresh and try again.");
        }
    }

    private static string FormatRole(PersonRole role) => role switch
    {
        PersonRole.FarmManager => "farm-manager",
        _ => role.ToString().ToLowerInvariant()
    };
}
