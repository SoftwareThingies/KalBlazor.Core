using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace SoftwareThingies.KalBlazor.Core.DataGrid;

public partial class KalGridColumn<TItem> : IDisposable
{
    private const string DefaultCollapsedContentClass = "mt-2 space-y-1 text-xs leading-5 text-slate-500";
    private static readonly RenderFragment EmptyCell = _ => { };
    private Expression<Func<TItem, object?>>? _compiledProperty;
    private Func<TItem, object?>? _propertyAccessor;

    protected override string ComponentClass => "kal-grid-column";

    protected override string DefaultClass => "px-4 py-3 text-xs font-semibold uppercase tracking-wide text-slate-600";

    protected override string DynamicClass => $"{KalDataGridResponsiveClass.TextAlignment(Alignment)} {KalDataGridResponsiveClass.HideBelow(CollapseBelow)} {WidthClass}".Trim();

    [CascadingParameter]
    internal KalDataGridContext<TItem>? GridContext { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public RenderFragment? CellHeaderTemplate { get; set; }

    [Parameter]
    public Expression<Func<TItem, object?>>? Property { get; set; }

    [Parameter]
    public RenderFragment<TItem>? CellTemplate { get; set; }

    [Parameter]
    public Func<TItem, string?>? CellTooltip { get; set; }

    [Parameter]
    public string? CellTooltipClass { get; set; }

    [Parameter]
    public KalDataGridAlignment Alignment { get; set; } = KalDataGridAlignment.Start;

    /// <summary>
    /// Hides this column below the selected Tailwind breakpoint.
    /// </summary>
    [Parameter]
    public KalDataGridBreakpoint CollapseBelow { get; set; } = KalDataGridBreakpoint.None;

    [Parameter]
    public string Scope { get; set; } = "col";

    [Parameter]
    public string? WidthClass { get; set; }

    [Parameter]
    public string? HeaderClass { get; set; }

    [Parameter]
    public string? AdditionalHeaderClass { get; set; }

    [Parameter]
    public string? CellClass { get; set; }

    [Parameter]
    public string? AdditionalCellClass { get; set; }

    [Parameter]
    public RenderFragment<TItem>? CollapsedContent { get; set; }

    [Parameter]
    public KalDataGridBreakpoint CollapsedContentBelow { get; set; } = KalDataGridBreakpoint.Md;

    [Parameter]
    public string? CollapsedContentClass { get; set; }

    [Parameter]
    public string? AdditionalCollapsedContentClass { get; set; }

    internal RenderFragment HeaderContent => CellHeaderTemplate
        ?? ChildContent
        ?? (builder => builder.AddContent(0, HeaderText));

    internal string HeaderCssClass
    {
        get
        {
            var effectiveClass = !string.IsNullOrWhiteSpace(HeaderClass)
                ? HeaderClass
                : CssClass;

            return $"{effectiveClass} {AdditionalHeaderClass}".Trim();
        }
    }

    internal IReadOnlyDictionary<string, object>? HeaderAttributes => FilteredAdditionalAttributes;

    internal string CellCssClass
    {
        get
        {
            var effectiveClass = !string.IsNullOrWhiteSpace(CellClass)
                ? CellClass
                : $"kal-grid-cell px-4 py-3 align-top text-sm text-slate-700 {KalDataGridResponsiveClass.TextAlignment(Alignment)} {KalDataGridResponsiveClass.HideBelow(CollapseBelow)}".Trim();

            return $"{effectiveClass} {AdditionalCellClass}".Trim();
        }
    }

    internal IReadOnlyDictionary<string, object>? GetCellAttributes(TItem item)
    {
        var cellTooltip = CellTooltip?.Invoke(item);
        var effectiveTooltip = string.IsNullOrWhiteSpace(cellTooltip) ? Tooltip : cellTooltip;
        var effectiveTooltipClass = string.IsNullOrWhiteSpace(CellTooltipClass) ? TooltipClass : CellTooltipClass;

        return BuildFilteredAdditionalAttributes(effectiveTooltip, effectiveTooltipClass);
    }

    internal string CollapsedContentCssClass
    {
        get
        {
            var effectiveClass = string.IsNullOrWhiteSpace(CollapsedContentClass)
                ? DefaultCollapsedContentClass
                : CollapsedContentClass;

            return $"{effectiveClass} {KalDataGridResponsiveClass.ShowBelow(CollapsedContentBelow)} {AdditionalCollapsedContentClass}".Trim();
        }
    }

    private string HeaderText => !string.IsNullOrWhiteSpace(Title) ? Title : PropertyName;

    private string PropertyName => TryGetPropertyInfo(Property, out var property) ? property.Name : string.Empty;

    internal string GetSearchText(TItem item)
    {
        var value = GetPropertyValue(item);
        if (value is null)
        {
            return string.Empty;
        }

        return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
    }

    internal RenderFragment RenderCell(TItem item)
    {
        if (CellTemplate is not null)
        {
            return CellTemplate(item);
        }

        if (Property is not null)
        {
            var value = GetPropertyValue(item);
            return builder => builder.AddContent(0, value);
        }

        return EmptyCell;
    }

    protected override void OnInitialized()
    {
        GridContext?.Register(this);
    }

    public void Dispose()
    {
        GridContext?.Unregister(this);
    }

    private object? GetPropertyValue(TItem item)
    {
        if (Property is null)
        {
            return null;
        }

        if (!ReferenceEquals(Property, _compiledProperty))
        {
            _compiledProperty = Property;
            _propertyAccessor = CompileNullSafeAccessor(Property);
        }

        return _propertyAccessor!(item);
    }

    private static Func<TItem, object?> CompileNullSafeAccessor(Expression<Func<TItem, object?>> expression)
    {
        var body = new NullSafeMemberAccessVisitor().Visit(expression.Body);
        return Expression.Lambda<Func<TItem, object?>>(body!, expression.Parameters).Compile();
    }

    private static bool TryGetPropertyInfo(Expression<Func<TItem, object?>>? expression, out PropertyInfo property)
    {
        property = null!;

        switch (expression?.Body)
        {
            case MemberExpression memberExpression when memberExpression.Member is PropertyInfo directProperty:
                property = directProperty;
                return true;
            case UnaryExpression { Operand: MemberExpression unaryMemberExpression }
                when unaryMemberExpression.Member is PropertyInfo unaryProperty:
                property = unaryProperty;
                return true;
            default:
                return false;
        }
    }

    private sealed class NullSafeMemberAccessVisitor : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is null)
            {
                return base.VisitMember(node);
            }

            var instance = Visit(node.Expression);
            var memberAccess = Expression.MakeMemberAccess(instance, node.Member);

            if (!CanBeNull(instance.Type))
            {
                return memberAccess;
            }

            var instanceVariable = Expression.Variable(instance.Type, "instance");
            return Expression.Block(
                [instanceVariable],
                Expression.Assign(instanceVariable, instance),
                Expression.Condition(
                    Expression.Equal(instanceVariable, Expression.Constant(null, instance.Type)),
                    Expression.Default(node.Type),
                    Expression.MakeMemberAccess(instanceVariable, node.Member)));
        }

        private static bool CanBeNull(Type type)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
        }
    }
}
