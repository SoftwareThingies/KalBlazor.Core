using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace SoftwareThingies.KalBlazor.Core;

public partial class KalIcon
{
    protected override string ComponentClass => "kal-icon";

    protected override string DefaultClass => "inline-block shrink-0 overflow-visible align-middle fill-current stroke-current";

    [Parameter]
    public string? Icon { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string IconBasePath { get; set; } = "/_content/SoftwareThingies.KalBlazor.Core/icons";

    [Parameter]
    public string? SymbolId { get; set; }

    [Parameter]
    public string ViewBox { get; set; } = "0 0 24 24";

    [Parameter]
    public string PreserveAspectRatio { get; set; } = "xMidYMid meet";

    private IReadOnlyDictionary<string, object>? IconAdditionalAttributes =>
        BuildFilteredAdditionalAttributes()?
            .Where(attribute => !IsClassOrStyleAttribute(attribute.Key))
            .ToDictionary(StringComparer.OrdinalIgnoreCase);

    private string? IconReference => ResolveIconReference();

    private string? StrokeWidth => ResolveStrokeWidth();

    private string IconStyle
    {
        get
        {
            var style = TryGetAttributeValue("style");
            var aspectRatio = ResolveAspectRatio();
            var strokeColor = ResolveStrokeColor();
            var iconStyle = $"height:1em;width:calc(1em * {aspectRatio.ToString(CultureInfo.InvariantCulture)});border-width:0;border-style:none";

            if (!string.IsNullOrWhiteSpace(strokeColor))
            {
                iconStyle = $"{iconStyle};stroke:{strokeColor}";
            }

            return string.IsNullOrWhiteSpace(style)
                ? iconStyle
                : $"{style};{iconStyle}";
        }
    }

    private string? Role => string.IsNullOrWhiteSpace(AriaLabel) ? null : "img";

    private string? AriaHidden => string.IsNullOrWhiteSpace(AriaLabel) ? "true" : null;

    private string? ResolveIconReference()
    {
        if (string.IsNullOrWhiteSpace(Icon))
        {
            return null;
        }

        var icon = Icon.Trim();

        if (icon.Contains('#', StringComparison.Ordinal))
        {
            return icon;
        }

        if (icon.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            return $"{icon}#{ResolveSymbolId(icon)}";
        }

        if (icon.Contains('/', StringComparison.Ordinal))
        {
            var iconPath = icon.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ? icon : $"{icon}.svg";
            return $"{iconPath}#{ResolveSymbolId(iconPath)}";
        }

        var basePath = IconBasePath.TrimEnd('/');
        return $"{basePath}/{icon}.svg#{ResolveSymbolId(icon)}";
    }

    private string ResolveSymbolId(string icon)
    {
        if (!string.IsNullOrWhiteSpace(SymbolId))
        {
            return SymbolId;
        }

        var fileName = icon.Split('/').Last();
        return fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^4]
            : fileName;
    }

    private string? ResolveStrokeWidth()
    {
        var classNames = GetClassNames().Reverse();

        foreach (var className in classNames)
        {
            var strokeWidth = ResolveStrokeWidth(className);

            if (strokeWidth is not null)
            {
                return strokeWidth;
            }
        }

        return null;
    }

    private IEnumerable<string> GetClassNames()
    {
        if (!string.IsNullOrWhiteSpace(Class))
        {
            foreach (var className in SplitClassNames(Class))
            {
                yield return className;
            }
        }
        else if (TryGetAttributeValue("class") is { } attributeClass)
        {
            foreach (var className in SplitClassNames(attributeClass))
            {
                yield return className;
            }
        }
        else
        {
            foreach (var className in SplitClassNames(DefaultClass))
            {
                yield return className;
            }
        }

        foreach (var className in SplitClassNames(AdditionalClass))
        {
            yield return className;
        }
    }

