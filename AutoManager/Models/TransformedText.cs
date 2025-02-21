using Microsoft.ML.Data;

namespace AutoManager.Models
{
    public class TransformedText
    {
        [VectorType]
        public float[] Features { get; set; }
    }
}