using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using TeReoLocalizer.Shared.Components.Shared;
using TeReoLocalizer.Shared.Components.Shared.Internal;

namespace TeReoLocalizer.Shared.Code;

public enum FlexCol
{
    Col1 = 1,
    Col2 = 2,
    Col3 = 3,
    Col4 = 4,
    Col5 = 5, 
    Col6 = 6,
    Col7 = 7,
    Col8 = 8,
    Col9 = 9,
    Col10 = 10,
    Col11 = 11,
    Col12 = 12
}

public class FlexColumns
{
    public static FlexColumns Desktop12 = new FlexColumns(FlexCol.Col12);
    public static FlexColumns Desktop11 = new FlexColumns(FlexCol.Col11);
    public static FlexColumns Desktop10 = new FlexColumns(FlexCol.Col10);
    public static FlexColumns Desktop9 = new FlexColumns(FlexCol.Col9);
    public static FlexColumns Desktop8 = new FlexColumns(FlexCol.Col8);
    public static FlexColumns Desktop7 = new FlexColumns(FlexCol.Col7);
    public static FlexColumns Desktop6 = new FlexColumns(FlexCol.Col6);
    public static FlexColumns Desktop5 = new FlexColumns(FlexCol.Col5);
    public static FlexColumns Desktop4 = new FlexColumns(FlexCol.Col4);
    public static FlexColumns Desktop3 = new FlexColumns(FlexCol.Col3);
    public static FlexColumns Desktop2 = new FlexColumns(FlexCol.Col2);
    public static FlexColumns Desktop1 = new FlexColumns(FlexCol.Col1);
    public static FlexColumns Desktop6Mobile12 = new FlexColumns(FlexCol.Col6);
    public static FlexColumns Desktop6Center = new FlexColumns(FlexCol.Col6, FlexCol.Col3);
    public static FlexColumns Desktop8Center = new FlexColumns(FlexCol.Col8, FlexCol.Col2);
    public static FlexColumns Desktop10Center = new FlexColumns(FlexCol.Col10, FlexCol.Col1);
    public static FlexColumns Desktop3Offset9 = new FlexColumns(FlexCol.Col3, FlexCol.Col9);
    
    public static FlexColumns Desktop2Medium6 = new FlexColumns { Lg = FlexCol.Col2, Md = FlexCol.Col6, Sm = FlexCol.Col12 };
    public static FlexColumns Desktop3Medium6 = new FlexColumns { Lg = FlexCol.Col3, Md = FlexCol.Col6, Sm = FlexCol.Col12 };

    public FlexCol? Default { get; set; }
    public FlexCol? OffsetDefault { get; set; }
    public FlexCol? Sm { get; set; }
    public FlexCol? Md { get; set; }
    public FlexCol? Lg { get; set; }

    public FlexColumns()
    {
        
    }
    
    public FlexColumns(FlexCol @default)
    {
        Default = @default;
    }
    
    public FlexColumns(FlexCol @default, FlexCol offsetDefault)
    {
        Default = @default;
        OffsetDefault = offsetDefault;
    }

    public string Serialize()
    {
        StringBuilder sb = new StringBuilder();
        
        if (Default != null)
        {
            sb.Append($"col-md-{(int) Default}");
        }
        else
        {
            
            sb.Append($"col-sm-{(int) Sm} col-md-{(int) Md} col-lg-{(int) Lg} col-xl-{(int) Lg}");
        }

        sb.Append(' ');
        
        if (OffsetDefault != null)
        {
            sb.Append($"offset-md-{(int)OffsetDefault}");
        }
        
        return sb.ToString();
    }
}

