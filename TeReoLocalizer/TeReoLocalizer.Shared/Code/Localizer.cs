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
        sb.AppendLine("""
                      using System.Globalization;
                      using System.Collections.Frozen;
                      using Microsoft.AspNetCore.Components;
                      using System.CodeDom.Compiler;
                      using System.Runtime.CompilerServices;
                      using Languages = ScioSkoly.Priprava.Code.Languages;
                      """);
        sb.AppendLine();
        sb.AppendLine("namespace ScioSkoly.Priprava.I18N;");
        sb.AppendLine();
        sb.AppendLine($"[GeneratedCode(\"TeReoLocalizer\", \"1.0.0\")]");
        sb.AppendLine("public sealed class Reo");
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
                          
                          public Reo()
                          {
                              
                          }
                          
                          public Reo(KnownLangs lang)
                          {
                              Language = lang;
                          }
                          
                          public Reo(Languages? lang)
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
                          
                          private static readonly FrozenDictionary<KnownLangs, Dictionary<string, string>> FrozenData = Data.ToFrozenDictionary();
                          
                          private static readonly Dictionary<int, KnownLangs> LcidDict = new Dictionary<int, KnownLangs>
                          {
                              {{DumpLcId()}}
                          };
                          
                          private string GetStringLocal(string key)
                          {
                              // if no known lang is set, use current culture
                              KnownLangs lang = Language ?? GetLanguageInternal();
                              return FrozenData[lang].TryGetValue(key, out string? translated) ? translated : key;
                          }
                          
                          public static KnownLangs GetLanguage()
                          {
                              return LcidDict.GetValueOrDefault(CultureInfo.CurrentUICulture.LCID, KnownLangs.CS);
                          }
                          
                          [MethodImpl(MethodImplOptions.AggressiveInlining)]
                          private static KnownLangs GetLanguageInternal()
                          {
                              return LcidDict.GetValueOrDefault(CultureInfo.CurrentUICulture.LCID, KnownLangs.CS);
                          }
                          
                          public static string GetString(string key)
                          {
                              KnownLangs lang = GetLanguageInternal();
                              return FrozenData[lang].TryGetValue(key, out string? translated) ? translated : key;
                          }
                          
                          public static string GetString(string key, KnownLangs? knownLang)
                          {
                              KnownLangs lang = knownLang ?? GetLanguageInternal();
                              return FrozenData[lang].TryGetValue(key, out string? translated) ? translated : key;
                          }
                          
                          public static string GetString(string key, Languages? knownLang)
                          {
                              KnownLangs lang = GetLanguage(knownLang) ?? GetLanguageInternal();
                              return FrozenData[lang].TryGetValue(key, out string? translated) ? translated : key;
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

        string? DefaultLangValue(string propName)
        {
            return langsData.Langs[Languages.CS].Data.GetValueOrDefault(propName);
        }

        bool DefaultLangValueHtml(string propName)
        {
            return DefaultLangValue(propName).ProbablyContainsHtml();
        }

        void DumpPropXmlDoc(StringBuilder sbLocal, string propName)
        {
            string? x = DefaultLangValue(propName);
            
            if (!x.IsNullOrWhiteSpace())
            {
                sbLocal.AppendLine($$"""
                                     /// <summary>
                                     /// {{x}}
                                     /// </summary>
                                     """);   
            }
        }

        string DumpProps()
        {
            StringBuilder propsBuilder = new StringBuilder();

            foreach (KeyValuePair<string, Key> x in decl.Keys)
            {
                x.Value.DefaultLangContainsHtml = DefaultLangValueHtml(x.Key);
            }

            foreach (KeyValuePair<string, Key> x in decl.Keys)
            {
                if (x.Key.IsNullOrWhiteSpace())
                {
                    continue;
                }

                DumpPropXmlDoc(propsBuilder, x.Key);
                propsBuilder.AppendLine($"public static {(x.Value.DefaultLangContainsHtml ? "MarkupString" : "string")} {CsIdentifier(x.Key)} => {(x.Value.DefaultLangContainsHtml ? "(MarkupString)" : string.Empty)}GetString(\"{x.Key.Trim()}\");");

                if (x.Value.DefaultLangContainsHtml)
                {
                    propsBuilder.AppendLine($"public static string {CsIdentifier(x.Key)}String => GetString(\"{x.Key.Trim()}\");");
                }
            }

            propsBuilder.AppendLine();

            foreach (KeyValuePair<string, Key> x in decl.Keys)
            {
                if (x.Key.IsNullOrWhiteSpace())
                {
                    continue;
                }
                
                DumpPropXmlDoc(propsBuilder, x.Key);
                propsBuilder.AppendLine($"public static {(x.Value.DefaultLangContainsHtml ? "MarkupString" : "string")} Get{CsIdentifier(x.Key)}(Languages lang) {{ return {(x.Value.DefaultLangContainsHtml ? "(MarkupString)" : string.Empty)}GetString(\"{x.Key.Trim()}\", lang); }}");
            }
            
            propsBuilder.AppendLine();
            
            foreach (KeyValuePair<string, Key> x in decl.Keys)
            {
                if (x.Key.IsNullOrWhiteSpace())
                {
                    continue;
                }
                
                DumpPropXmlDoc(propsBuilder, x.Key);
                propsBuilder.AppendLine($"public {(x.Value.DefaultLangContainsHtml ? "MarkupString" : "string")} Local{CsIdentifier(x.Key)} => {(x.Value.DefaultLangContainsHtml ? "(MarkupString)" : string.Empty)}GetString(\"{x.Key.Trim()}\", Language);");
            }
            
            propsBuilder.AppendLine();
            
            foreach (KeyValuePair<string, Key> x in decl.Keys)
            {
                if (x.Key.IsNullOrWhiteSpace())
                {
                    continue;
                }
                
                DumpPropXmlDoc(propsBuilder, x.Key);
                propsBuilder.AppendLine($"public const string Key{CsIdentifier(x.Key)} = \"{x.Key.Trim()}\";");
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

    public static string BaseIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "_";
        }

        name = name.ToBaseLatin(false).Trim().FirstLetterToUpper().Replace("-", string.Empty);
        
        string[] words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string identifier = string.Join(string.Empty, words.Select(x => x.Length > 0 ? char.ToUpper(x[0]) + (x.Length > 1 ? x[1..] : string.Empty) : string.Empty));
        identifier = MyRegex().Replace(identifier, string.Empty);

        return identifier;
    }
    
    public static string CsIdentifier(string name)
    {
        string identifier = BaseIdentifier(name);
        
        if (identifier.Length > 0 && char.IsDigit(identifier[0]))
        {
            identifier = $"_{identifier}";
        }
        
        if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(identifier)))
        {
            identifier = $"@{identifier}";
        }

        return identifier;
    }

    [GeneratedRegex(@"[^\p{L}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}