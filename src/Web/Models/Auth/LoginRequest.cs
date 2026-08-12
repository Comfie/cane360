using System.ComponentModel.DataAnnotations;

namespace Cane360.Web.Models.Auth;

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);
