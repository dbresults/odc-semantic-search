namespace SemanticEngine.V2.Contracts;

public sealed class IngestionVectorRecord
{
    public int PageNumber { get; set; }        // 1-based
    public int ChunkIndex { get; set; }        // 0-based per page
    public string ChunkText { get; set; } = string.Empty;

    // Store this into VectorMemory.VectorJson (or whatever field you use)
    public string VectorJson { get; set; } = "[]";

    // Optional: store a stable hash for dedupe/idempotency if you want later
    public string ChunkHash { get; set; } = string.Empty;
}
