using System.Text;

namespace TeReoLocalizer.Shared.Code.Services;

public static class InitService
{
    public static void Init()
    {
        EncodingProvider provider = CodePagesEncodingProvider.Instance;
        Encoding.RegisterProvider(provider);
    }
}