public interface IVirtualInputComponent
{
    public Task Clear();
    public string JsFilterProperty { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public T? GetValue<T>();
    public Task SetValue(object? value);
    public Task SyncData(bool freshRender, object? selected, bool instantRender);
    public void StateHasChangedPublic();
}

public enum LabelPositions
{
    Above,
    Before,
    After,
    Below
}

public interface IInputComponent : IVirtualInputComponent
{
    public dynamic? GroupValue { get; set; }
    public void SetValueFromGroup(object? value);
    public IList? FetchedOptions { get; set; }
    public string SerializeRow();
    public string Style { get; set; }
    public bool RenderLabel { get; set; }
    public LabelPositions ComputedLabelPosition { get; }
    public bool DecorateLabel { get; set; }
    public EdForm? Context { get; set; }
    public string Label { get; set; }
    public FlexColumns? ComputedLabelSize { get; }
    public string ComputedInputSize { get; }
    public string Placeholder { get; set; }
    public TextAligns TextAlign { get; set; }
    public bool ForceRenderLabel { get; set; }
    public string? Class { get; set; }
    public string? ColumnClass { get; set; }
    public string? LabelStyle { get; set; }
    public NavigationManager? Nm { get; set; }
}


public enum TextAligns
{
    [StringValue("left")]
    Left,
    [StringValue("center")]
    Center,
    [StringValue("right")]
    Right,
    Inherit
}

public class RowChildComponent : ComponentBaseEx
{
    [CascadingParameter(Name = "EdRowShape")]
    public FlexColumns? EdRowShape { get; set; }
}

public class DataFetch
{
    public Providers Provider { get; set; }
    /// <summary>
    /// Expects <see cref="ISelectOption"/> as IList T
    /// </summary>
    public Func<Task<IList?>?>? FetchAsync { get; set; }
    /// <summary>
    /// Expects <see cref="ISelectOption"/> as IList T
    /// </summary>
    public Func<IList?>? FetchSync { get; set; }
    /// <summary>
    /// Expects <see cref="ISelectOption"/> as IList T
    /// </summary>
    public IList? Data { get; set; }
    public bool HasData { get; set; }
    
    public DataFetch(Providers provider, Func<Task<IList?>?> fetch)
    {
        Provider = provider;
        FetchAsync = (Func<Task<IList?>?>?)fetch;
    }
    
    public DataFetch(Providers provider, Func<IList?> fetch)
    {
        Provider = provider;
        FetchSync = fetch;
    }
    
    public DataFetch(Providers provider, IList? data)
    {
        Provider = provider;
        Data = data;
        HasData = true;
    }
    
    public static implicit operator DataFetch(List<ISelectOption> data) => new DataFetch(Providers.ServerOnceSelfHandle, () => data);
    public static implicit operator DataFetch(List<NativeSelectOption> data) => new DataFetch(Providers.ServerOnceSelfHandle, () => data);
    public static implicit operator DataFetch(List<DescriptionSelectOption> data) => new DataFetch(Providers.ServerOnceSelfHandle, () => data);
}

public enum Providers
{
    ServerOnce,
    ServerFilter,
    ServerOnceBatch,
    ServerOnceSelfHandle
}

public class IdSelectOption : NativeSelectOption
{
    public object? Id { get; set; }
}

public class ParentSelectOption : NativeSelectOption
{
    public int ParentId { get; set; }
    public int ParentIndex { get; set; }
}

public interface IDescriptionOption
{
    public string? Description { get; set; }
}

public class DescriptionSelectOption : NativeSelectOption, IDescriptionOption
{
    public string? Description { get; set; }
}


public class NativeSelectOption : ISelectOption
{
    public string Name { get; set; }
    public dynamic? Value { get; set; }
    public bool Selected { get; set; }
}

public interface ISelectOption
{
    public string Name { get; set; }
    public dynamic? Value { get; set; }
    public bool Selected { get; set; }
}

public interface IDataFetchComponent
{
    public DataFetch? Fetch { get; set; }
    public TaskAggregator? TaskAggregator { get; set; }
    public string RandomKey { get; set; }
}

public class TaskAggregator
{
    private Dictionary<string, bool> Tasks = new Dictionary<string, bool>();
    private Action? TasksReadySync { get; set; }
    private Func<Task>? TasksReadyAsync { get; set; }
    private bool CallbackCalled { get; set; }

    public TaskAggregator(Action tasksReady)
    {
        TasksReadySync = tasksReady;
    }
    
    public TaskAggregator(Func<Task> tasksReady)
    {
        TasksReadyAsync = tasksReady;
    }

    public bool AllCompleted => !Tasks.ContainsValue(false);
    
    public void Enlist(string cmp)
    {
        Tasks.TryAdd(cmp, false);
    }
    
    public async Task AnnouceTaskCompleted(string cmp)
    {
        Tasks[cmp] = true;

        if (AllCompleted && !CallbackCalled)
        {
            CallbackCalled = true;
            
            if (TasksReadyAsync != null)
            {
                await TasksReadyAsync.Invoke();
            }
            else
            {
                TasksReadySync?.Invoke();
            }
        }
    }
}

public class FormUpdateArgs
{
    public IVirtualInputComponent Sender { get; set; }
    public object? Value { get; set; }
    public object? CacheValue { get; set; }

