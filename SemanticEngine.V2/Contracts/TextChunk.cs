namespace SemanticEngine.V2.Contracts;

public sealed class TextChunk
{
    public int PageNumber { get; set; }     // 1-based
    public int ChunkIndex { get; set; }     // 0-based per page
    public string Text { get; set; } = string.Empty;
}
