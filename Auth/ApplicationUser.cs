using Microsoft.AspNetCore.Identity;

namespace ProdHelperService.Auth;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