    public FormUpdateArgs(IVirtualInputComponent sender, object? value)
    {
        Sender = sender;
        Value = value;
        CacheValue = value;
    }
}

public enum RowMargins
{
    [StringValue("")]
    None,
    [StringValue("row-margin-bottom-single")]
    Single,
    [StringValue("row-margin-bottom-double")]
    Double
}

public interface IEdInputGroup
{
    public Task ToggleItem(object? item);
    public void Enlist(IInputComponent cmp);
    Task SetOptions<T>(IEnumerable<T> options);
    public string Id { get; set; }
}

public enum TweakTypes
{
    Toggle
}

public class TweakItem
{
    public TweakTypes Type { get; set; }
}

public class TweakConfig
{
    public List<TweakItem> Items { get; set; } = new List<TweakItem>();
}

public class Ref<T>
{
    public T? Value { get; set; }
}

public class LabelToolRender
{
    public bool Enabled { get; set; }
    public string? Tooltip { get; set; }
    public Icons? Icon { get; set; }
    public string? Text { get; set; }

    public LabelToolRender(bool enabled)
    {
        Enabled = enabled;
    }
}
    
public class LabelTool
{
    public Func<IInputComponent, Task<LabelToolRender>> Render { get; set; }
    /// <summary>
    /// Whether to re-render the tool
    /// </summary>
    public Func<IInputComponent, Task<bool>> OnClick { get; set; }
    
    /// <summary>
    /// Do not set manually, handled by <see cref="InternalEdInputLabel{TValue}"/>
    /// </summary>
    public LabelToolRender? LastRenderResult { get; set; }

    public LabelTool(Func<IInputComponent, Task<LabelToolRender>> renderFn, Func<IInputComponent, Task<bool>> onClick)
    {
        Render = renderFn;
        OnClick = onClick;
    }
}

public class AuthComponent : ComponentBaseEx
{
    [CascadingParameter]
    private Task<AuthenticationState>? authenticationStateTask { get; set; }
    public ClaimsPrincipal? User { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        await GetAuthState();
        await base.OnInitializedAsync();
    }

    protected async Task GetAuthState()
    {
        if (authenticationStateTask != null)
        {
            User = (await authenticationStateTask).User;
        }
    }
}

public interface IInputComponentMultivalue<T>
{
    public T GetScalarValue();
    public List<T> GetValues();
    public bool Multiple { get; set; }
}

public class InputComponent<T> : RowChildComponent, IInputComponent, IDataFetchComponent
{
    [CascadingParameter]
    public TaskAggregator? TaskAggregator { get; set; }

    public string RandomKey { get; set; } = General.IIID();

    [Parameter]
    public string? Placeholder { get; set; }
    [Parameter]
    public DataFetch? Fetch { get; set; }
    public IList? FetchedOptions { get; set; }
    [Parameter]
    public string? Label { get; set; }
    [Parameter]
    public string? RowFrameStyle { get; set; }
    [Parameter] 
    public TextAligns TextAlign { get; set; } = TextAligns.Inherit;
    [Parameter]
    public bool Inline { get; set; }
    /// <summary>
    /// Called whenever the user attempts to submit the form from this input, typically with Enter
    /// </summary>
    [Parameter]
    public Func<Task<bool>>? OnSubmit { get; set; }
    [Parameter]
    public string? LabelUnderline { get; set; }
    [Parameter]
    public string? LabelUnderlineStyle { get; set; }
    
    [CascadingParameter]
    public EdForm? Context { get; set; }

    private T? _value;
    protected bool ignoreJsSignals = false;

    public virtual void OnValueSet(T? newVal)
    {
        
    }
    
    [Parameter]
    public T? Value {
        get => _value;

        set
        {
            _value = value;
            OnValueSet(value);
        }
    }
    [Parameter]
    public bool Disabled { get; set; }
    [Parameter] 
    public string Name { get; set; } = General.IIID();
    [Parameter] 
    public bool Autocomplete { get; set; } = true;
    [Parameter]
    public FlexColumns? Shape { get; set; }
    [Parameter]
    public RowMargins? RowMargin { get; set; }
    [Parameter]
    public bool IgnoreGrid { get; set; }
    [Parameter]
    public Func<Task>? OnReady { get; set; }
    [Parameter]
    public Action<IInputComponent>? OnSetup { get; set; }
    [Parameter]
    public string? JsFilterProperty { get; set; }
    [Parameter]
    public dynamic? GroupValue { get; set; }
    [Parameter]
    public Func<IInputComponent, Task>? OnUpdate { get; set; }
    [Parameter]
    public Func<IInputComponent, Task>? OnBlur { get; set; }
    [Parameter]
    public string Id { get; set; } = General.IIID();
    [Parameter]
    public bool ForceRenderLabel { get; set; }
    [CascadingParameter(Name = "OnFormUpdate")]
    public Func<FormUpdateArgs, Task>? OnFormUpdate { get; set; }
    [CascadingParameter(Name = "EdFormShape")]
    public FlexColumns? EdFormShape { get; set; }
    [CascadingParameter(Name = "IgnoreGridCascaded")]
    public bool IgnoreGridCascaded { get; set; }
    [CascadingParameter(Name = "Group")]
    public IEdInputGroup? Group { get; set; }
    [Parameter]
    public TweakConfig? Tweaks { get; set; }
    [Parameter] 
    public bool RenderRow { get; set; } = true;
    [Parameter]
    public EventCallback<T> ValueChanged { get; set; }
    [Parameter] 
    public bool RenderLabel { get; set; } = true;
    [Parameter]
    public LabelPositions? LabelPosition { get; set; }
    [Parameter] 
    public bool DecorateLabel { get; set; }
    [Parameter]
    public Ref<IInputComponent>? Ref { get; set; }
    [Parameter]
    public List<LabelTool>? LabelTools { get; set; }
    [Parameter]
    public string? ColumnClass { get; set; }
    [Parameter] 
    public bool EnableValidation { get; set; } = true;
    [Parameter]
    public string? LabelStyle { get; set; }

