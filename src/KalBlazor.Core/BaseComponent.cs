using Microsoft.AspNetCore.Components;

namespace SoftwareThingies.KalBlazor.Core;

public abstract class BaseComponent : ComponentBase
{
    /// <summary>
    /// Replaces the component default classes.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Additional classes appended to the effective component classes.
    /// </summary>
    [Parameter]
    public string? AdditionalClass { get; set; }

    /// <summary>
    /// Plain text tooltip content shown when the pointer hovers the component root element.
    /// </summary>
    [Parameter]
    public string? Tooltip { get; set; }

    /// <summary>
    /// Tailwind utility classes applied to the shared tooltip host when this component is hovered.
    /// </summary>
    [Parameter]
    public string? TooltipClass { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    protected virtual string ComponentClass => string.Empty;

    protected abstract string DefaultClass { get; }

    protected virtual string DynamicClass => string.Empty;

    protected IReadOnlyDictionary<string, object>? FilteredAdditionalAttributes => BuildFilteredAdditionalAttributes();

    protected string CssClass
    {
        get
        {
            var explicitClass = !string.IsNullOrWhiteSpace(Class)
                ? Class
                : TryGetAttributeClass(out var attributeClass)
                    ? attributeClass
                    : string.Empty;

            var effectiveClass = !string.IsNullOrWhiteSpace(explicitClass)
                ? explicitClass
                : $"{DefaultClass} {DynamicClass}".Trim();

            return $"{ComponentClass} {effectiveClass} {AdditionalClass}".Trim();
        }
    }

    protected IReadOnlyDictionary<string, object>? BuildFilteredAdditionalAttributes(
        string? tooltip = null,
        string? tooltipClass = null)
    {
        Dictionary<string, object>? filteredAttributes = null;
        var effectiveTooltip = string.IsNullOrWhiteSpace(tooltip) ? Tooltip : tooltip;
        var effectiveTooltipClass = string.IsNullOrWhiteSpace(tooltipClass) ? TooltipClass : tooltipClass;

        if (AdditionalAttributes is not null)
        {
            filteredAttributes = AdditionalAttributes
                .Where(attribute => !IsClassAttribute(attribute.Key))
                .ToDictionary(StringComparer.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(effectiveTooltip))
        {
            AddFilteredAttribute(ref filteredAttributes, "data-kal-tooltip", effectiveTooltip!);

            if (!string.IsNullOrWhiteSpace(effectiveTooltipClass))
            {
                AddFilteredAttribute(ref filteredAttributes, "data-kal-tooltip-class", effectiveTooltipClass!);
            }
        }

        return filteredAttributes is { Count: > 0 } ? filteredAttributes : null;
    }

    private static void AddFilteredAttribute(
        ref Dictionary<string, object>? filteredAttributes,
        string key,
        string value)
    {
        filteredAttributes ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        filteredAttributes[key] = value;
    }

    private bool TryGetAttributeClass(out string attributeClass)
    {
        attributeClass = string.Empty;

        if (AdditionalAttributes is null)
        {
            return false;
        }

        foreach (var attribute in AdditionalAttributes)
        {
            if (!IsClassAttribute(attribute.Key))
            {
                continue;
            }

            attributeClass = attribute.Value?.ToString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(attributeClass);
        }

        return false;
    }

    private static bool IsClassAttribute(string attributeName)
    {
        return string.Equals(attributeName, "class", StringComparison.OrdinalIgnoreCase)
            || string.Equals(attributeName, "@class", StringComparison.OrdinalIgnoreCase);
    }
}
