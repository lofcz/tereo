using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FastCloner.Code;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TeReoLocalizer.Annotations;
using TeReoLocalizer.Shared.Code.Services;
using TeReoLocalizer.Shared.Components.Pages;
using TeReoLocalizer.Shared.Components.Shared;

namespace TeReoLocalizer.Shared.Code;

public class Key
{
    public string Name { get; set; }
    public string Id { get; set; }
    public bool AutoTranslatable { get; set; } = true;
    public DateTime DateCreated { get; set; } = DateTime.Now;

    public Key()
    {
        
    }
    
    // dynamic
    [JsonIgnore]
    [FastClonerIgnore]
    public Decl Owner { get; set; }
    [JsonIgnore]
    public bool DefaultLangContainsHtml { get; set; }
    [JsonIgnore] 
    public bool IsVisible { get; set; } = true;
    [JsonIgnore]
    public int SearchPriority { get; set; }
}

public enum KeySearchModes
{
    Unknown,
    [StringValue("Obsahuje")]
    Contains,
    [StringValue("Přesná shoda")]
    Exact
}

public class TranslateTaskInput
{
    public string PrimaryValue { get; set; }
    public KeyValuePair<Languages, LangData> Source { get; set; }
    public bool Adding { get; set; }
    public KeyValuePair<string, Key> Key { get; set; }
}

public class CodegenSettings
{
    public string Namespace { get; set; }
}

public class ProjectSettings
{
    public CodegenSettings Codegen { get; set; } = new CodegenSettings();
    public TranslationProviders TranslationProviders { get; set; } = new TranslationProviders();
    public Languages PrimaryLanguage { get; set; }
}

public class TranslationProviders
{
    public TranslationProviderDeeplL DeepL { get; set; } = new TranslationProviderDeeplL();
}

public class TranslationProviderDeeplL
{
    public string? Context { get; set; }
}

public class Project
{
    public static readonly int LatestVersionMajor = 1;
    public static readonly int LatestVersionMinor = 3;
    public static readonly int LatestVersionPatch = 0;
    
    public static string LatestVersion => $"{LatestVersionMajor}.{LatestVersionMinor}.{LatestVersionPatch}";

    public string SchemaVersion { get; set; } = LatestVersion;
    public List<Decl> Decls { get; set; } = [];
    public ProjectSettings Settings { get; set; } = new ProjectSettings();

    public bool NeedsUpgrade => VersionMajor < LatestVersionMajor || VersionMinor < LatestVersionMinor || VersionPatch < LatestVersionPatch;
    
    // dynamic
    [JsonIgnore] 
    public Decl SelectedDecl { get; set; } = new Decl();
    [JsonIgnore]
    public int VersionMajor { get; set; }
    [JsonIgnore]
    public int VersionMinor { get; set; }
    [JsonIgnore]
    public int VersionPatch { get; set; }

    public Decl? GetDecl(string id) => Decls.FirstOrDefault(x => x.Id == id);
}

public enum DisplayPositions
{
    [StringValue("block")]
    Block,
    [StringValue("inline-block")]
    InlineBlock,
    [StringValue("flex")]
    Flex,
    [StringValue("inline-flex")]
    InlineFlex
}

public enum ButtonTypes
{
    Button,
    Submit
}

public class DynamicComponentInfo
{
    public Type Type { get; set; }
    public Dictionary<string, object?> Params { get; set; } = new Dictionary<string, object?>();
}

public interface ISelectActionModal
{
    public SelectActionModal OwnerModal { get; set; }
}


public enum ButtonFillModes
{
    [StringValue("")]
    Fill,
    [StringValue("outline")]
    Outline,
    [StringValue("href")]
    None
}

public enum ButtonSizes
{
    [StringValue("btn-lg")]
    Large,
    [StringValue("btn-md")]
    Medium,
    [StringValue("btn-sm")]
    Small,
    [StringValue("btn-exsm")]
    ExtraSmall,
    [StringValue("btn-md-old")]
    MediumFixed,
    [StringValue("btn-cta")]
    Cta
}

public enum TitleTypes
{
    Unknown,
    Index,
    App
}