    public NavigationManager? Nm { get; set; }

    [CascadingParameter(Name = "FormLabelPosition")]
    public LabelPositions? FormLabelPosition { get; set; }

    private bool announcedReady;
    public LabelPositions ComputedLabelPosition => LabelPosition ?? FormLabelPosition ?? LabelPositions.Above;
    public FlexColumns ComputedLabelSize => FlexColumns.Desktop3;
    public string ComputedInputSize => ComputedLabelPosition is LabelPositions.After or LabelPositions.Before ? FlexColumns.Desktop9.Serialize() : "";
    
    [JSInvokable]
    public async Task AnnounceReady()
    {
        if (OnReady != null && !announcedReady)
        {
            announcedReady = true;
            await OnReady.Invoke();
        }
    }

    public async Task<bool> Submit()
    {
        if (Context is not null)
        {
            return await Context.Submit();
        }

        return false;
    }

    public virtual async Task SetValue(object? value)
    {
        if (value is T tVal)
        {
            Value = tVal;
            StateHasChanged();

            // select has it's own handling as it impls multival
            if (this is ISelectInput)
            {
                return;
            }
            
            await ValueChanged.InvokeAsync(Value);
        
            if (TaskAggregator != null)
            {
                await TaskAggregator.AnnouceTaskCompleted(RandomKey);
            }
        }
    }

    public TInput? GetValue<TInput>()
    {
        if (this is IInputComponentMultivalue<T> multivalue)
        {
            if (typeof(TInput) == typeof(List<T>))
            {
                return (TInput?)multivalue.GetValues().ChangeType(typeof(TInput));
            }

            if (typeof(TInput) == typeof(T))
            {
                if (multivalue.Multiple)
                {
                    return (TInput?) multivalue.GetValues().FirstOrDefault().ChangeType(typeof(TInput));   
                }
                
                return (TInput?) multivalue.GetScalarValue().ChangeType(typeof(TInput));
            }

            if (typeof(T) == typeof(int))
            {
                return (TInput?)multivalue.GetValues().ChangeType(typeof(TInput));
            }
        }
        
        if (typeof(TInput) == typeof(T))
        {
            return (TInput?) Value.ChangeType(typeof(TInput));
        }

        return default;
    }
    
    [JSInvokable]
    public virtual async Task AttemptSubmitJs()
    {
        await Submit();
    }

    public void SetValueFromGroup(object? value)
    {
        if (value is T tVal)
        {
            Value = tVal;
            StateHasChanged();
        }
    }
    
    private void EnlistTask()
    {
        TaskAggregator?.Enlist(RandomKey);
    }

    public virtual async Task SyncData(bool freshRender, object? selected, bool instantRender)
    {
        
    }
    
    public virtual async Task Clear()
    {
        Value = default;
        StateHasChanged();
    }

    void EnlistForm()
    {
        if (Context != null)
        {
            Context.Inputs ??= new Dictionary<string, IVirtualInputComponent>();
            Context.Inputs.AddOrUpdate(Name, this);
        }
    }

    protected override void OnParametersSet()
    {
        EnlistForm();
        
        if (Ref != null)
        {
            Ref.Value = this;
        }
        
        base.OnParametersSet();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        if (Ref is not null)
        {
            Ref.Value = this;
        }
        
        EnlistForm();

        if (Fetch is not null)
        {
            EnlistTask();   
        }

        Group?.Enlist(this);
        OnSetup?.Invoke(this);
    }
    
