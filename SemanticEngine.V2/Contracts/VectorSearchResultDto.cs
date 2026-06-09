namespace SemanticEngine.V2.Contracts;

public sealed class VectorSearchResultDto
{
    public int Rank { get; set; }
    public float Score { get; set; }
    public string Id { get; set; } = string.Empty;
    // Include the metadata in the result
    public int PageNumber { get; set; }
    public int ChunkIndex { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public string ChunkHash { get; set; } = string.Empty;
}