    private static IEnumerable<string> SplitClassNames(string? classNames)
    {
        return string.IsNullOrWhiteSpace(classNames)
            ? []
            : classNames.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string? ResolveStrokeWidth(string className)
    {
        if (className.Contains(':', StringComparison.Ordinal))
        {
            return null;
        }

        var normalizedClassName = className;

        return normalizedClassName switch
        {
            "border" or "border-x" or "border-y" or "border-s" or "border-e" or "border-t" or "border-r" or "border-b" or "border-l" => "1",
            "border-0" or "border-x-0" or "border-y-0" or "border-s-0" or "border-e-0" or "border-t-0" or "border-r-0" or "border-b-0" or "border-l-0" => "0",
            "border-2" or "border-x-2" or "border-y-2" or "border-s-2" or "border-e-2" or "border-t-2" or "border-r-2" or "border-b-2" or "border-l-2" => "2",
            "border-4" or "border-x-4" or "border-y-4" or "border-s-4" or "border-e-4" or "border-t-4" or "border-r-4" or "border-b-4" or "border-l-4" => "4",
            "border-8" or "border-x-8" or "border-y-8" or "border-s-8" or "border-e-8" or "border-t-8" or "border-r-8" or "border-b-8" or "border-l-8" => "8",
            _ => ResolveArbitraryBorderWidth(normalizedClassName)
        };
    }

    private string? ResolveStrokeColor()
    {
        var classNames = GetClassNames().ToArray();

        if (classNames.Any(IsStrokeClass))
        {
            return null;
        }

        foreach (var className in classNames.Reverse())
        {
            var strokeColor = ResolveStrokeColor(className);

            if (strokeColor is not null)
            {
                return strokeColor;
            }
        }

        return null;
    }

    private static string? ResolveStrokeColor(string className)
    {
        if (className.Contains(':', StringComparison.Ordinal))
        {
            return null;
        }

        var colorName = ResolveBorderColorName(className);

        if (colorName is null)
        {
            return null;
        }

        return colorName switch
        {
            "current" => "currentColor",
            "inherit" => "inherit",
            "transparent" => "transparent",
            "black" => "#000",
            "white" => "#fff",
            _ when colorName.StartsWith("[", StringComparison.Ordinal) && colorName.EndsWith("]", StringComparison.Ordinal) => colorName[1..^1],
            _ => $"var(--color-{colorName})"
        };
    }

    private static string? ResolveBorderColorName(string className)
    {
        var prefixes = new[]
        {
            "border-x-",
            "border-y-",
            "border-s-",
            "border-e-",
            "border-t-",
            "border-r-",
            "border-b-",
            "border-l-",
            "border-"
        };

        var prefix = prefixes.FirstOrDefault(className.StartsWith);

        if (prefix is null)
        {
            return null;
        }

        var colorName = className[prefix.Length..];

        if (string.IsNullOrWhiteSpace(colorName)
            || IsBorderWidth(colorName)
            || IsBorderStyle(colorName))
        {
            return null;
        }

        var opacitySeparator = colorName.IndexOf('/', StringComparison.Ordinal);
        return opacitySeparator >= 0 ? colorName[..opacitySeparator] : colorName;
    }

    private static bool IsStrokeClass(string className)
    {
        if (className.Contains(':', StringComparison.Ordinal))
        {
            return false;
        }

        return className.StartsWith("stroke-", StringComparison.Ordinal)
            && !IsBorderWidth(className["stroke-".Length..]);
    }

    private static bool IsBorderWidth(string value)
    {
        return value is "0" or "2" or "4" or "8"
            || value.StartsWith("[", StringComparison.Ordinal);
    }

    private static bool IsBorderStyle(string value)
    {
        return value is "solid" or "dashed" or "dotted" or "double" or "hidden" or "none";
    }

    private static string? ResolveArbitraryBorderWidth(string className)
    {
        var marker = className.StartsWith("border-[", StringComparison.Ordinal)
            ? "border-["
            : className.StartsWith("border-x-[", StringComparison.Ordinal)
                ? "border-x-["
                : className.StartsWith("border-y-[", StringComparison.Ordinal)
                    ? "border-y-["
                    : null;

        if (marker is null || !className.EndsWith(']'))
        {
            return null;
        }

        var value = className[marker.Length..^1];
        return value.EndsWith("px", StringComparison.OrdinalIgnoreCase)
            ? value[..^2]
            : value;
    }

    private double ResolveAspectRatio()
    {
        var values = ViewBox
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => double.TryParse(value, CultureInfo.InvariantCulture, out var number) ? number : 0)
            .ToArray();

        if (values.Length < 4 || values[2] <= 0 || values[3] <= 0)
        {
            return 1;
        }

        return values[2] / values[3];
    }

    private string? TryGetAttributeValue(string attributeName)
    {
        if (AdditionalAttributes is null)
        {
            return null;
        }

        foreach (var attribute in AdditionalAttributes)
        {
            if (string.Equals(attribute.Key, attributeName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(attribute.Key, $"@{attributeName}", StringComparison.OrdinalIgnoreCase))
            {
                return attribute.Value?.ToString();
            }
        }

        return null;
    }

    private static bool IsClassOrStyleAttribute(string attributeName)
    {
        return string.Equals(attributeName, "class", StringComparison.OrdinalIgnoreCase)
            || string.Equals(attributeName, "@class", StringComparison.OrdinalIgnoreCase)
            || string.Equals(attributeName, "style", StringComparison.OrdinalIgnoreCase)
            || string.Equals(attributeName, "@style", StringComparison.OrdinalIgnoreCase);
    }
}