    public string SerializeRow()
    {
        if (IgnoreGrid || IgnoreGridCascaded)
        {
            return "";
        }
        
        return $"{(this is EdCheckbox ? "" : (Shape?.Serialize() ?? EdRowShape?.Serialize() ?? EdFormShape?.Serialize() ?? "col-md-12"))} form-group {RowMargin?.GetStringValue() ?? Context?.RowMargin.GetStringValue() ?? RowMargins.Single.GetStringValue()}";
    }
}

public enum TextareaTypes
{
    /// <summary>
    /// Native textarea. Max content length is limited to 15k chars. Use <see cref="NativeLargeText"/> if you need to support more than that.
    /// </summary>
    Native,
    /// <summary>
    /// Backed by WYSIWYG JS library. Does not support two way binding. 
    /// </summary>
    RTE,
    /// <summary>
    /// Not implemented. Probably Monaco based.
    /// </summary>
    CodeEditor,
    /// <summary>
    /// Supports more than 15k chars, does not support two-way binding.
    /// </summary>
    NativeLargeText
}

public enum TextareaHeights
{
    [StringValue("small")]
    [StringValue("2", "rows")]
    Small,
    [StringValue("")]
    [StringValue("3", "rows")]
    Medium,
    [StringValue("large")]
    [StringValue("4", "rows")]
    Large
}

public enum FormTypes
{
    Default,
    Inline
}


public interface ISelectInput
{
    public Task SetOptions(IList? options);
    public Task SetOptionsWithoutDataReset(IList? options);
    public Task SetValue(object? value);
    public Task SignalFetchedDataReady();
    public IList? FetchedOptions { get; set; }
    public Task SyncPlaceholder(string placeholder);
}

public interface IValidationComponent : IInputComponent
{
    public bool RenderValidation { get; set; }
    public string? ValidationMsg { get; set; }
    public Task<bool> RunValidations(ValidationEvents evt);
}

[Flags]
public enum ValidationEvents
{
    Submit = 1,
    OnInput = 2,
    OnChange = 4,
    Any = OnInput | OnChange
}


public class ValidatorConfig
{
    // pre-baked lists
    public static readonly List<Validator> ValidationsNone = [];
    public static readonly List<Validator> ValidationsRequired = [Validator.Required];
    public static readonly List<Validator> ValidationsEmail = [Validator.Required, Validator.Email];
    public static readonly List<Validator> ValidationsRequiredId = [Validator.RequiredId];
    public static readonly List<Validator> ValidationsPassword = [Validator.Password];
    public static readonly List<Validator> ValidationsPhone = new List<Validator> { Validator.Phone };
    public static readonly List<Validator> ValidationsRequiredFullName = new List<Validator> { Validator.Required, Validator.FullName };
    public static readonly List<Validator> ValidationsRequiredBirthNumber = new List<Validator> { Validator.Required, Validator.BirthNumber };
    
    // pre-baked configs
    public static readonly ValidatorConfig None = new ValidatorConfig(ValidationsNone);
    public static readonly ValidatorConfig Required = new ValidatorConfig(ValidationsRequired);
    public static readonly ValidatorConfig Email = new ValidatorConfig(ValidationsEmail);
    public static readonly ValidatorConfig RequiredId = new ValidatorConfig(ValidationsRequiredId);
    public static readonly ValidatorConfig Password = new ValidatorConfig(ValidationsPassword);
    public static readonly ValidatorConfig Phone = new ValidatorConfig(ValidationsPhone);
    public static readonly ValidatorConfig RequiredFullName = new ValidatorConfig(ValidationsRequiredFullName);
    public static readonly ValidatorConfig RequiredBirthNumber = new ValidatorConfig(ValidationsRequiredBirthNumber);
    
    public List<Validator>? Validators { get; set; }
    [JsonIgnore]
    public List<ValidatorConfig>? DependantValidators { get; set; }
    [JsonIgnore]
    public IValidationComponent? ValidationComponent { get; set; }

    public ValidatorConfig()
    {
        
    }
    
    public ValidatorConfig(List<Validator> validators, List<ValidatorConfig>? dependantValidators = null)
    {
        Validators = validators;
        DependantValidators = dependantValidators;
    }
    
    public ValidatorConfig(Validator validator, List<ValidatorConfig>? dependantValidators = null)
    {
        Validators = [ validator ];
        DependantValidators = dependantValidators;
    }
    
    public ValidatorConfig(Validator validator)
    {
        Validators = [ validator ];
    }

    
    public ValidatorConfig(Validator validator, ValidatorConfig? dependantValidator)
    {
        Validators = [ validator ];
        if (dependantValidator is not null)
        {
            DependantValidators = [ dependantValidator ];
        }
    }

