namespace SemanticEngine.V2.Contracts;

public sealed class VectorCandidateDto
{
    public string Id { get; set; } = string.Empty;
    public string VectorJson { get; set; } = "[]";
    public int PageNumber { get; set; }
    public int ChunkIndex { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public string ChunkHash { get; set; } = string.Empty;
}