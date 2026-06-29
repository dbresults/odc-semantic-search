// ISemanticEngineV2.cs
using OutSystems.ExternalLibraries.SDK;

namespace SemanticEngine.V2.ODC
{
    [OSInterface(Description = "ODC-native vector ingestion and semantic search primitives (V2). JSON contracts.", IconResourceName = "SemanticEngine.V2.Logo.png")]
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

        [OSAction(
            Description = "Generates a SHA-256 hash of the input text. Use this to create a stable cache key for a query string.",
            ReturnName = "HashHex"
        )]
        string HashText(
            [OSParameter(Description = "The plain text string to hash.")]
            string inputText
        );

        [OSAction(
            Description = "Embeds a text string and returns the vector as a serialized JSON float array. Use this to pre-compute and cache a query vector so repeated searches avoid redundant API calls.",
            ReturnName = "VectorJson"
        )]
        string EmbedTextToVectorJson(
            [OSParameter(Description = "The text to embed.")]
            string inputText,
            [OSParameter(Description = "Embedding API base URL (e.g. https://api.openai.com).")]
            string endpoint,
            [OSParameter(Description = "API key.")]
            string apiKey,
            [OSParameter(Description = "Embedding model name (e.g. text-embedding-3-small).")]
            string model,
            [OSParameter(Description = "True for Azure OpenAI, False for OpenAI.")]
            bool isAzure
        );
    }
}
