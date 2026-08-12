using System.ComponentModel.DataAnnotations;

namespace Cane360.Web.Models.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
