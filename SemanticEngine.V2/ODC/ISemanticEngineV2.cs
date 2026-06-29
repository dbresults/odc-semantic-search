// ISemanticEngineV2.cs
using OutSystems.ExternalLibraries.SDK;

namespace SemanticEngine.V2.ODC
{
    [OSInterface(Description = "PDF ingestion and semantic search for RAG applications. Extracts text from PDFs, generates embeddings, and finds the most relevant chunks for a given query. No vector database required — store vectors as text in your ODC entities.", IconResourceName = "SemanticEngine.V2.Logo.png")]
    public interface ISemanticEngineV2
    {
        [OSAction(
            Description = "Ingests a PDF document — extracts text, splits it into chunks, and generates an embedding vector for each chunk. Returns a JSON array of vector records to store in your ODC entities. Call this once per document during your RAG ingestion flow.",
            ReturnName = "VectorRecordsJson"
        )]
        string PrepareVectorsFromPdfJson(
            [OSParameter(Description = "Raw bytes of the PDF file to ingest.")]
            byte[] pdfBytes,
            [OSParameter(Description = "Embedding API base URL. Use https://api.openai.com for OpenAI, or your Azure OpenAI endpoint for Azure.")]
            string endpoint,
            [OSParameter(Description = "API key for the embedding service.")]
            string apiKey,
            [OSParameter(Description = "Embedding model name (e.g. text-embedding-3-small). Must match the model used at search time.")]
            string model,
            [OSParameter(Description = "Set to True when using Azure OpenAI, False for standard OpenAI.")]
            bool isAzure,
            [OSParameter(Description = "Maximum number of pages to process. Set to 0 to process all pages (default 50).")]
            int maxPages,
            [OSParameter(Description = "Maximum characters to read per page. Set to 0 for no limit (default 4000).")]
            int maxCharsPerPage,
            [OSParameter(Description = "Target chunk size in characters. Smaller chunks are more precise; larger chunks give more context (default 1000).")]
            int chunkSize,
            [OSParameter(Description = "Number of characters to overlap between consecutive chunks. Helps preserve context at chunk boundaries (default 150).")]
            int overlap,
            [OSParameter(Description = "Maximum total number of chunks to generate per document. Set to 0 for no limit (default 200).")]
            int maxChunksTotal
        );

        [OSAction(
            Description = "Extracts plain text from a PDF with page markers (===PAGE:N===) between each page. No chunking or embedding is performed and no API call is made. Use this to inspect what text will be ingested before committing to a full ingest.",
            ReturnName = "ExtractedText"
        )]
        string ExtractTextFromPdfWithPageMarkers(
            [OSParameter(Description = "Raw bytes of the PDF file to extract text from.")]
            byte[] pdfBytes,
            [OSParameter(Description = "Maximum number of pages to extract. Set to 0 to extract all pages (default 50).")]
            int maxPages,
            [OSParameter(Description = "Maximum characters to read per page. Set to 0 for no limit (default 4000).")]
            int maxCharsPerPage
        );

        [OSAction(
            Description = "Embeds the query text via the API and returns the most relevant stored chunks ranked by cosine similarity. Also returns the query vector so you can cache it and use SearchTopKByCosineJson on repeat searches to avoid redundant API calls.",
            ReturnName = "SearchResultsJson"
        )]
        string EmbedAndSearchTopKJson(
            [OSParameter(Description = "The user's search query or question.")]
            string queryText,
            [OSParameter(Description = "Embedding API base URL. Must match the endpoint used during ingestion.")]
            string endpoint,
            [OSParameter(Description = "API key for the embedding service.")]
            string apiKey,
            [OSParameter(Description = "Embedding model name. Must match the model used during ingestion.")]
            string model,
            [OSParameter(Description = "Set to True when using Azure OpenAI, False for standard OpenAI.")]
            bool isAzure,
            [OSParameter(Description = "JSON array of stored vector records to search against. Pass your ingested VectorChunk records here.")]
            string candidatesJson,
            [OSParameter(Description = "Number of top results to return (default 5).")]
            int topK,
            [OSParameter(Description = "Minimum cosine similarity score to include in results, from 0.0 to 1.0. Set to 0 for no threshold.")]
            float minScore,
            [OSParameter(Description = "Output: the query vector as a serialized JSON float array. Store this alongside HashText(queryText) to cache the vector and skip the embedding API call on repeat searches.")]
            out string queryVectorJson
        );

        [OSAction(
            Description = "Finds the most relevant stored chunks using a pre-computed query vector. No embedding API call is made. Use this when you have already cached the query vector to avoid repeat API calls.",
            ReturnName = "SearchResultsJson"
        )]
        string SearchTopKByCosineJson(
            [OSParameter(Description = "The query vector as a serialized JSON float array. Obtain this from EmbedTextToVectorJson and cache it for reuse.")]
            string queryVectorJson,
            [OSParameter(Description = "JSON array of stored vector records to search against. Pass your ingested VectorChunk records here.")]
            string candidatesJson,
            [OSParameter(Description = "Number of top results to return (default 5).")]
            int topK,
            [OSParameter(Description = "Minimum cosine similarity score to include in results, from 0.0 to 1.0. Set to 0 for no threshold.")]
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