public enum ButtonDesigns
{
    [StringValue("secondary")]
    Action,
    [StringValue("tertiary")]
    Cancel,
    [StringValue("success")]
    Success,
    [StringValue("danger")]
    Danger,
    [StringValue("secondary")]
    Secondary,
    [StringValue("successGreen")]
    GreenSuccess,
    [StringValue("secondaryOutlined")]
    SecondaryOutlined,
    [StringValue("primarySameWidth")]
    ActionSameWidth,
    [StringValue("successGreenSameWidth")]
    GreenSuccessSameWidth,
    [StringValue("orangeAction")]
    OrangeAction,
    [StringValue("whiteAction")]
    WhiteAction,
    [StringValue("orangeLightAction")]
    OrangeLightAction,
}

public enum Icons
{

}

public class Button
{
    public ButtonFillModes Fill { get; set; } = ButtonFillModes.Fill;
    public ButtonDesigns Design { get; set; } = ButtonDesigns.Action;
    public ButtonSizes Size { get; set; } = ButtonSizes.Medium;
    public string Text { get; set; }
    public string Tooltip { get; set; }
    public Func<Task<string>> TooltipFn { get; set; }
    public Func<Task>? OnClick { get; set; }
    public string? Icon { get; set; }
    public Icons? KnownIcon { get; set; }
    public string EphemeralId { get; set; } = General.IIID();
    
    public Button()
    {
        
    }
    
    public Button(string text)
    {
        Text = text;
    }
}

public enum ModalSizes
{
    [StringValue("modal-xl")]
    ExtraLarge,
    [StringValue("modal-lg")]
    Large,
    [StringValue("modal-md")]
    Medium,
    [StringValue("modal-sm")]
    Small
}

public interface IDescriptionModal : IComponent
{
    public Dictionary<string, object?> Params { get; set; }
}

public enum ModalRenderModes
{
    Auto,
    Manual
}

public interface IRenderOnceModal
{
    public bool? RenderOnce { get; set; }
}

public class ModalAction
{
    public static ModalAction Cancel { get; } = new ModalAction(new Button("Zrušit") { Design = ButtonDesigns.Cancel }, async () => { });
    public static ModalAction GenericCancel(string text) => new ModalAction(new Button(text) { Design = ButtonDesigns.Cancel }, async () => { });
    public static ModalAction GenericConfirm(string text) => new ModalAction(new Button(text) { Design = ButtonDesigns.Action }, async () => { });
    
    public Func<Task> Action { get; set; }
    public Button Button { get; set; }

    public ModalAction(Button button, Func<Task> action)
    {
        Button = button;
        Action = action;
    }
}

public interface IGenericModalRef
{
    public GenericModal? GenericModalRef { get; set; }
}

public class Decl : IEquatable<Decl>
{
    public string Id { get; init; } = General.IIID();
    public string? Name { get; set; }
    [JsonConverter(typeof(SortedDictionaryConverter<string, Key>))]
    public ConcurrentDictionary<string, Key> Keys { get; set; } = [];

    public DeclSettings Settings { get; set; } = new DeclSettings();

    public Decl()
    {
        
    }

    public Decl(string id)
    {
        Id = id;
    }
    
    public bool Equals(Decl? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        
        return obj.GetType() == GetType() && Equals((Decl)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }
}

public class DeclSettings
{
    public DeclSettingsCodegen Codegen { get; set; } = new DeclSettingsCodegen();
}

public class DeclSettingsCodegen
{
    public bool Frontend { get; set; }
    public bool Backend { get; set; } = true;
    public bool FrontendStandalone { get; set; }
    public string? FrontendStandaloneName { get; set; }
}

public class LangsData
{
    [JsonConverter(typeof(SortedDictionaryConverter<Languages, LangData>))]
    public ConcurrentDictionary<Languages, LangData> Langs { get; set; } = [];

