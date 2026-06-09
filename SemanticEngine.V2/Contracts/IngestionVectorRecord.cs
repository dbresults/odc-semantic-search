namespace SemanticEngine.V2.Contracts;

public sealed class IngestionVectorRecord
{
    public int PageNumber { get; set; }        // 1-based
    public int ChunkIndex { get; set; }        // 0-based per page
    public string ChunkText { get; set; } = string.Empty;

    public string VectorJson { get; set; } = "[]";
    public string ChunkHash { get; set; } = string.Empty;
}
