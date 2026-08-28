namespace Cane360.Application.Payroll;

public sealed record PayrollWorkspaceDto(string Role, IReadOnlyList<PayrollWorkerOptionDto> Workers, IReadOnlyList<PayrollPersonOptionDto> PayingPersons);
