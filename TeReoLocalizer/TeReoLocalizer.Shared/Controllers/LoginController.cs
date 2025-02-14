using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using TeReoLocalizer.Shared.Code;
using TeReoLocalizer.Shared.Code.Services;

namespace TeReoLocalizer.Shared.Controllers;

public class LoginController : Controller
{
    public async Task<IActionResult> Login(string projectId)
    {
        string id = General.IIID();
        
        List<Claim> claimsList =
        [
            new Claim("id", id),
            new Claim("type", "ephemeral")
        ];

        ObserverService.SetUserData(id, new ObservedUser(projectId));
        
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