using System;
using System.Collections.Generic;
using System.Linq; // Added for .Select()
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SemanticEngine.V2.Chunking;
using SemanticEngine.V2.Contracts;
using SemanticEngine.V2.Embeddings;
using SemanticEngine.V2.Pdf;
using SemanticEngine.V2.Serialization;

namespace SemanticEngine.V2.Core;

public static class IngestionOrchestrator
{
    private static readonly SemaphoreSlim _embeddingSemaphore = new(10, 10);

    public static async Task<List<IngestionVectorRecord>> PrepareVectorsFromPdfAsync(
        byte[] pdfBytes,
        EmbeddingClient embeddingClient,
        int maxPages = 0,
        int maxCharsPerPage = 0,
        int chunkSize = 1000,
        int overlap = 150,
        int maxChunksTotal = 0)
    {
        if (embeddingClient == null) throw new ArgumentNullException(nameof(embeddingClient));

        // 1) Extract pages
        var pages = PdfTextExtractor.ExtractTextFromPdfPages(pdfBytes, maxPages, maxCharsPerPage);

        // 2) Chunk pages
        var chunks = TextChunker.ChunkPages(pages, chunkSize, overlap);

        if (maxChunksTotal > 0 && chunks.Count > maxChunksTotal)
        {
            chunks = chunks.GetRange(0, maxChunksTotal);
        }

        // 3) Generate embeddings in PARALLEL with a concurrency cap to avoid rate-limit errors
        var tasks = chunks.Select(async chunk =>
        {
            await _embeddingSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var vector = await embeddingClient.GenerateEmbeddingAsync(chunk.Text).ConfigureAwait(false);
                var vectorJson = VectorSerializer.SerializeToJson(vector);

                return new IngestionVectorRecord
                {
                    PageNumber = chunk.PageNumber,
                    ChunkIndex = chunk.ChunkIndex,
                    ChunkText = chunk.Text,
                    VectorJson = vectorJson,
                    ChunkHash = ComputeSha256(chunk.Text)
                };
            }
            finally { _embeddingSemaphore.Release(); }
        });

        // Fire all requests at once and wait for the group to finish
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        return results.ToList();
    }

    private static string ComputeSha256(string input)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash); 
    }
}