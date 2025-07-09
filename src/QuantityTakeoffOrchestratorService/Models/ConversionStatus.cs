namespace QuantityTakeoffOrchestratorService.Models;

public class ConversionStatus
{
    public string Status { get; set; }
    public int Progress { get; set; }
    public string JobModelId { get; internal set; }
}
