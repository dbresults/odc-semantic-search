# SemanticEngine.V2

An OutSystems ODC External Library that provides self-contained PDF ingestion and semantic search primitives for building RAG (Retrieval-Augmented Generation) applications.

No vector database required — embeddings are stored as JSON strings in your ODC data entities.

## What it does

| Step | Action | Description |
|------|--------|-------------|
| Ingest | `PrepareVectorsFromPdfJson` | PDF → extract text → chunk → embed → return vector records |
| Debug | `ExtractTextFromPdfWithPageMarkers` | PDF → plain text with `===PAGE:N===` markers, no API call |
| Search (end-to-end) | `EmbedAndSearchTopKJson` | Embed query text → cosine similarity → top-K results |
| Search (pre-computed) | `SearchTopKByCosineJson` | Cosine similarity only, no API call — use when you already have the query vector |

## Prerequisites

- An [OpenAI](https://platform.openai.com) or [Azure OpenAI](https://azure.microsoft.com/en-us/products/ai-services/openai-service) account with access to an embeddings model (e.g. `text-embedding-3-small`)
- OutSystems ODC Portal access to upload an External Library

## ODC Setup

1. Download `SemanticEngine.V2.zip` from [Releases](../../releases) or build it yourself (see below)
2. In the **ODC Portal**, go to **External Libraries → Upload**
3. Upload the ZIP — ODC will surface four actions under the library name
4. Add the library to your ODC app and configure your embedding endpoint and API key as **Site Properties** or **Secrets**

## Actions

### `PrepareVectorsFromPdfJson`

Ingests a PDF and returns a JSON array of vector records. Call this once per document and store the results in your data entities.

| Parameter | Type | Description |
|-----------|------|-------------|
| `pdfBytes` | Binary | Raw PDF file bytes |
| `endpoint` | Text | Embedding API base URL (e.g. `https://api.openai.com`) |
| `apiKey` | Text | API key |
| `model` | Text | Model name (e.g. `text-embedding-3-small`) |
| `isAzure` | Boolean | `True` for Azure OpenAI, `False` for OpenAI |
| `maxPages` | Integer | Max pages to process (0 = all, default 50) |
| `maxCharsPerPage` | Integer | Character cap per page (0 = unlimited, default 4000) |
| `chunkSize` | Integer | Target chunk size in characters (default 1000) |
| `overlap` | Integer | Overlap between chunks in characters (default 150) |
| `maxChunksTotal` | Integer | Hard cap on total chunks per document (0 = unlimited, default 200) |

**Returns:** JSON array of `IngestionVectorRecord`:
```json
[
  {
    "PageNumber": 1,
    "ChunkIndex": 0,
    "ChunkText": "...",
    "VectorJson": "[0.123, -0.456, ...]",
    "ChunkHash": "A3F9..."
  }
]
```

Store each record in an ODC entity. `ChunkHash` (SHA-256 of the chunk text) can be used for deduplication.

---

### `ExtractTextFromPdfWithPageMarkers`

Returns the raw text of a PDF with explicit page markers. No embedding API call is made. Useful for inspecting what text will be ingested before committing API calls.

**Returns:** Plain text string with `===PAGE:N===` delimiters between pages.

---

### `EmbedAndSearchTopKJson`

Embeds the query text via the API and returns the most similar stored chunks. Use this in your search/query flow.

| Parameter | Type | Description |
|-----------|------|-------------|
| `queryText` | Text | The user's question or search query |
| `endpoint` | Text | Embedding API base URL |
| `apiKey` | Text | API key |
| `model` | Text | Must match the model used during ingestion |
| `isAzure` | Boolean | Provider flag |
| `candidatesJson` | Text | JSON array of `VectorCandidateDto` (your stored records) |
| `topK` | Integer | Number of results to return (default 5) |
| `minScore` | Decimal | Minimum cosine similarity threshold, 0.0–1.0 (0 = no filter) |

**Returns:** JSON array of `VectorSearchResultDto` ordered by score descending:
```json
[
  {
    "Rank": 1,
    "Id": "...",
    "Score": 0.91,
    "PageNumber": 3,
    "ChunkIndex": 2,
    "ChunkText": "...",
    "ChunkHash": "..."
  }
]
```

---

### `SearchTopKByCosineJson`

Pure cosine similarity search with no API call. Use this when you already have the query vector (e.g. you cached it to avoid re-embedding the same query).

| Parameter | Type | Description |
|-----------|------|-------------|
| `queryVectorJson` | Text | Pre-computed embedding as a JSON float array |
| `candidatesJson` | Text | JSON array of `VectorCandidateDto` |
| `topK` | Integer | Number of results to return |
| `minScore` | Decimal | Minimum cosine similarity threshold |

---

## Typical RAG flow in ODC

```
[Upload Document]
  → PrepareVectorsFromPdfJson
  → For each record: Create VectorChunk entity record (store ChunkText + VectorJson)

[User Query]
  → EmbedAndSearchTopKJson (pass stored VectorChunk records as candidatesJson)
  → Take top 1–3 ChunkText values
  → Append to your LLM prompt as context
  → Call your LLM action
```

## Building from source

```bash
git clone https://github.com/YOUR_USERNAME/SemanticEngine.V2
cd Lib_SemanticKernel_Core_V2

# Build
dotnet build SemanticEngineV2.sln

# Publish and package
dotnet publish SemanticEngine.V2/SemanticEngine.V2.csproj -c Release -o ./package_output
cd package_output && zip -j ../SemanticEngine.V2.zip *.dll *.json *.pdb
```

Upload `SemanticEngine.V2.zip` to the ODC Portal.

## Running the test harness

```bash
export OPENAI_API_KEY="sk-..."
export OPENAI_ENDPOINT="https://api.openai.com"   # optional
export OPENAI_MODEL="text-embedding-3-small"       # optional
export OPENAI_IS_AZURE="true"                      # optional, for Azure OpenAI

cd SemanticEngine.Test && dotnet run
```

Place a PDF named `File.pdf` in the `SemanticEngine.Test/` directory before running.

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `OutSystems.ExternalLibraries.SDK` | 1.5.0 | ODC integration |
| `PdfPig` | 0.1.12 | PDF text extraction |

No Semantic Kernel, no Azure SDK, no OpenAI SDK — the library calls the embeddings REST API directly via `HttpClient`.

## License

MIT
