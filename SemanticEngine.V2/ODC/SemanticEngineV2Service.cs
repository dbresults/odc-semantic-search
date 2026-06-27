// SemanticEngineV2Service.cs
using System.Text.Json;
using SemanticEngine.V2.Contracts;

namespace SemanticEngine.V2.ODC
{
    public sealed class SemanticEngineV2Service : ISemanticEngineV2
    {
        private const string DefaultEmbeddingModel = "text-embedding-3-small";
        private const int DefaultTopK = 5;

        private const int DefaultMaxPages = 50;
        private const int DefaultMaxCharsPerPage = 4000;

        private const int DefaultChunkSize = 1000;
        private const int DefaultOverlap = 150;
        private const int DefaultMaxChunksTotal = 200;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public string PrepareVectorsFromPdfJson(
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
            model = NormalizeModel(model);
            maxPages = NormalizeMaxPages(maxPages);
            maxCharsPerPage = NormalizeMaxCharsPerPage(maxCharsPerPage);
            chunkSize = NormalizeChunkSize(chunkSize);
            overlap = NormalizeOverlap(overlap, chunkSize);
            maxChunksTotal = NormalizeMaxChunksTotal(maxChunksTotal);

            var records = SemanticEngineV2Facade.PrepareVectorsFromPdf(
                pdfBytes,
                endpoint,
                apiKey,
                model,
                isAzure,
                maxPages,
                maxCharsPerPage,
                chunkSize,
                overlap,
                maxChunksTotal);

            return JsonSerializer.Serialize(records, JsonOpts);
        }

        public string ExtractTextFromPdfWithPageMarkers(
            byte[] pdfBytes,
            int maxPages,
            int maxCharsPerPage)
        {
            maxPages = NormalizeMaxPages(maxPages);
            maxCharsPerPage = NormalizeMaxCharsPerPage(maxCharsPerPage);

            return SemanticEngineV2Facade.ExtractTextFromPdfWithPageMarkers(
                pdfBytes,
                maxPages,
                maxCharsPerPage);
        }

        public string EmbedAndSearchTopKJson(
            string queryText,
            string endpoint,
            string apiKey,
            string model,
            bool isAzure,
            string candidatesJson,
            int topK,
            float minScore)
        {
            model = NormalizeModel(model);
            topK = NormalizeTopK(topK);

            var candidates = DeserializeCandidates(candidatesJson);

            var results = SemanticEngineV2Facade.EmbedAndSearchTopK(
                queryText,
                endpoint,
                apiKey,
                model,
                isAzure,
                candidates,
                topK,
                minScore);

            return JsonSerializer.Serialize(results, JsonOpts);
        }

        public string SearchTopKByCosineJson(
            string queryVectorJson,
            string candidatesJson,
            int topK,
            float minScore)
        {
            topK = NormalizeTopK(topK);

            var candidates = DeserializeCandidates(candidatesJson);

            var results = SemanticEngineV2Facade.SearchTopKByCosine(
                queryVectorJson,
                candidates,
                topK,
                minScore);

            return JsonSerializer.Serialize(results, JsonOpts);
        }

        public string HashText(string inputText)
        {
            return SemanticEngineV2Facade.HashText(inputText);
        }

        private static List<VectorCandidateDto> DeserializeCandidates(string candidatesJson)
        {
            if (string.IsNullOrWhiteSpace(candidatesJson))
                return new List<VectorCandidateDto>();

            var list = JsonSerializer.Deserialize<List<VectorCandidateDto>>(candidatesJson, JsonOpts);
            return list ?? new List<VectorCandidateDto>();
        }

        private static string NormalizeModel(string model)
            => string.IsNullOrWhiteSpace(model) ? DefaultEmbeddingModel : model.Trim();

        private static int NormalizeTopK(int topK)
            => topK <= 0 ? DefaultTopK : topK;

        private static int NormalizeMaxPages(int maxPages)
            => maxPages <= 0 ? DefaultMaxPages : maxPages;

        private static int NormalizeMaxCharsPerPage(int maxCharsPerPage)
            => maxCharsPerPage <= 0 ? DefaultMaxCharsPerPage : maxCharsPerPage;

        private static int NormalizeChunkSize(int chunkSize)
            => chunkSize <= 0 ? DefaultChunkSize : chunkSize;

        private static int NormalizeMaxChunksTotal(int maxChunksTotal)
            => maxChunksTotal <= 0 ? DefaultMaxChunksTotal : maxChunksTotal;

        private static int NormalizeOverlap(int overlap, int chunkSize)
        {
            overlap = overlap < 0 ? DefaultOverlap : overlap;

            if (chunkSize <= 0) chunkSize = DefaultChunkSize;
            if (overlap >= chunkSize)
                overlap = Math.Max(0, chunkSize / 6);

            return overlap;
        }
    }
}