    public async Task<ValidationResult> Run(object? input, IValidationComponent sender, ValidationEvents evt)
    {
        ValidationResult vr = new ValidationResult(true);

        if (DependantValidators is not null)
        {
            foreach (ValidatorConfig vc in DependantValidators)
            {
                if (vc.ValidationComponent is not null)
                {
                    await vc.ValidationComponent.RunValidations(evt);   
                }
            }
        } 
        
        if (Validators is not null)
        {
            foreach (Validator? validator in Validators)
            {
                if (evt is ValidationEvents.Submit or ValidationEvents.Any || validator.RunOn.HasFlag(evt))
                {
                    vr = await validator.Validate(input, sender, evt);

                    if (!vr.Ok)
                    {
                        break;
                    }   
                }   
            }   
        }

        return vr;
    }
}

public class ValidationResult
{
    public bool Ok { get; set; }
    public string? Message { get; set; }
    public bool MessageIsTemplated { get; set; }

    public ValidationResult(bool ok, string message = null, bool messageIsTemplated = false)
    {
        Ok = ok;
        Message = message;
        MessageIsTemplated = messageIsTemplated;
    }
}

public class Validator
{
    private static readonly Regex PhoneRegex = new Regex("^\\+((?:9[679]|8[035789]|6[789]|5[90]|42|3[578]|2[1-689])|9[0-58]|8[1246]|6[0-6]|5[1-8]|4[013-9]|3[0-469]|2[70]|7|1)(?:\\W*\\d){0,13}\\d$", RegexOptions.Compiled);
    private static readonly Regex BirthNumberRegexCz = new Regex("\\d{2}(0[1-9]|1[0-2]|5[1-9]|6[0-2])(0[1-9]|1[0-9]|2[0-9]|3[0-1])\\/?\\d{3,4}", RegexOptions.Compiled);
    private static readonly Regex BirthNumberRegexPl = new Regex("^[0-9]{2}([02468]1|[13579][012])(0[1-9]|1[0-9]|2[0-9]|3[01])[0-9]{5}$", RegexOptions.Compiled);
    internal const string FIELD_NAME = "{FIELD_NAME}";

    private static string Verb(IValidationComponent cmp)
    {
        return cmp is ISelectInput ? "Vyberte" : "Vyplňte";
    }

    // atomic
    public static readonly Validator RequiredId = new Validator((s, evt, sender) => s.IsNullOrWhiteSpace() || s is "0" ? new ValidationResult(false, $"{Verb(sender)} {FIELD_NAME}", true) : new ValidationResult(true), ValidationEvents.Any);
    
    public static readonly Validator Required = new Validator((s, evt, sender) => s == string.Empty ? new ValidationResult(false, $"{Verb(sender)} {FIELD_NAME}", true) : new ValidationResult(true), ValidationEvents.Any);
    
    public static readonly Validator Phone = new Validator((s, evt, sender) =>
    {
        if (s.IsNullOrWhiteSpace())
        {
            return new ValidationResult(false, $"Vyplňte {FIELD_NAME}", true);
        }

        if (!s.StartsWith('+'))
        {
            return new ValidationResult(false, "FIX", true);
        }

        if (!PhoneRegex.IsMatch(s))
        {
            return new ValidationResult(false, "FIX", true);
        }
        
        return new ValidationResult(true);
    }, ValidationEvents.OnChange);
    
    public static readonly Validator FullName = new Validator((s, evt, sender) =>
    {
        if (!s.Contains(' '))
        {
            return new ValidationResult(false, "FIX", true);
        }
        
        return new ValidationResult(true);
    }, ValidationEvents.OnChange);
    
