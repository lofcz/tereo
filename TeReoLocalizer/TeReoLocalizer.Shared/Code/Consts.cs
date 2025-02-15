using System.Text.Json.Serialization;
using Json5Core;
using TeReoLocalizer.Shared.Components;

namespace TeReoLocalizer.Shared.Code;

public class Config
{
    [JsonIgnore]
    public bool ConfigReadOk { get; set; }
    public string? DeepL { get; set; }
    public bool Experimental { get; set; }
}

public enum AppType
{
    Web,
    Native
}

public static class Consts
{
    static Config? cfg;
    public static string Entropy { get; set; } = General.IIID();
    public static AppType AppType { get; set; } = AppType.Web;

    public static void UpdateConfigFromShared()
    {
        if (SharedProxy.IsMaui)
        {
            AppType = AppType.Native;
        }
    }
    
    public static Config Cfg
    {
        get
        {
            if (cfg is not null)
            {
                return cfg;
            }

            string? cfgSource = Linker.LinkFileContent("appCfg.json");

            if (cfgSource is not null)
            {
                if (!cfgSource.IsNullOrWhiteSpace())
                {
                    cfg = cfgSource.JsonDecode<Config>();
                }
            
                if (cfg != null)
                {
                    cfg.ConfigReadOk = true;
                    
                }

                return cfg ?? new Config();
            }
            
            cfgSource = Linker.LinkFileContent("appCfg.json5");

            if (!cfgSource.IsNullOrWhiteSpace())
            {
                try
                {
                    cfg = Json5.Deserialize<Config>(cfgSource);
                }
                catch (Exception e)
                {
                    
                }
            }
            
            if (cfg is not null)
            {
                cfg.ConfigReadOk = true;
            }

            return cfg ?? new Config();   
        }
    }

}