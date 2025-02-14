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
        string content = GenerateHtmlContent();
        File.WriteAllText(OutputPath, content);
        return true;
    }
    
    private string GenerateHtmlContent()
    {
        string templatePath = Path.Combine(ProjectDir, "wwwroot", "index.html");
        
        if (File.Exists(templatePath))
        {
            string template = File.ReadAllText(templatePath);
            
            template = template.Replace("{ENTROPY}", $"_{Entropy}");
            template = $"<!-- this is a generated file, do not modify manually. Changes will be lost.-->\n{template}";
            
            return template;
        }
        
        return $"""
                <!DOCTYPE html>
                <html>
                <body>
                <p>Missing file {templatePath}</p>
                </body>
                </html>
                """;
    }
}
