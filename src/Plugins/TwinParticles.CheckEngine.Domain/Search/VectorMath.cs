using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TwinParticles.CheckEngine.Domain.Search;

/// <summary>
/// Maintains the top-K cosine similarities without sorting the full candidate set.
/// </summary>
public sealed class CosineSimilarityTopK<T>
{
    private readonly float[] _query;
    private readonly int _capacity;
    private readonly Func<T, int> _tieBreaker;
    private readonly List<(T Item, float Score)> _items = new();

    public CosineSimilarityTopK(float[] query, int capacity, Func<T, int> tieBreaker)
    {
        _query = query ?? throw new ArgumentNullException(nameof(query));
        _capacity = Math.Max(1, capacity);
        _tieBreaker = tieBreaker ?? throw new ArgumentNullException(nameof(tieBreaker));
    }

    public void Consider(T item, float[] vector)
    {
        var score = VectorMath.CosineSimilarity(_query, vector);
        if (score <= 0f)
            return;

        if (_items.Count < _capacity)
        {
            _items.Add((item, score));
            return;
        }

        var worstIndex = 0;
        var worstScore = _items[0].Score;
        for (var index = 1; index < _items.Count; index++)
        {
            if (_items[index].Score < worstScore)
            {
                worstScore = _items[index].Score;
                worstIndex = index;
            }
        }

        if (score > worstScore)
            _items[worstIndex] = (item, score);
    }

    public IReadOnlyList<(T Item, float Score)> Results() =>
        _items
            .OrderByDescending(entry => entry.Score)
            .ThenBy(entry => _tieBreaker(entry.Item))
            .ToList();
}

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
