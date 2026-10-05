using System.Globalization;
using Microsoft.Extensions.Options;

namespace Cane360.Application.CropCycles;

public sealed class CalculateCropMaturityQueryHandler(IFarmSetupRepository repository, IUser user,
    IOptions<CropMaturityOptions> options) : IRequestHandler<CalculateCropMaturityQuery, CropMaturityDto>
{
    public async Task<CropMaturityDto> Handle(CalculateCropMaturityQuery request, CancellationToken cancellationToken)
    {
        Tenant tenant = await CropCycleAccess.RequireTenantAsync(repository, user, false, cancellationToken);
        CropCycleAccess.RequireField(tenant, request.FieldId);
        return new CropMaturityDto(options.Value.DefaultCropMaturityMonths,
            options.Value.Calculate(request.PlantingDate)?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}
