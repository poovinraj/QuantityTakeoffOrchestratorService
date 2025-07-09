using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.Models.View;

[ExcludeFromCodeCoverage]
public class QuantityValueView
{
    public string? QuantityTakeoffId { get; set; }
    public decimal? Value { get; set; }
    public decimal? OriginalValue { get; set; }
    public string? AnnotationId { get; set; }
    public bool IsManuallyEdited { get; set; }
    public bool HasAnnotation
    {
        get { return !string.IsNullOrEmpty(AnnotationId); }
        private set { }
    }
}
