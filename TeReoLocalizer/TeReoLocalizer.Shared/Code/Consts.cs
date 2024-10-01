using System.Text.Json.Serialization;
using Json5Core;

namespace TeReoLocalizer.Shared.Code;

public class Config
{
    [JsonIgnore]
    public bool ConfigReadOk { get; set; }
    public string Repository { get; set; }
    public string Sln { get; set; }
    public string DeepL { get; set; }
    public bool Experimental { get; set; }
}

public class Consts
{
    private static Config? cfg;
    public static string Entropy { get; set; } = Code.General.IIID();
    
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