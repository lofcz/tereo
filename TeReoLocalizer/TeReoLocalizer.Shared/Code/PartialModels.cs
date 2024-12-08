using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using TeReoLocalizer.Annotations;
using TeReoLocalizer.Shared.Components.Shared;

namespace TeReoLocalizer.Shared.Code;

public class Key
{
    public string Name { get; set; }
    public string Id { get; set; }
    public bool AutoTranslatable { get; set; } = true;
    public DateTime DateCreated { get; set; } = DateTime.Now;
    
    // dynamic
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

public class Project
{
    public static readonly int LatestVersionMajor = 1;
    public static readonly int LatestVersionMinor = 1;
    public static readonly int LatestVersionPatch = 0;
    
    public static string LatestVersion => $"{LatestVersionMajor}.{LatestVersionMinor}.{LatestVersionPatch}";

    public string SchemaVersion { get; set; } = LatestVersion;
    public List<Decl> Decls { get; set; } = [];

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

public class Decl
{
    public string Id { get; set; } = General.IIID();
    public string? Name { get; set; }
    [JsonConverter(typeof(SortedDictionaryConverter<string, Key>))]
    public ConcurrentDictionary<string, Key> Keys { get; set; } = [];
}

public class LangsData
{
    [JsonConverter(typeof(SortedDictionaryConverter<Languages, LangData>))]
    public ConcurrentDictionary<Languages, LangData> Langs { get; set; } = [];
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