    public bool SetKey(Languages language, string key, string value)
    {
        LangData lang = Langs[language];

        if (lang.FocusData.TryGetValue(key, out string? str))
        {
            if (str == value)
            {
                return false;
            }
        }
        
        lang.Data[key] = value;
        return true;
    }
}

public class LangData
{
    [JsonConverter(typeof(SortedDictionaryConverter<string, string>))]
    public ConcurrentDictionary<string, string> Data { get; set; } = [];
    [JsonIgnore] 
    public bool Visible { get; set; } = true;
    [JsonIgnore]
    public ConcurrentDictionary<string, string> PersistedData { get; set; } = [];
    [JsonIgnore]
    public ConcurrentDictionary<string, string> UncommitedChanges { get; set; } = [];
    [JsonIgnore]
    public ConcurrentDictionary<string, string> FocusData { get; set; } = [];
}

public enum ToastTypes
{
    [StringValue("ok")]
    Success,
    [StringValue("info")]
    Info,
    [StringValue("warning")]
    Warning,
    [StringValue("error")]
    Error
}

public enum RenderModes
{
    Unknown,
    [StringValue("Jednořádkový text")] 
    Input,
    [StringValue("Víceřádkový text")]
    Textarea
}
    
public enum TranslationModes
{
    Unknown,
    [StringValue("Výchozí")] 
    Default,
    [StringValue("Zneplatnění")] 
    Invalidate
}
    
public class UserSettings
{
    public List<Languages>? ShowLangs { get; set; }
    public RenderModes RenderMode { get; set; } = RenderModes.Input;
    public TranslationModes TranslationMode { get; set; } = TranslationModes.Default;
    public int LimitRender { get; set; } = 8;
    public string? SelectedDecl { get; set; }
    public KeySearchModes KeySearchMode { get; set; } = KeySearchModes.Contains;
    public Languages? KeySearchLang { get; set; }
    public bool AutoSave { get; set; } = true;
    public bool DisableTips { get; set; }
    public bool KeySearchAllGroups { get; set; }
        
    // dynamic
    [JsonIgnore]
    public bool ShowUncommitedChangesDetail { get; set; }
}

public class ProjectCtx
{
    public Project Project { get; set; }
    public Decl Decl { get; set; }
    public UserSettings Settings { get; set; }
    public IJSRuntime Js { get; set; }
    public Localize Owner { get; set; }
    public LangsData LangsData { get; set; }

    public ProjectCtx(Project project, Decl decl, UserSettings settings, LangsData langsData, IJSRuntime js, Localize owner)
    {
        Project = project;
        Decl = decl;
        Settings = settings;
        Js = js;
        Owner = owner;
        LangsData = langsData;
    }
}

public interface ICommand
{
    Task<DataOrException<bool>> Do(bool firstTime);
    Task Undo();
    public string GetName();
    public ProjectCtx Ctx { get; set; }
    public IProgress<CommandProgress>? Progress { get; set; }
}

public class ReverseCommand : ICommand
{
    private readonly ICommand originalCommand;

    public ReverseCommand(ICommand command)
    {
        originalCommand = command;
        Ctx = command.Ctx;
    }

    public async Task<DataOrException<bool>> Do(bool firstTime)
    {
        await originalCommand.Undo();
        return new DataOrException<bool>(true);
    }

    public async Task Undo()
    {
        await originalCommand.Do(false);
    }

    public string GetName() => $"Reverse of {originalCommand.GetName()}";
    
    public ProjectCtx Ctx { get; set; }
    public IProgress<CommandProgress>? Progress { get; set; }
}

public class HistoryItem
{
    public ICommand Command { get; }
    public int Index { get; }
    public bool Selected { get; set; }

    public HistoryItem(ICommand command, int index)
    {
        Command = command;
        Index = index;
    }
}

public abstract class BaseCommand : ICommand
{
    public ProjectCtx Ctx { get; set; } = default!;
    public virtual IProgress<CommandProgress>? Progress { get; set; }

    public Project Project => Ctx.Project;
    public Decl Decl => Ctx.Decl;
    public UserSettings Settings => Ctx.Settings;
    public IJSRuntime Js => Ctx.Js;
    public Localize Owner => Ctx.Owner;
    public LangsData LangsData => Ctx.LangsData;
    
    /// <summary>
    /// Performs an action.
    /// </summary>
    /// <returns>Whether the action executed. If false the action is discarded and not placed into the history.</returns>
    public abstract Task<DataOrException<bool>> Do(bool firstTime);

