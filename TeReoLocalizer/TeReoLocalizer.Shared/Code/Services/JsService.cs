using Microsoft.JSInterop;

namespace TeReoLocalizer.Shared.Code.Services;


public static class JsService
{
    public static async Task SafeCall(this IJSRuntime js, string ident, params object[] args)
    {
        await js.InvokeVoidAsync("mcf.callIfDefined", ident, args);
    }
    
    public static async void Call(this IJSRuntime js, string fn)
    {
        await js.InvokeVoidAsync("mcf.fn", fn);
    }

    public static async Task HideAllTooltips(this IJSRuntime js)
    {
        await js.InvokeVoidAsync("mcf.hideAllTooltips");
    }
    
    public static async Task Focus(this IJSRuntime js, string elId)
    {
        await js.InvokeVoidAsync("mcf.focus", elId);
    }

    public static async Task Scrollbar(this IJSRuntime js, string id)
    {
        await js.InvokeVoidAsync("mcf.fn", "Scrollbar", id);
    }
    
    public static async Task BodyToggleScroll(this IJSRuntime js, bool enabled)
    {
        await js.InvokeVoidAsync("mcf.bodyToggleScroll", enabled);
    }

    public static async Task LogError(this IJSRuntime js, Exception e)
    {
        await js.InvokeVoidAsync("mcf.log", $"L1: {e.Message} L2: {(e.InnerException?.Message)} L3: {(e.InnerException?.InnerException?.Message)} TRACE: {e.StackTrace}");
    }
    
    public static async Task Log(this IJSRuntime js, string msg)
    {
        await js.InvokeVoidAsync("mcf.log", msg);
    }
    
    public static async Task StoreError(this IJSRuntime js, Exception e)
    {
        await js.InvokeVoidAsync("mcf.storeError", e.ToJson());
    }

    public static async Task<int> GetErrorsInInterval(this IJSRuntime js, int secondsInterval)
    {
        return await js.InvokeAsync<int>("mcf.getErrosInInterval", secondsInterval);
    }
    
    public static async Task<bool> Copy(this IJSRuntime js, string text)
    {
        return await js.InvokeAsync<bool>("mcf.copy", text);
    }
    
    public static async Task<string?> GetUnauthorizedSocketId(this IJSRuntime js)
    {
        return await js.InvokeAsync<string>("mcf.getSocketId");
    }
    
    public static async Task Toast(this IJSRuntime js, ToastTypes type, string text, int timeOut = 5000)
    {
        await js.InvokeVoidAsync("mcf.toast", type.GetStringValue(), text, null, timeOut);
    }
    
    public static async Task SetCookie(this IJSRuntime js, string key, string value, int daysValid = 365)
    {
        await js.InvokeVoidAsync("mcf.setCookie", key, value, daysValid);
    }
    
    public static async Task ClearCookie(this IJSRuntime js, string key)
    {
        await js.InvokeVoidAsync("mcf.eraseCookie", key);
    }

    public static async Task<string?> GetCookie(this IJSRuntime js, string key)
    {
        return await js.InvokeAsync<string?>("mcf.getCookie", key);
    }
    
    public static async Task ToastClear(this IJSRuntime js)
    {
        await js.InvokeVoidAsync("mcf.toastClear");
    }

    public static async Task Download(this IJSRuntime js, string path, string name)
    {
        await js.InvokeVoidAsync("mcf.download", path, name);
    }
    
    public static async Task OpenInNewTab(this IJSRuntime js, string url)
    {
        await js.InvokeVoidAsync("mcf.openInNewTab", url);
    }
    
    public static async Task Toast(this IJSRuntime js, ToastTypes type, object? text)
    {
        await js.InvokeVoidAsync("mcf.toast", type.GetStringValue(), text?.ToString() ?? "null");
    }

    public static async void SetAppVersion(this IJSRuntime js, string versionEntropy)
    {
        await js.InvokeVoidAsync("mcf.setAppVersion", versionEntropy);
    }

    public static async Task DisposePersistentTooltips(this IJSRuntime js)
    {
        await js.InvokeVoidAsync("mcf.disposePersistentTooltips");
    }
    
    public static async Task InvokeVoidAsyncSafe(this IJSRuntime js, string identifier, string errorText, params object[] pars)
    {
        try
        {
            await js.InvokeVoidAsync(identifier, pars);
        }
        catch (Exception e)
        {
            await js.Toast(ToastTypes.Error, errorText);
        }
    }
    
    public static async Task Exec(this IJSRuntime js, string code)
    {
        await js.InvokeVoidAsync("mcf.eval", code);
    }
    
    public static async Task ScrollToBottom(this IJSRuntime js, bool smooth)
    {
        await js.InvokeVoidAsync("mcf.scrollToBottom", smooth);
    }
    
    public static async Task ScrollToTop(this IJSRuntime js, bool smooth)
    {
        await js.InvokeVoidAsync("mcf.scrollToTop", smooth);
    }
    
    public static async Task Reload(this IJSRuntime js)
    {
        await js.InvokeVoidAsync("mcf.reload");
    }
    
    public static async Task ToIndex(this IJSRuntime js)
    {
        await js.InvokeVoidAsync("mcf.navigateTo", "/");
    }
    
    public static async Task VselectResetFilters(this IJSRuntime js, string id)
    {
        await js.InvokeVoidAsync($"vselectApi_{id}.resetFilters");
    }

    public static async Task VselectClear(this IJSRuntime js, string id)
    {
        await js.InvokeVoidAsync($"vselectApi_{id}.clear");
    }
    
    public static async Task DisposeOldEvents(this IJSRuntime js)
    {
        bool x = await js.InvokeAsync<bool>("mcf.disposeSafeEvents");
        bool xx = x;
    }
    
    public static async Task Alert(this IJSRuntime js, string? text = null)
    {
        await js.InvokeVoidAsync("alert", text ?? $"test - nahodne IIID - {General.IIID()}");
    }
    
    public static async Task ToggleCheckbox(this IJSRuntime js, string id)
    {
        await js.InvokeVoidAsync("mcf.toggleCheckbox", id);
    }

    public static async Task RequireLib(this IJSRuntime js, IEnumerable<string> libs)
    {
        await js.InvokeAsync<Task>("mcf.requireLibArr", libs.ToCsv());
    }
}