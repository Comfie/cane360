using System.Reflection;
using Cane360.Application.Common.Security;

namespace Cane360.Application.Common.Behaviours;

public class AuthorizationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IFarmSetupRepository _farms;
    private readonly IIdentityService _identityService;
    private readonly IUser _user;

    public AuthorizationBehaviour(
        IUser user,
        IIdentityService identityService,
        IFarmSetupRepository farms)
    {
        _user = user;
        _identityService = identityService;
        _farms = farms;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        IEnumerable<AuthorizeAttribute> authorizeAttributes =
            request.GetType().GetCustomAttributes<AuthorizeAttribute>();

        if (authorizeAttributes.Any())
        {
            // Must be authenticated user
            if (_user.Id == null)
            {
                throw new UnauthorizedAccessException();
            }

            IEnumerable<AuthorizeAttribute> authorizeAttributesWithTenantRoles = authorizeAttributes
                .Where(attribute => !string.IsNullOrWhiteSpace(attribute.TenantRoles));
            if (authorizeAttributesWithTenantRoles.Any())
            {
                string? role = await _farms.GetActiveTenantSecurityRoleForUserAsync(
                    _user.Id, cancellationToken);
                if (role is null)
                {
                    throw new NotFoundException(_user.Id, "Active grower or farm-manager membership");
                }

                if (!authorizeAttributesWithTenantRoles.Any(attribute => attribute.TenantRoles
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                        .Contains(role, StringComparer.Ordinal)))
                {
                    throw new ForbiddenAccessException();
                }
            }

            // Role-based authorization
            IEnumerable<AuthorizeAttribute> authorizeAttributesWithRoles =
                authorizeAttributes.Where(a => !string.IsNullOrWhiteSpace(a.Roles));

            if (authorizeAttributesWithRoles.Any())
            {
                bool authorized = false;

                foreach (string[] roles in authorizeAttributesWithRoles.Select(a => a.Roles.Split(',')))
                {
                    foreach (string role in roles)
                    {
                        bool isInRole = _user.Roles?.Any(x => role == x) ?? false;
                        if (isInRole)
                        {
                            authorized = true;
                            break;
                        }
                    }
                }

                // Must be a member of at least one role in roles
                if (!authorized)
                {
                    throw new ForbiddenAccessException();
                }
            }

            // Policy-based authorization
            IEnumerable<AuthorizeAttribute> authorizeAttributesWithPolicies =
                authorizeAttributes.Where(a => !string.IsNullOrWhiteSpace(a.Policy));
            if (authorizeAttributesWithPolicies.Any())
            {
                foreach (string policy in authorizeAttributesWithPolicies.Select(a => a.Policy))
                {
                    bool authorized = await _identityService.AuthorizeAsync(_user.Id, policy);

                    if (!authorized)
                    {
                        throw new ForbiddenAccessException();
                    }
                }
            }
        }

        // User is authorized / authorization not required
        return await next();
    }
}
