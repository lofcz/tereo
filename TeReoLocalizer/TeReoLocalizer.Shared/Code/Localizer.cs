using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using TeReoLocalizer.Annotations;

namespace TeReoLocalizer.Shared.Code;

public partial class Localizer(Project project, LangsData langsData)
{
    private CodegenResult result = new CodegenResult();
    
    private static string EscapeStringFast(string input)
    {
        StringBuilder sb = new StringBuilder(input.Length + 16); // some random reasonable offset, if we overflow it's ok
        
        foreach (char c in input)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\r': sb.Append("\\r"); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                case '\v': sb.Append("\\v"); break;
                case '\f': sb.Append("\\f"); break;
                default: sb.Append(c); break;
            }
        }
        
        return sb.ToString();
    }

    public async Task<CodegenResult> Generate()
    {
        result = new CodegenResult();
        Parallel.Invoke(GenerateBackend, GenerateFrontend);

        return result;
    }

    private string GetDeclModuleName(Decl decl)
    {
        return (decl.Settings.Codegen.FrontendStandaloneName ?? decl.Name ?? "Nepojmenovaná skupina").ToBaseLatin();
    }

    private void GenerateFrontend()
    {
        List<Decl> frontendDecls = project.Decls.Where(x => x.Settings.Codegen.Frontend).ToList();
        
        if (frontendDecls.Count is 0)
        {
            return;
        }
        
        Parallel.Invoke(
            () =>
            {
                result.Frontend.Declarations = GenerateDeclarationFile(frontendDecls);
            },
            () =>
            {
                result.Frontend.AmbientDeclarations = GenerateAmbientDeclarationFile();
            },
            () =>
            {
                result.Frontend.Mgr = GenerateMgrFile();
            },
            () =>
            {
                result.Frontend.Tsconfig = GenerateTsconfig();
            },
            () =>
            {
                result.Frontend.Map = GenerateKeysMap(frontendDecls);
            },
            () =>
            {
                List<Decl> baseDecls = frontendDecls.Where(x => !x.Settings.Codegen.FrontendStandalone).ToList();
 
                if (baseDecls.Count > 0)
                {
                    Parallel.ForEach(langsData.Langs, lang =>
                    {
                        result.Frontend.Decls[lang.Key] = GenerateLanguageFile(lang.Key, baseDecls);
                    });   
                }
            },
            () =>
            {
                List<Decl> standaloneDecls = frontendDecls.Where(x => x.Settings.Codegen.FrontendStandalone).ToList();
                
                if (standaloneDecls.Count > 0)
                {
                    Parallel.ForEach(standaloneDecls.SelectMany(x => langsData.Langs.Select(y => (Declaration: x, Language: y))), x =>
                    {
                        result.Frontend.StandaloneDecls[(x.Language.Key, GetDeclModuleName(x.Declaration))] = GenerateStandaloneLanguageFile(x.Language, x.Declaration);
                    });
                }
            }
        );
    }

    private readonly string AutogenComment = "// This code is automatically generated by TeReoLocalizer, do not edit manually. Changes will be overriden.";

    private CodegenTsTranspiledFile GenerateMgrFile()
    {
        return new CodegenTsTranspiledFile
        {
            Ts = $"""
                  {AutogenComment}
                  {Linker.LinkFileContent("wwwroot/Scripts/reoMgrProto.ts")}
                  """,
            Js = $"""
                  {AutogenComment}
                  {Linker.LinkFileContent("wwwroot/Scripts/reoMgrProto.js")}
                  """,
            Map = Linker.LinkFileContent("wwwroot/Scripts/reoMgrProto.js.map") ?? string.Empty
        };
    }
    
    private string GenerateTsconfig()
    {
        return Linker.LinkFileContent("wwwroot/Scripts/tsconfigProto.json") ?? string.Empty;
    }

    private string GenerateAmbientDeclarationFile()
    {
        return $"""
                {AutogenComment}
                {Linker.LinkFileContent("wwwroot/Scripts/reoAmbient.d.ts")}
                """;
    }

    private string GenerateKeysMap(List<Decl> decls)
    {
        Dictionary<string, HashSet<string>> moduleMap = [];

        foreach (Decl decl in decls)
        {
            bool isStandalone = decl.Settings.Codegen.FrontendStandalone;
            string moduleName = isStandalone ? GetDeclModuleName(decl) : "reo";
            
            if (!moduleMap.TryGetValue(moduleName, out _))
            {
                moduleMap[moduleName] = [];
            }
            
            foreach (KeyValuePair<string, Key> x in decl.Keys.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(x.Key))
                {
                    continue;
                }
            
                string key = x.Key.Trim();
                string propName = CsIdentifier(key);
                moduleMap[moduleName].Add(propName);
            }
        }
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");

        int i = 0;
        
        foreach (KeyValuePair<string, HashSet<string>> module in moduleMap.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            sb.Append($" \"{CsIdentifier(module.Key).FirstLetterToLower()}\": [");
            
            List<string> keys = module.Value.OrderBy(x => x, StringComparer.Ordinal).ToList();
            
            for (int j = 0; j < keys.Count; j++)
            {
                sb.Append($"\"{EscapeStringFast(keys[j])}\"");
                
                if (j < keys.Count - 1)
                {
                    sb.Append(", ");
                }
            }
        
            sb.Append(']');
        
            if (i < moduleMap.Count - 1)
            {
                sb.Append(',');
            }

            sb.AppendLine();
            i++;
        }

        sb.AppendLine("}");
    
        return sb.ToString().Trim();
    }

    
    private string GenerateDeclarationFile(List<Decl> decls)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(AutogenComment);
        sb.AppendLine("declare const reo: {");

        string[]? content = Linker.LinkFileContent("wwwroot/Scripts/reoMgrProto.d.ts", 3, 1);

        if (content is not null)
        {
            foreach (string line in content)
            {
                sb.AppendLine(line);
            }
        }
        
        foreach (Decl decl in decls)
        {
            bool isStandalone = decl.Settings.Codegen.FrontendStandalone;
            string moduleName = isStandalone ? GetDeclModuleName(decl) : "reo";
            
            foreach (KeyValuePair<string, Key> x in decl.Keys.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(x.Key))
                {
                    continue;
                }
                
                string key = x.Key.Trim();
                string propName = CsIdentifier(key);
                
                sb.AppendLine("     /**");
                
                if (langsData.Langs[Languages.CS].Data.TryGetValue(x.Key, out string? primaryVal))
                {
                    sb.AppendLine($"     * @description {primaryVal.Replace("*/", "*\\/")}");    
                }
                
                sb.AppendLine($"     * @note requires module \"{moduleName}\"");
                
                sb.AppendLine("     */");
                sb.AppendLine($"    {propName}: string,");
                sb.AppendLine($"    Key_{propName}: \"{EscapeStringFast(key)}\",");
            }
        }

        sb.AppendLine("}");
        return sb.ToString().Trim();
    }

    private string GenerateStandaloneLanguageFile(KeyValuePair<Languages, LangData> language, Decl decl)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");

        int i = 0;
            
        foreach (KeyValuePair<string, Key> item in decl.Keys.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (langsData.Langs[language.Key].Data.TryGetValue(item.Key, out string? value))
            {
                if (!string.IsNullOrWhiteSpace(item.Key))
                {
                    sb.Append($"    \"{item.Key.Trim()}\": \"{EscapeStringFast(value)}\"");

                    if (i < decl.Keys.Count - 1)
                    {
                        sb.Append(',');
                    }
                    
                    sb.AppendLine();
                    i++;
                }   
            }
        }
        
        sb.AppendLine("}");
        return sb.ToString().Trim();
    }

    private string GenerateLanguageFile(Languages language, List<Decl> decls)
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine("{");

        foreach (Decl decl in decls)
        {
            int i = 0;
            
            foreach (KeyValuePair<string, Key> item in decl.Keys.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                if (langsData.Langs[language].Data.TryGetValue(item.Key, out string? value))
                {
                    if (!string.IsNullOrWhiteSpace(item.Key))
                    {
                        sb.Append($"    \"{item.Key.Trim()}\": \"{EscapeStringFast(value)}\"");

                        if (i < decl.Keys.Count - 1)
                        {
                            sb.Append(',');
                        }
                    
                        sb.AppendLine();
                        i++;
                    }   
                }
            }
        }
        
        sb.AppendLine("}");
        return sb.ToString().Trim();
    }
    
    private void GenerateBackend()
    {
        List<Decl> backendDecls = project.Decls.Where(x => x.Settings.Codegen.Backend).ToList();

        if (backendDecls.Count is 0)
        {
            return;
        }
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(AutogenComment);
        sb.AppendLine($$"""
                        using System.Globalization;
                        using System.Collections.Frozen;
                        using Microsoft.AspNetCore.Components;
                        using System.CodeDom.Compiler;
                        using System.Runtime.CompilerServices;
                        using Languages = {{project.Settings.Codegen.Namespace}}.Code.Languages;
                        """); // todo: Microsoft.AspNetCore.Components is included for MarkupString support, we should detect whether the project is asp or not and codegen appropriately
        sb.AppendLine();
        sb.AppendLine($"namespace {project.Settings.Codegen.Namespace}.I18N;");
        sb.AppendLine();
        sb.AppendLine($"[GeneratedCode(\"TeReoLocalizer\", \"{project.SchemaVersion}\")]");
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
                          
                          {{DumpProps(backendDecls)}}
                      """);

        sb.AppendLine("}");

        result.Backend = SimpleFormatCSharpCode(sb.ToString().Trim());
        return;

        string DumpDictionaries()
        {
            StringBuilder dictBuilder = new StringBuilder();
            int dictIndex = 0;
            
            foreach (KeyValuePair<Languages, LangData> lang in langsData.Langs.OrderBy(x => x.Key))
            {
                dictBuilder.AppendLine("{");

                dictBuilder.AppendLine($"KnownLangs.{lang.Key}, new Dictionary<string, string>");
                dictBuilder.AppendLine("{");

                foreach (KeyValuePair<string, string> x in lang.Value.Data.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    if (x.Key.IsNullOrWhiteSpace())
                    {
                        continue;
                    }
                    
                    dictBuilder.AppendLine($"{{ \"{x.Key.Trim()}\", \"{EscapeStringFast(x.Value)}\" }},");
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
                                     /// {{EscapeStringFast(x)}}
                                     /// </summary>
                                     """);   
            }
        }

        string DumpProps(List<Decl> decls)
        {
            StringBuilder propsBuilder = new StringBuilder();

            foreach (Decl decl in decls)
            {
                foreach (KeyValuePair<string, Key> x in decl.Keys)
                {
                    x.Value.DefaultLangContainsHtml = DefaultLangValueHtml(x.Key);
                }

                List<KeyValuePair<string, Key>> sourceKeys = decl.Keys.OrderBy(x => x.Key.ToString(), StringComparer.Ordinal).ToList();

                foreach (KeyValuePair<string, Key> x in sourceKeys)
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

                foreach (KeyValuePair<string, Key> x in sourceKeys)
                {
                    if (x.Key.IsNullOrWhiteSpace())
                    {
                        continue;
                    }
                    
                    DumpPropXmlDoc(propsBuilder, x.Key);
                    propsBuilder.AppendLine($"public static {(x.Value.DefaultLangContainsHtml ? "MarkupString" : "string")} Get{CsIdentifier(x.Key)}(Languages lang) {{ return {(x.Value.DefaultLangContainsHtml ? "(MarkupString)" : string.Empty)}GetString(\"{x.Key.Trim()}\", lang); }}");
                }
                
                propsBuilder.AppendLine();
                
                foreach (KeyValuePair<string, Key> x in sourceKeys)
                {
                    if (x.Key.IsNullOrWhiteSpace())
                    {
                        continue;
                    }
                    
                    DumpPropXmlDoc(propsBuilder, x.Key);
                    propsBuilder.AppendLine($"public {(x.Value.DefaultLangContainsHtml ? "MarkupString" : "string")} Local{CsIdentifier(x.Key)} => {(x.Value.DefaultLangContainsHtml ? "(MarkupString)" : string.Empty)}GetString(\"{x.Key.Trim()}\", Language);");
                }
                
                propsBuilder.AppendLine();
                
                foreach (KeyValuePair<string, Key> x in sourceKeys)
                {
                    if (x.Key.IsNullOrWhiteSpace())
                    {
                        continue;
                    }
                    
                    DumpPropXmlDoc(propsBuilder, x.Key);
                    propsBuilder.AppendLine($"public const string Key{CsIdentifier(x.Key)} = \"{x.Key.Trim()}\";");
                }   
            }

            return propsBuilder.ToString().Trim();
        }

        string DumpLangsEnum()
        {
            StringBuilder langsSb = new StringBuilder();

            foreach (KeyValuePair<Languages, LangData> x in langsData.Langs.OrderBy(x => x.Key.ToString(), StringComparer.Ordinal))
            {
                sb.AppendLine($"{x.Key.ToString()},");
            }
            
            return langsSb.ToString().Trim();
        }

        string DumpLcId()
        {
            StringBuilder lcidSb = new StringBuilder();
            
            foreach (KeyValuePair<Languages, LangData> x in langsData.Langs.OrderBy(x => x.Key.ToString(), StringComparer.Ordinal))
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

    private static string SimpleFormatCSharpCode(string code)
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
        
        name = ValidHtmlTagRegex().Replace(name, string.Empty);
        name = name.ToBaseLatin(false).Trim().FirstLetterToUpper().Replace("-", string.Empty);
    
        string[] words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string identifier = string.Join(string.Empty, words.Select(x => x.Length > 0 ? char.ToUpper(x[0]) + (x.Length > 1 ? x[1..] : string.Empty) : string.Empty));
        identifier = UnicodeRegex().Replace(identifier, string.Empty);

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
    private static partial Regex UnicodeRegex();
    [GeneratedRegex("</?[a-zA-Z][^>]*>", RegexOptions.Compiled)]
    private static partial Regex ValidHtmlTagRegex();
}