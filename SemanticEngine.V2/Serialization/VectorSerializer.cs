using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SemanticEngine.V2.Serialization;

public static class VectorSerializer
{
    /// <summary>
    /// Serializes a float vector into a compact JSON array string.
    /// Deterministic formatting helps testing and avoids locale issues.
    /// </summary>
    public static string SerializeToJson(float[] vector)
    {
        if (vector == null) throw new ArgumentNullException(nameof(vector));
        if (vector.Length == 0) return "[]";

        // Fast, locale-safe compact JSON without whitespace.
        // Using invariant culture ensures '.' decimal separator even on non-US locales.
        var sb = new StringBuilder(capacity: vector.Length * 8);
        sb.Append('[');

        for (int i = 0; i < vector.Length; i++)
        {
            if (i > 0) sb.Append(',');

            // "R" round-trip keeps precision stable.
            sb.Append(vector[i].ToString("R", CultureInfo.InvariantCulture));
        }

        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>
    /// Deserializes a JSON array string into float[].
    /// </summary>
    public static float[] DeserializeFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Vector JSON is empty.", nameof(json));

        // Using System.Text.Json keeps dependencies minimal.
        var arr = JsonSerializer.Deserialize<float[]>(json);
        return arr ?? Array.Empty<float>();
    }

    /// <summary>
    /// Convenience: validate vector JSON quickly.
    /// </summary>
    public static bool IsValidVectorJson(string json)
    {
        try
        {
            _ = DeserializeFromJson(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
