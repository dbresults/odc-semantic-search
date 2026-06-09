using System;
using System.Collections.Generic;
using System.Net.Http;
using SemanticEngine.V2.Contracts;
using SemanticEngine.V2.Core;
using SemanticEngine.V2.Embeddings;
using SemanticEngine.V2.Pdf;
using SemanticEngine.V2.Retrieval;
using SemanticEngine.V2.Serialization;

namespace SemanticEngine.V2;

public static class SemanticEngineV2Facade
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };
    /// <summary>
    /// ODC-friendly synchronous entrypoint.
    /// Internally executes async ingestion safely.
    /// </summary>
    public static List<IngestionVectorRecord> PrepareVectorsFromPdf(
        byte[] pdfBytes,
        string endpoint,
        string apiKey,
        string model,
        bool isAzure,
        int maxPages,
        int maxCharsPerPage,
        int chunkSize,
        int overlap,
        int maxChunksTotal)
    {
        if (pdfBytes == null || pdfBytes.Length == 0)
            throw new ArgumentException("PDF bytes are empty.", nameof(pdfBytes));

        maxPages = (maxPages <= 0) ? 50 : maxPages;
        chunkSize = (chunkSize <= 0) ? 1000 : chunkSize;
        overlap = (overlap < 0) ? 150 : overlap;
        maxChunksTotal = (maxChunksTotal <= 0) ? 200 : maxChunksTotal;

        if (overlap >= chunkSize)
            overlap = Math.Max(0, chunkSize / 6);

        var embeddingClient = new EmbeddingClient(
            _httpClient,
            endpoint,
            apiKey,
            model,
            isAzure);

        return IngestionOrchestrator
            .PrepareVectorsFromPdfAsync(
                pdfBytes,
                embeddingClient,
                maxPages,
                maxCharsPerPage,
                chunkSize,
                overlap,
                maxChunksTotal)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Debug / inspection helper.
    /// Extracts PDF text with explicit page markers.
    /// No chunking. No embeddings. No external calls.
    /// </summary>
    public static string ExtractTextFromPdfWithPageMarkers(
        byte[] pdfBytes,
        int maxPages,
        int maxCharsPerPage)
    {
        if (pdfBytes == null || pdfBytes.Length == 0)
            throw new ArgumentException("PDF bytes are empty.", nameof(pdfBytes));

        // Defensive defaults (ODC-safe)
        maxPages = (maxPages <= 0) ? 50 : maxPages;
        maxCharsPerPage = (maxCharsPerPage < 0) ? 0 : maxCharsPerPage;

        return PdfTextExtractor.ExtractTextFromPdfWithPageMarkers(
            pdfBytes,
            maxPages,
            maxCharsPerPage);
    }

    public static List<VectorSearchResultDto> SearchTopKByCosine(
    string queryVectorJson,
    List<VectorCandidateDto> candidates,
    int topK,
    float minScore)
    {
        if (string.IsNullOrWhiteSpace(queryVectorJson))
            throw new ArgumentException("Query vector JSON is empty.", nameof(queryVectorJson));

        if (candidates == null)
            throw new ArgumentNullException(nameof(candidates));

        topK = (topK <= 0) ? 10 : topK;

        var queryVector = VectorSerializer.DeserializeFromJson(queryVectorJson);

        var internalCandidates = new List<VectorCandidate>(candidates.Count);
        foreach (var c in candidates)
        {
            if (c == null) continue;
            internalCandidates.Add(new VectorCandidate
            {
                Id = c.Id ?? string.Empty,
                VectorJson = c.VectorJson ?? "[]",
                PageNumber = c.PageNumber,
                ChunkIndex = c.ChunkIndex,
                ChunkText = c.ChunkText,
                ChunkHash = c.ChunkHash
            });
        }

        var results = TopKVectorSearch.Search(queryVector, internalCandidates, topK, minScore);

        int rank = 1;
        var dto = new List<VectorSearchResultDto>(results.Count);
        foreach (var r in results)
        {
            dto.Add(new VectorSearchResultDto
            {
                Rank = rank++,
                Id = r.Id,
                Score = r.Score,
                PageNumber = r.PageNumber,
                ChunkIndex = r.ChunkIndex,
                ChunkText = r.ChunkText,
                ChunkHash = r.ChunkHash
            });
        }
        return dto;
    }

    public static List<VectorSearchResultDto> EmbedAndSearchTopK(
        string queryText,
        string endpoint,
        string apiKey,
        string model,
        bool isAzure,
        List<VectorCandidateDto> candidates,
        int topK,
        float minScore)
    {
        if (string.IsNullOrWhiteSpace(queryText))
            throw new ArgumentException("Query text is empty.", nameof(queryText));

        if (candidates == null)
            throw new ArgumentNullException(nameof(candidates));

        topK = (topK <= 0) ? 10 : topK;

        var embeddingClient = new EmbeddingClient(_httpClient, endpoint, apiKey, model, isAzure);

        var queryVector = embeddingClient.GenerateEmbeddingAsync(queryText)
            .GetAwaiter()
            .GetResult();

        var internalCandidates = new List<VectorCandidate>(candidates.Count);
        foreach (var c in candidates)
        {
            if (c == null) continue;
            internalCandidates.Add(new VectorCandidate
            {
                Id = c.Id ?? string.Empty,
                VectorJson = c.VectorJson ?? "[]",
                PageNumber = c.PageNumber,
                ChunkIndex = c.ChunkIndex,
                ChunkText = c.ChunkText,
                ChunkHash = c.ChunkHash
            });
        }

        var results = TopKVectorSearch.Search(queryVector, internalCandidates, topK, minScore);

        int rank = 1;
        var dto = new List<VectorSearchResultDto>(results.Count);
        foreach (var r in results)
        {
            dto.Add(new VectorSearchResultDto
            {
                Rank = rank++,
                Id = r.Id,
                Score = r.Score,
                PageNumber = r.PageNumber,
                ChunkIndex = r.ChunkIndex,
                ChunkText = r.ChunkText,
                ChunkHash = r.ChunkHash
            });
        }

        return dto;
    }

}
