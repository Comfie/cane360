namespace Cane360.Web.Models.CropCycles;

public sealed record CancelCropCycleRequest(long ExpectedVersion, string Reason);