    private static readonly HashSet<char> BirthNumberHashSet = [' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '/', '\\'];
    
    public static readonly Validator BirthNumber = new Validator((s, evt, sender) =>
    {
        if (!s.ContainsOnlyWhitelistChars(BirthNumberHashSet))
        {
            return new ValidationResult(false, "FIX");
        }

        if (sender.Nm is null)
        {
            return new ValidationResult(true);
        }

        return new ValidationResult(true);
    }, ValidationEvents.OnChange);
    
    public static readonly Validator Email = new Validator((s, evt, sender) => s.IsValidEmail() ? new ValidationResult(true) : new ValidationResult(false, "FIX"), ValidationEvents.OnChange);
    public static readonly Validator Password = new Validator((s, evt, sender) => !s.IsNullOrWhiteSpace() && s.Length >= 6 ? new ValidationResult(true) : s.Length == 0 ? new ValidationResult(false, $"{Verb(sender)} {FIELD_NAME}", true) : new ValidationResult(false, "FIX"), ValidationEvents.Any);
    public ValidationEvents RunOn { get; set; }
    private Func<string, ValidationEvents, IValidationComponent, Task<ValidationResult>>? ValidateAsync { get; set; }
    private Func<string, ValidationEvents, IValidationComponent, ValidationResult>? ValidateSync { get; set; }

    public Validator()
    {
        
    }
    
    public Validator(Func<string, ValidationEvents, IValidationComponent, Task<ValidationResult>> validateAsync, ValidationEvents runOn)
    {
        ValidateAsync = validateAsync;
        RunOn = runOn;
    }
    
    public Validator(Func<string, ValidationEvents, IValidationComponent, ValidationResult> validate, ValidationEvents runOn)
    {
        ValidateSync = validate;
        RunOn = runOn;
    }

    public async Task<ValidationResult> Validate(object? input, IValidationComponent sender,  ValidationEvents evt)
    {
        string inputVal = input?.ToString() ?? string.Empty;

        if (input is Enum e)
        {
            inputVal = Convert.ChangeType(e, e.GetTypeCode()).ToString() ?? "0";
        }
        
        ValidationResult toRet = new ValidationResult(false);
        
        if (ValidateAsync is not null)
        {
            toRet = await ValidateAsync.Invoke(inputVal, evt, sender);
        }
        else if (ValidateSync is not null)
        {
            toRet = ValidateSync.Invoke(inputVal, evt, sender);   
        }

        return toRet;
    }
}

public class ValidationComponent<T> : InputComponent<T>, IValidationComponent, IAsyncDisposable
{
    [Parameter]
    public ValidatorConfig? Validate { get; set; }
    [Parameter]
    public string? ValidateLabel { get; set; }

    public bool RenderValidation { get; set; }
    public string? ValidationMsg { get; set; }

    protected override Task OnInitializedAsync()
    {
        if (Validate is not null)
        {
            Validate.ValidationComponent = this;
        }
        
        if (Context is not null)
        {
            Context.ValidationsAccumulator ??= [];
            
            if (Validate is not null)
            {
                Context.ValidationsAccumulator.TryAdd(this, true);
            }            
        }
        
        return base.OnInitializedAsync();
    }

    public override Task Clear()
    {
        ValidationMsg = string.Empty;
        RenderValidation = false;
        Value = default;
        StateHasChanged();
        return Task.CompletedTask;
    }

    public string SerializeValidators()
    {
        return Validate is { Validators: not null } && Validate.Validators.Any(x => x == Validator.Required || x == Validator.RequiredId) ? "required" : string.Empty;
    }

    public virtual async Task<bool> RunValidations(ValidationEvents evt)
    {
        if (Validate is not null && EnableValidation)
        {
            T? value = Value;
            
            if (this is IInputComponentMultivalue<T> multiVal)
            {
                value = multiVal.GetScalarValue();
            }
            
            ValidationResult vr = await Validate.Run(value, this, evt);
            RenderValidation = !vr.Ok;
            ValidationMsg = vr.Message;
            
            if (vr.MessageIsTemplated)
            {
                ValidationMsg = ValidationMsg?.Replace(Validator.FIELD_NAME, ValidateLabel?.ToLowerInvariant() ?? Label?.ToLowerInvariant());
            }
            
            StateHasChanged();
            return vr.Ok;
        }

        return true;
    }

