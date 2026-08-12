using System.ComponentModel.DataAnnotations;

namespace Cane360.Web.Models.Auth;

public sealed record RegisterRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);
