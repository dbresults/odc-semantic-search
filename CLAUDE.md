# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build the solution
dotnet build SemanticEngineV2.sln

# Run the manual test harness — set env vars first, then run
export OPENAI_API_KEY="sk-..."                        # required
export OPENAI_ENDPOINT="https://api.openai.com"       # optional, this is the default
export OPENAI_MODEL="text-embedding-3-small"          # optional, this is the default
export OPENAI_IS_AZURE="true"                         # optional, only for Azure OpenAI
cd SemanticEngine.Test && dotnet run

# Publish and package for ODC upload
dotnet publish SemanticEngine.V2/SemanticEngine.V2.csproj -c Release -o ./package_output
cd package_output && zip -j ../SemanticEngine.V2.zip *.dll *.json *.pdb
```

`SemanticEngine.Test` is a console app (not an xUnit/NUnit test project) — it directly exercises the facade with a real PDF file and live API calls. The API key is read from environment variables; it is never hardcoded in source.

## Architecture

This is a .NET 8 class library published as an **OutSystems ODC External Library**. It provides a self-contained PDF-to-vector ingestion and semantic search pipeline with no external SDKs — only `PdfPig` for PDF parsing and raw `HttpClient` for embedding API calls.

### Layer order (top to bottom)

```
ODC Layer        ISemanticEngineV2 [OSInterface]  ← ODC discovers this interface
                 SemanticEngineV2Service           ← normalizes inputs, returns JSON strings
                         ↓
Facade           SemanticEngineV2Facade            ← static, synchronous, ODC-safe
                         ↓
Core             IngestionOrchestrator             ← async pipeline: extract → chunk → embed (parallel)
                         ↓
Leaf modules     Pdf/   Chunking/   Embeddings/   Retrieval/   Serialization/
```

**ODC constraint:** ODC calls sync methods only. The `SemanticEngineV2Facade` bridges async internals with `.GetAwaiter().GetResult()` — do not remove this pattern. All `ISemanticEngineV2` methods return `string` (JSON-encoded results), because ODC's type system cannot handle generic collections directly.

### Key design decisions

- **No DI container.** ODC instantiates `SemanticEngineV2Service` directly. A static `_httpClient` in `SemanticEngineV2Facade` is shared across calls — `EmbeddingClient` sets auth headers on the `HttpRequestMessage`, not on the client, so sharing is safe.
- **Parallel embeddings with rate-limit cap.** `IngestionOrchestrator` fires all chunk embeddings concurrently via `Task.WhenAll`, throttled by a static `SemaphoreSlim(10)`. Do not raise this above ~20 — OpenAI will return HTTP 429s.
- **Two entry paths.** `SemanticEngineV2Service` (ODC path, JSON contracts) and `SemanticEngineV2Facade` (typed .NET contracts, used directly in the test harness and by the service). Both normalize parameters — `SemanticEngineV2Service.Normalize*` methods handle ODC defaults; the facade has its own defensive defaults as a fallback.
- **`SearchTopKByCosineJson` vs `EmbedAndSearchTopKJson`.** These are intentionally different: `EmbedAndSearchTopKJson` takes query text and calls the embedding API; `SearchTopKByCosineJson` takes a pre-computed `queryVectorJson` (float array as JSON string) and does pure cosine math with no HTTP call. Use the latter when you already have the query vector (e.g. cached from a previous call).
- **VectorJson as storage format.** Vectors are stored as JSON strings (`float[]` serialized with `"R"` round-trip format and invariant culture) rather than binary, to stay compatible with ODC's text-based data types.

### Namespace map

| Namespace | Responsibility |
|---|---|
| `SemanticEngine.V2.ODC` | ODC interface + service implementation |
| `SemanticEngine.V2` (root) | `SemanticEngineV2Facade` — the public typed API |
| `SemanticEngine.V2.Core` | `IngestionOrchestrator` — pipeline coordinator |
| `SemanticEngine.V2.Pdf` | `PdfTextExtractor` using PdfPig |
| `SemanticEngine.V2.Chunking` | `TextChunker` — paragraph-aware, word-boundary-safe with overlap |
| `SemanticEngine.V2.Embeddings` | `EmbeddingClient` — supports both OpenAI and Azure OpenAI |
| `SemanticEngine.V2.Retrieval` | `CosineSimilarity`, `TopKVectorSearch` |
| `SemanticEngine.V2.Serialization` | `VectorSerializer` — locale-safe float[] ↔ JSON |
| `SemanticEngine.V2.Contracts` | DTOs shared across layers |

### Azure vs OpenAI embedding URLs

`EmbeddingClient.BuildUrl()` handles both providers. For Azure, the URL pattern is `{baseEndpoint}/openai/deployments/{model}/embeddings?api-version=2024-10-21` with `api-key` header. For OpenAI, it appends `/v1/embeddings` to the base and uses `Authorization: Bearer` header. The Azure API version constant is in `EmbeddingClient.cs` — update it when Microsoft releases a newer stable GA version.

### Chunking behavior

`TextChunker` splits each PDF page into paragraph-sized chunks first (split on `\n\n`), then further splits any oversized paragraphs with word-boundary-aware sliding windows and overlap. `NormalizeText` converts single newlines to spaces but preserves double newlines so paragraph structure survives into `SplitIntoParagraphs`.

### ODC package

The upload-ready ZIP is produced by publishing the library project in Release mode and zipping the output. The ZIP contains `SemanticEngine.V2.dll`, its `.deps.json`, `OutSystems.ExternalLibraries.SDK.dll`, and all `UglyToad.PdfPig*.dll` dependencies. The API key is **never** in the package — ODC callers supply it as a parameter at runtime (from a site property or secret).

### Output directories

- `package_output/` — fresh Release publish output, source for the ODC ZIP
- `SemanticEngine.V2.zip` — the ODC upload package (regenerate via the publish+zip commands above)
- `publish/` / `odc_pkg/` / `deploy/` — older build artifacts, can be ignored
