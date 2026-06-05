using System;
using System.Collections.Generic;
using System.Linq;
using SemanticEngine.V2.Serialization;

namespace SemanticEngine.V2.Retrieval;

public sealed class VectorCandidate
{
    public string Id { get; set; } = string.Empty;
    public string VectorJson { get; set; } = "[]";
    public int PageNumber { get; set; }
    public int ChunkIndex { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public string ChunkHash { get; set; } = string.Empty;
}

public sealed class VectorSearchResult
{
    public string Id { get; set; } = string.Empty;
    public float Score { get; set; }
    public int PageNumber { get; set; }
    public int ChunkIndex { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public string ChunkHash { get; set; } = string.Empty;
}

public static class TopKVectorSearch
{
    public static List<VectorSearchResult> Search(
        float[] queryVector,
        List<VectorCandidate> candidates,
        int topK = 10,
        float minScore = 0.0f) 
    {
        if (queryVector == null) throw new ArgumentNullException(nameof(queryVector));
        if (candidates == null) throw new ArgumentNullException(nameof(candidates));

        var scored = new List<VectorSearchResult>(candidates.Count);

        foreach (var c in candidates)
        {
            if (c == null || string.IsNullOrWhiteSpace(c.VectorJson)) continue;

            // RESTORED: This defines 'vec' which was missing
            var vec = VectorSerializer.DeserializeFromJson(c.VectorJson);
            if (vec.Length != queryVector.Length) continue;

            float score = CosineSimilarity.Compute(queryVector, vec);

            // Apply the threshold filter
            if (score >= minScore) 
            {
                scored.Add(new VectorSearchResult
                {
                    Id = c.Id,
                    Score = score,
                    PageNumber = c.PageNumber,
                    ChunkIndex = c.ChunkIndex,
                    ChunkText = c.ChunkText,
                    ChunkHash = c.ChunkHash
                });
            }
        }

        return scored
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();
    }
}