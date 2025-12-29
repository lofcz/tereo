using BlazingModal;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TeReoLocalizer.Shared.Code;
using TeReoLocalizer.Shared.Code.Commands;

namespace TeReoLocalizer.Shared.Components.Pages;

public partial class Localize
{
    [Parameter]
    public string? Id { get; set; }
    
    public Project Project = new Project();
    public Decl Decl => Project.SelectedDecl;
    public UserSettings Settings = new UserSettings();
    public readonly LangsData LangsData = new LangsData();
    public string NewKey = string.Empty;
    public string? InputToFocus;
    public string? LastFocusedInput { get; set; }
    public string? LastAddedKey { get; set; }
    public string KeySearch = string.Empty;
    public readonly CommandManager CommandManager = new CommandManager();
    public bool JumpingInHistory { get; set; }
    public bool EnableRendering { get; set; } = true;
    public IModalReference? RewindModalRef;
    public bool Translating;
    public int Translated;
    public List<TranslateTaskInput> ToTranslate = [];
    InvertedIndex Index => Program.Index;

    DotNetObjectReference<Localize>? JsRef { get; set; }
    IJSObjectReference? JsObjectRef;
    IJSObjectReference? JsObjectRefNative;

    string langCode = string.Empty;
    string basePath = AppDomain.CurrentDomain.BaseDirectory;
    List<string> existingFiles = [];
    bool showLangs, showLangsSelection, showAddLangs, showGroupSettings;
    bool ready;
    bool synchronizingIndex;
    string id = General.IIID();
    bool jsInitialized;
    List<NativeCommand> nativeCommands = [];
    List<KeyValuePair<string, Key>> visibleKeys = [];
    int keyPages = -1;
    int keySelectedPage = 1;
    ElementReference? jumpToPageInput;
    bool renderSearchModeSelection;
    List<ProjectError>? loadErrors;
    bool showLoadErrors = true;
    bool projectLoadingFinished;
    bool contentRendered;
    BootDataProject? openProject;
    bool Panicked => loadErrors?.Count > 0;
}