using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TwinParticles.CheckEngine.Domain.Search;

public static class VectorMath
{
    public const int DefaultDimensions = 64;

    public static float[] EmbedText(string text, int dimensions = DefaultDimensions)
    {
        var vector = new float[dimensions];
        foreach (var token in Tokenize(text))
        {
            var bucket = Math.Abs(token.GetHashCode(StringComparison.Ordinal)) % dimensions;
            vector[bucket] += 1f;
        }

        return Normalize(vector);
    }

    public static float CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        if (left.Count == 0 || right.Count == 0 || left.Count != right.Count)
            return 0f;

        var dot = 0f;
        var leftNorm = 0f;
        var rightNorm = 0f;
        for (var i = 0; i < left.Count; i++)
        {
            dot += left[i] * right[i];
            leftNorm += left[i] * left[i];
            rightNorm += right[i] * right[i];
        }

        if (leftNorm <= 0f || rightNorm <= 0f)
            return 0f;

        return dot / (MathF.Sqrt(leftNorm) * MathF.Sqrt(rightNorm));
    }

    public static string HashModel(string providerName, int dimensions)
    {
        var raw = $"{providerName}|{dimensions}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        foreach (var token in text.ToLower(CultureInfo.InvariantCulture).Split(
                     [' ', '-', '_', '/', '\\', ',', '.', ';', ':'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (token.Length >= 2)
                yield return token;
        }
    }

    private static float[] Normalize(float[] vector)
    {
        var norm = 0f;
        foreach (var value in vector)
            norm += value * value;

        if (norm <= 0f)
            return vector;

        var scale = 1f / MathF.Sqrt(norm);
        for (var i = 0; i < vector.Length; i++)
            vector[i] *= scale;

        return vector;
    }
}
