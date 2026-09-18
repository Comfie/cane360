using System.Reflection;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Common.Security;

namespace Cane360.Application.Common.Behaviours;

public class AuthorizationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> 
    where TRequest : notnull
{
    private readonly IUser _user;
    private readonly IIdentityService _identityService;
    private readonly IFarmSetupRepository _farms;

    public AuthorizationBehaviour(
        IUser user,
        IIdentityService identityService,
        IFarmSetupRepository farms)
    {
        _user = user;
        _identityService = identityService;
        _farms = farms;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var authorizeAttributes = request.GetType().GetCustomAttributes<AuthorizeAttribute>();

        if (authorizeAttributes.Any())
        {
            // Must be authenticated user
            if (_user.Id == null)
            {
                throw new UnauthorizedAccessException();
            }

            var authorizeAttributesWithTenantRoles = authorizeAttributes
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
            var authorizeAttributesWithRoles = authorizeAttributes.Where(a => !string.IsNullOrWhiteSpace(a.Roles));

            if (authorizeAttributesWithRoles.Any())
            {
                var authorized = false;

                foreach (var roles in authorizeAttributesWithRoles.Select(a => a.Roles.Split(',')))
                {
                    foreach (var role in roles)
                    {
                        var isInRole = _user.Roles?.Any(x => role == x)??false;
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
            var authorizeAttributesWithPolicies = authorizeAttributes.Where(a => !string.IsNullOrWhiteSpace(a.Policy));
            if (authorizeAttributesWithPolicies.Any())
            {
                foreach (var policy in authorizeAttributesWithPolicies.Select(a => a.Policy))
                {
                    var authorized = await _identityService.AuthorizeAsync(_user.Id, policy);

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
