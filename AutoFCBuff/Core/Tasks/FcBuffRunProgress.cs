namespace AutoFCBuff.Core.Tasks;

public enum FcRunStep
{
    Initializing,
    Teleporting,
    Navigating,
    Purchasing,
    Completed,
    Failed
}

public sealed class FcBuffRunProgress
{
    public FcRunStep CurrentStep { get; set; } = FcRunStep.Initializing;
    public string StatusMessage { get; set; } = "Initializing...";
    public int TotalToBuy { get; set; }
    public int PurchasedCount { get; set; }
    public string TargetGcCity { get; set; } = string.Empty;

    public float StepPercentage => CurrentStep switch
    {
        FcRunStep.Initializing => 0.1f,
        FcRunStep.Teleporting => 0.3f,
        FcRunStep.Navigating => 0.5f,
        FcRunStep.Purchasing => 0.5f + (TotalToBuy > 0 ? (float)PurchasedCount / TotalToBuy * 0.4f : 0.4f),
        FcRunStep.Completed => 1.0f,
        FcRunStep.Failed => 0.0f,
        _ => 0.0f
    };
}
