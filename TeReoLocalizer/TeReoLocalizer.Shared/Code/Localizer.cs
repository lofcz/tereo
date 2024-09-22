using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using TeReoLocalizer.Annotations;
using TeReoLocalizer.Shared.Components.Pages;

namespace TeReoLocalizer.Shared.Code;

public partial class Localizer(Decl decl, LangsData langsData)
{
    public async Task<string> Generate()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($$"""
                        // This code is automatically generated by TeReoLocalizer, do not edit manually. Changes will be overriden.
                        // Date generated: {{DateTime.Now}}
                        """);
        sb.AppendLine("using System.Globalization;");
        sb.AppendLine("using Languages = ScioSkoly.Priprava.Code.Languages;");
        sb.AppendLine();
        sb.AppendLine("namespace ScioSkoly.Priprava.I18N;");
        sb.AppendLine();
        sb.AppendLine("public class Resource2");
        sb.AppendLine("{");
        sb.AppendLine($$"""
                          public enum KnownLangs
                          {
                              Unknown,
                              {{DumpLangsEnum()}}
                          }

                          private Dictionary<string, string> currentLangDict;
                          private KnownLangs currentLang;
                          
                          public KnownLangs? Language { get; set; }
                          
                          public Resource2()
                          {
                              
                          }
                          
                          public Resource2(KnownLangs lang)
                          {
                              Language = lang;
                          }
                          
                          public Resource2(Languages? lang)
                          {
                              Language = GetLanguage(lang);
                          }
                          
                          public static KnownLangs? GetLanguage(Languages? language)
                          {
                              return language switch
                              {
                                  Languages.Czech => KnownLangs.CS,
                                  Languages.English => KnownLangs.EN,
                                  Languages.German => KnownLangs.DE,
                                  Languages.Polish => KnownLangs.PL,
                                  Languages.Spanish => KnownLangs.ES,
                                  Languages.Russian => KnownLangs.RU,
                                  _ => null
                              };
                          }
                          
                          private static readonly Dictionary<KnownLangs, Dictionary<string, string>> Data = new Dictionary<KnownLangs, Dictionary<string, string>>
                          {
                                {{DumpDictionaries()}}
                          };
                          
                          private static readonly Dictionary<int, KnownLangs> LcidDict = new Dictionary<int, KnownLangs>
                          {
                              {{DumpLcId()}}
                          };
                          
                          private string GetStringLocal(string key)
                          {
                              // if no known lang is set, use current culture
                              KnownLangs lang = Language ?? LcidDict.GetValueOrDefault(CultureInfo.CurrentUICulture.LCID, KnownLangs.CS);
                              
                              if (Data[lang].TryGetValue(key, out string? translated))
                              {
                                  return translated;
                              }
                          
                              return key;
                          }
                          
                          private static string GetString(string key)
                          {
                              KnownLangs lang = LcidDict.GetValueOrDefault(CultureInfo.CurrentUICulture.LCID, KnownLangs.CS);
                              return Data[lang].TryGetValue(key, out string? translated) ? translated : key;
                          }
                          
                          private static string GetString(string key, KnownLangs? knownLang)
                          {
                              KnownLangs lang = knownLang ?? LcidDict.GetValueOrDefault(CultureInfo.CurrentUICulture.LCID, KnownLangs.CS);
                              return Data[lang].TryGetValue(key, out string? translated) ? translated : key;
                          }
                          
                          private static string GetString(string key, Languages? knownLang)
                          {
                              KnownLangs lang = GetLanguage(knownLang) ?? LcidDict.GetValueOrDefault(CultureInfo.CurrentUICulture.LCID, KnownLangs.CS);
                              return Data[lang].TryGetValue(key, out string? translated) ? translated : key;
                          }
                          
                          {{DumpProps()}}
                      """);

        sb.AppendLine("}");

        string val = SimpleFormatCSharpCode(sb.ToString().Trim());
        return val;

