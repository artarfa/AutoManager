using Microsoft.ML.Data;

namespace AutoManager.Models;

public class RequirementInput
{
    public string Text { get; set; }
}

public class RequirementFeatures
{
    [VectorType]
    public float[] Features { get; set; }
}