    public virtual ValueTask DisposeAsync()
    {
        try
        {
            if (Context is not null)
            {
                Context.ValidationsAccumulator?.Remove(this, out bool _);
                Context.SetFocusedInput(this, false);
            }
        }
        catch (Exception e)
        {
            
        }
        
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

public enum FeedbackTypes
{
    Ok,
    Error,
    Warn,
    Info,
    Love,
    Tip
}

public class FormResult
{
    public FeedbackTypes Type { get; set; }
    public string? Msg { get; set; }

    public FormResult(string msg)
    {
        Type = FeedbackTypes.Error;
        Msg = msg;
    }

    public FormResult(ICrudResult cr, string okMsg)
    {
        Type = cr.Ok ? FeedbackTypes.Ok : FeedbackTypes.Error;
        Msg = cr.Ok ? okMsg : cr.Error;
    }

    public FormResult(bool ok, string msg)
    {
        Type = ok ? FeedbackTypes.Ok : FeedbackTypes.Error;
        Msg = msg;
    }
    
    public FormResult(bool ok)
    {
        Type = ok ? FeedbackTypes.Ok : FeedbackTypes.Error;
    }
    
    public FormResult(FeedbackTypes type, string msg)
    {
        Type = type;
        Msg = msg;
    }
}

public class ValidationFeedback
{
    public string? Text { get; set; }
    public FeedbackTypes? Type { get; set; }

    public void Clear()
    {
        Text = null;
    }

    public void SetError(string text)
    {
        Text = text;
        Type = FeedbackTypes.Error;
    }
    
    public void Set(string text, FeedbackTypes type)
    {
        Text = text;
        Type = type;
    }
}

public class SmartFormConfig
{
    public bool Smart { get; set; }
    public Func<Task>? Reset { get; set; }

    public SmartFormConfig()
    {
        
    }
    
    public SmartFormConfig(Func<Task> reset)
    {
        Smart = true;
        Reset = reset;
    }
}

public class EdFormRef
{
    public EdForm? Form { get; set; }
    public bool Initialized { get; set; }
    public Func<EdForm, Task>? OnBind { get; set; }

    public EdFormRef()
    {
        
    }

    public EdFormRef(Func<EdForm, Task> onBind)
    {
        OnBind = onBind;
    }
}

public interface ICacheStruct
{
    public object? GetCacheValue();
}

public enum RteConfigModes
{
    /// <summary>
    /// All tools are available
    /// </summary>
    Default,
    /// <summary>
    /// Tools unique to the master editor are disabled
    /// </summary>
    [StringValue("tableEditor")]
    SlaveEditor,
    /// <summary>
    /// Reserved
    /// </summary>
    Admin
}

public enum RteConfigToolTypes 
{
    Unknown
}

public class RteConfigTool
{
    public RteConfigToolTypes Type { get; set; }
    public object? Cfg { get; set; }
}

public class RteConfig
{
    public RteConfigModes Mode { get; set; } = RteConfigModes.Default;
    public List<RteConfigTool> AdditionalTools { get; set; } = [];

    public RteConfig()
    {
        
    }
}

public enum ClientInputEvents
{
    Input,
    Change,
    Blur
}

public enum ClientTransferProtocols
{
    Unknown,
    Plaintext,
    Patch
}

public enum HorizontalPositions
{
    Left,
    Center,
    Right
}

public enum InputTypes
{
    Text,
    Number,
    Email,
    Password,
    Search,
    Date,
    File
}

public enum HintFetchUrls
{
    Unknown
}

public enum CheckboxAlignments
{
    Default,
    [StringValue("checkboxAlignTop")]
    Top
}

public enum SelectTypes
{
    Native,
    Virtual,
    Tagify,
    Tags,
    Pills,
    Dropdown,
    Radioboxes,
    Phone
}

[TypeConverter(typeof(TcStringToListInt))]
public class ConvertibleList<T> : List<T>
{
    public ConvertibleList()
    {
        
    }

    public ConvertibleList(IEnumerable<T>? collection)
    {
        if (collection is not null)
        {
            AddRange(collection);            
        }
    }
}

public class TcStringToListInt : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return value is string casted ? new List<int>(casted.FromCsv()) : base.ConvertFrom(context, culture, value);
    }
    
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        return destinationType == typeof (string) && value is List<int> casted ? casted.ToCsv() : base.ConvertTo(context, culture, value, destinationType);
    }
}

public enum SelectDesignTypes
{
    Unknown,
    Default,
    InlineDescription
}

public class NativeSelectOptionGroup : ISelectOptionGroup
{
    public string Name { get; set; }
    public dynamic? Value { get; set; }
    public bool Selected { get; set; }
    public IEnumerable<dynamic>? Options { get; set; }
}

public interface ISelectOptionGroup : ISelectOption
{
    public IEnumerable<dynamic>? Options { get; set; }
}

public enum SelectOptionModes
{
    Options,
    Groups
}

public class TagSelectOption : ISelectOption
{
    public string Icon { get; set; }
    public string Description { get; set; }
    public string Name { get; set; }
    public dynamic? Value { get; set; }
    public bool Selected { get; set; }
}

public class PillSelectOption : TooltipSelectOption
{
    public string? Decorator { get; set; }
}

public class TooltipSelectOption : NativeSelectOption
{
    [JsonIgnore]
    public Func<Task<string>>? GetTooltip { get; set; }
}

public class OrderIndexAttribute : Attribute
{
    public int Index { get; set; }

    public OrderIndexAttribute(int index)
    {
        Index = index;
    }
}

public class ImageSelectOption : NativeSelectOption
{
    public string Image { get; set; }
}

public class ImageSelectOptionExtended : ImageSelectOption
{
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    
    /// <summary>
    /// Turn this on to render the option's image in selected preview
    /// </summary>
    public bool RenderSelectedImage { get; set; }
}