        string DumpDictionaries()
        {
            StringBuilder dictBuilder = new StringBuilder();
            int dictIndex = 0;
            
            foreach (KeyValuePair<Languages, LangData> lang in langsData.Langs.OrderBy(x => x.Key))
            {
                dictBuilder.AppendLine("{");

                dictBuilder.AppendLine($"KnownLangs.{lang.Key}, new Dictionary<string, string>");
                dictBuilder.AppendLine("{");

                foreach (KeyValuePair<string, string> x in lang.Value.Data.OrderBy(x => x.Key))
                {
                    if (x.Key.IsNullOrWhiteSpace())
                    {
                        continue;
                    }
                    
                    dictBuilder.AppendLine($"{{ \"{x.Key.Trim()}\", \"{x.Value.Replace("\"", "\\\"")}\" }},");
                }
                
                dictBuilder.AppendLine("}");
                
                dictBuilder.AppendLine($"}}{(dictIndex < langsData.Langs.Count - 1 ? "," : string.Empty)}");
                dictIndex++;
            }
            
            return dictBuilder.ToString().Trim();
        }

        string DumpProps()
        {
            StringBuilder propsBuilder = new StringBuilder();

            foreach (KeyValuePair<string, Key> x in decl.Keys)
            {
                if (x.Key.IsNullOrWhiteSpace())
                {
                    continue;
                }
                
                propsBuilder.AppendLine($"public static string {CsIdentifier(x.Key)} => GetString(\"{x.Key.Trim()}\");");
            }

            propsBuilder.AppendLine();

            foreach (KeyValuePair<string, Key> x in decl.Keys)
            {
                if (x.Key.IsNullOrWhiteSpace())
                {
                    continue;
                }
                
                propsBuilder.AppendLine($"public static string Get{CsIdentifier(x.Key)}(Languages lang) {{ return GetString(\"{x.Key.Trim()}\", lang); }}");
            }
            
            propsBuilder.AppendLine();
            
            foreach (KeyValuePair<string, Key> x in decl.Keys)
            {
                if (x.Key.IsNullOrWhiteSpace())
                {
                    continue;
                }
                
                propsBuilder.AppendLine($"public string Local{CsIdentifier(x.Key)} => GetString(\"{x.Key.Trim()}\", Language);");
            }

            return propsBuilder.ToString().Trim();
        }

        string DumpLangsEnum()
        {
            StringBuilder langsSb = new StringBuilder();

            foreach (KeyValuePair<Languages, LangData> x in langsData.Langs.OrderBy(x => x.Key))
            {
                sb.AppendLine($"{x.Key.ToString()},");
            }
            
            return langsSb.ToString().Trim();
        }

        string DumpLcId()
        {
            StringBuilder lcidSb = new StringBuilder();
            
            foreach (KeyValuePair<Languages, LangData> x in langsData.Langs.OrderBy(x => x.Key))
            {
                if (LcDict.LcIds.TryGetValue(x.Key, out List<int>? iVals))
                {
                    foreach (int iVal in iVals)
                    {
                        sb.AppendLine($"{{ {iVal}, KnownLangs.{x.Key.ToString()} }},");   
                    }
                }
            }

            return lcidSb.ToString().Trim();
        }
    }

    public static string SimpleFormatCSharpCode(string code)
    {
        string[] lines = code.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        int indentLevel = 0;
        List<string> formattedLines = [];

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith('}'))
            {
                indentLevel--;
            }

            formattedLines.Add(new string(' ', indentLevel * 4) + trimmedLine);

            if (trimmedLine.EndsWith('{'))
            {
                indentLevel++;
            }
        }

        return string.Join(Environment.NewLine, formattedLines);
    }


    public static string CsIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "_";
        }

        string identifier = name.Trim();
        identifier = IdentRegex().Replace(identifier, "_");
        
        if (char.IsDigit(identifier[0]))
        {
            identifier = "_" + identifier;
        }
        
        if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(identifier)))
        {
            identifier = "@" + identifier;
        }

        return identifier;
    }

    [GeneratedRegex(@"[^\p{L}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]", RegexOptions.Compiled)]
    private static partial Regex IdentRegex();
}