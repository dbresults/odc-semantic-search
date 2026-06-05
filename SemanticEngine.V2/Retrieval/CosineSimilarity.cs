using System;

namespace SemanticEngine.V2.Retrieval;

public static class CosineSimilarity
{
    public static float Compute(float[] a, float[] b)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));
        if (a.Length == 0 || b.Length == 0) return 0f;
        if (a.Length != b.Length)
            throw new ArgumentException($"Vector length mismatch: a={a.Length}, b={b.Length}");

        double dot = 0;
        double normA = 0;
        double normB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            double x = a[i];
            double y = b[i];

            dot += x * y;
            normA += x * x;
            normB += y * y;
        }

        if (normA == 0 || normB == 0) return 0f;

        return (float)(dot / (Math.Sqrt(normA) * Math.Sqrt(normB)));
    }
}