    /// <summary>
    /// Reverts an action
    /// </summary>
    /// <returns></returns>
    public abstract Task Undo();

    public virtual string GetName()
    {
        return "Nepojmenovaná akce";
    }

    public override string ToString()
    {
        return GetName();
    }
}

public class CommandHistory
{
    public List<HistoryItem> Before { get; }
    public List<HistoryItem> After { get; }

    public CommandHistory(List<HistoryItem> before, List<HistoryItem> after)
    {
        Before = before;
        After = after;
    }
}

public enum ExceptionTypes
{
    Unknown,
    Unhandled,
    Handled
}

public class ExecResult : ICrudResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public int AffectedRows { get; set; }
}

public class DeleteResult : ICrudResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public int AffectedRows { get; set; }
}

public class ReadResult<T> : ICrudResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public T? Data { get; set; }
}

public class UpdateResult : ICrudResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public int AffectedRows { get; set; }
}

public class UpdateResult<T> : UpdateResult
{
    public T? Data { get; set; }
    
    public UpdateResult(string errorMsg)
    {
        Error = errorMsg;
    }
    
    public UpdateResult(T data)
    {
        Ok = true;
        Data = data;
    }
}

public class UpdateResult<T, T2> : UpdateResult
{
    public T? Data { get; set; }
    public T2? Data2 { get; set; }
}

public interface ICrudResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
}

public class CreateResult : ICrudResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }

    public CreateResult()
    {
        
    }

    public CreateResult(string errorMsg)
    {
        Error = errorMsg;
    }
}

public class CreateResult<T> : ICrudResult where T : class
{
    public T? Entity { get; set; }
    public bool Ok { get; set; }
    public string? Error { get; set; }

    public CreateResult()
    {
        
    }

    public CreateResult(string errorMsg)
    {
        Error = errorMsg;
    }

    public CreateResult(T entity)
    {
        Entity = entity;
        Ok = true;
    }
}

public class DataOrException<T>
{
    [MemberNotNullWhen(false, nameof(Exception))]
    private bool DataIsNotNull => Data is not null;

    [MemberNotNullWhen(false, nameof(Data))]
    private bool ExceptionIsNotNull => Exception is not null;

    public T? Data { get; set; }
    public Exception? Exception { get; set; }
    public ExceptionTypes ExceptionType { get; set; }

    public DataOrException(T? data)
    {
        Data = data;
    }

    public DataOrException(Exception? exception, ExceptionTypes type = ExceptionTypes.Unhandled)
    {
        Exception = exception;
        ExceptionType = type;
    }

    public static implicit operator DataOrException<T>(UpdateResult<T> data) => data.Error is not null ? new DataOrException<T>(new Exception(data.Error ?? "[neznámá chyba]")) : new DataOrException<T>(data.Data);
}

public enum RewindActions
{
    Unknown,
    Undo,
    Redo
}

public enum KeyRenameReasons
{
    Unknown,
    Manual,
    Regenerate
}

public class CodegenResult
{
    public string? Backend { get; set; }
    public CodegenFrontendResult Frontend { get; set; } = new CodegenFrontendResult();
}

public class CodegenFrontendResult
{
    public string? Declarations { get; set; }
    public string? AmbientDeclarations { get; set; }
    public CodegenTsTranspiledFile? Mgr { get; set; }
    public string? Tsconfig { get; set; }
    public string? Map { get; set; }
    public ConcurrentDictionary<Languages, string> Decls { get; set; } = [];
    public ConcurrentDictionary<(Languages language, string decl), string> StandaloneDecls { get; set; } = [];
}

public class CodegenTsTranspiledFile
{
    public string Ts { get; set; }
    public string Js { get; set; }
    public string Map { get; set; }
}

public class DeeplifiedText
{
    public string Text { get; set; }
    public Dictionary<string, string> Placeholders { get; set; }
}

public class ProjectError
{
    public string Message { get; set; }

    public ProjectError(string error)
    {
        Message = error;
    }
}

public class NativeCommand
{
    public NativeCommands Type { get; set; }
    public object? Data { get; set; }
}

public enum NativeCommands
{
    Unknown,
    SetTextareaHeight
}