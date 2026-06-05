namespace SemanticEngine.V2.Contracts;

public sealed class PdfPageText
{
    public int PageNumber { get; set; }  // 1-based page index
    public string Text { get; set; } = string.Empty;
}
