// ISemanticEngineV2.cs
using OutSystems.ExternalLibraries.SDK;

namespace SemanticEngine.V2.ODC
{
    [OSInterface(Description = "ODC-native vector ingestion and semantic search primitives (V2). JSON contracts.")]
    public interface ISemanticEngineV2
    {
        [OSAction(Description = "Extracts PDF text, chunks it, and generates embeddings. Returns a JSON array of IngestionVectorRecord. Use during RAG ingestion.")]
        string PrepareVectorsFromPdfJson(
            byte[] pdfBytes,
            string endpoint,
            string apiKey,
            string model,
            bool isAzure,
            int maxPages,
            int maxCharsPerPage,
            int chunkSize,
            int overlap,
            int maxChunksTotal
        );

        [OSAction(Description = "Extracts raw text from a PDF with page markers (===PAGE:N===). No chunking or embedding. Use for inspection and debugging.")]
        string ExtractTextFromPdfWithPageMarkers(
            byte[] pdfBytes,
            int maxPages,
            int maxCharsPerPage
        );

        [OSAction(Description = "Embeds the query text via the embedding API, then returns the top-K matching chunks by cosine similarity.")]
        string EmbedAndSearchTopKJson(
            string queryText,
            string endpoint,
            string apiKey,
            string model,
            bool isAzure,
            string candidatesJson,
            int topK,
            float minScore
        );

        [OSAction(Description = "Returns top-K chunks by cosine similarity using a pre-computed query vector JSON. No embedding API call is made.")]
        string SearchTopKByCosineJson(
            string queryVectorJson,
            string candidatesJson,
            int topK,
            float minScore
        );
    }
}
