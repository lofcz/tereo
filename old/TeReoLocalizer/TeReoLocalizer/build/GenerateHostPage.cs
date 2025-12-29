using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;

namespace TeReoLocalizer.build;

public class GenerateHostPage : Microsoft.Build.Utilities.Task
{
    [Required]
    public string OutputPath { get; set; }
    [Required]
    public string ProjectDir { get; set; }
    [Required]
    public string Entropy { get; set; }

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.High, $"Generating host page at {OutputPath}");
        
        string templatePath = Path.Combine(ProjectDir, "wwwroot", "index.html");
        string sharedTemplatePath = Path.Combine(ProjectDir, "..", "TeReoLocalizer.Shared", "wwwroot", "index.html");
        
        string? finalTemplatePath = File.Exists(templatePath) ? templatePath : File.Exists(sharedTemplatePath) ? sharedTemplatePath : null;

        if (finalTemplatePath is null)
        {
            Log.LogError($"Template file not found at {templatePath} or {sharedTemplatePath}");
            return false;
        }

        Log.LogMessage(MessageImportance.High, $"Using template at: {finalTemplatePath}");

        string content = GenerateHtmlContent(finalTemplatePath);
        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        File.WriteAllText(OutputPath, content);
        
        Log.LogMessage(MessageImportance.High, "Host page generated successfully");
        return true;
    }

    string GenerateHtmlContent(string templatePath)
    {
        string template = File.ReadAllText(templatePath);
        template = template.Replace("{ENTROPY}", $"_{Entropy}");
        
        #if RELEASE
        template = template.Replace("<!--{BASE_LINK}-->", "<base href=\"/\" />");
        #endif
        
        template = $"<!-- this is a generated file, do not modify manually. Changes will be lost.-->\n{template}";
        return template;
    }
}
