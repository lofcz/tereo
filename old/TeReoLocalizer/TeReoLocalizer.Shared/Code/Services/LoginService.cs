using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace TeReoLocalizer.Shared.Code.Services;

public class MauiAuthenticationStateProvider : AuthenticationStateProvider
{
    ClaimsPrincipal currentUser = new ClaimsPrincipal(new ClaimsIdentity());

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(currentUser));
    }

    public Task UpdateAuthenticationState(IEnumerable<Claim> claims)
    {
        ClaimsIdentity identity = new ClaimsIdentity(claims, "Identity", "auth", string.Empty);
        currentUser = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return Task.CompletedTask;
    }

    public Task ClearAuthenticationState()
    {
        currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return Task.CompletedTask;
    }
}

public class LoginService : ILoginService
{
    readonly AuthenticationStateProvider authStateProvider;
    readonly NavigationManager navigationManager;

    public LoginService(AuthenticationStateProvider authStateProvider, NavigationManager navigationManager)
    {
        this.authStateProvider = authStateProvider;
        this.navigationManager = navigationManager;
    }

    public static List<Claim> BuildClaims(string projectId)
    {
        string id = General.IIID();
		
        List<Claim> claims =
        [
            new Claim("id", id),
            new Claim("type", "ephemeral")
        ];
		
        ObserverService.SetUserData(id, new ObservedUser(projectId));
        return claims;
    }
    
    public async Task<bool> LoginAsync(string projectId)
    {
        switch (authStateProvider)
        {
            case MauiAuthenticationStateProvider customAuth:
            {
                await customAuth.UpdateAuthenticationState(BuildClaims(projectId));
                navigationManager.NavigateTo("/localize");
                return true;
            }
            case ServerAuthenticationStateProvider:
            {
                // since we can't write to the headers here, redirect to the controller for server config:
                navigationManager.NavigateTo($"/login/loginAction?projectId={projectId}", true);
                return true;
            }
        }

        return false;
    }
}