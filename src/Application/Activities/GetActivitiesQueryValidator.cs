using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed class GetActivitiesQueryValidator : AbstractValidator<GetActivitiesQuery>
{
    public GetActivitiesQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.ToDate).GreaterThanOrEqualTo(query => query.FromDate!.Value)
            .When(query => query.FromDate.HasValue && query.ToDate.HasValue);
        RuleFor(query => query.Status).IsEnumName(typeof(ActivityStatus), false)
            .When(query => !string.IsNullOrWhiteSpace(query.Status));
    }
}
