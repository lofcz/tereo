using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using TeReoLocalizer.Shared.Code.Services;

namespace TeReoLocalizer.Shared.Controllers;

public class LoginController : Controller
{
    public async Task<IActionResult> LoginAction(string projectId)
    {
        List<Claim> claimsList = LoginService.BuildClaims(projectId);
        ClaimsIdentity identity = new ClaimsIdentity(claimsList, "Identity", "auth", string.Empty);
        ClaimsPrincipal userPrincipal = new ClaimsPrincipal([identity]);
        
        await HttpContext.SignInAsync(userPrincipal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.Now.AddDays(365)
        });

        return Redirect("/localize");
    }
}