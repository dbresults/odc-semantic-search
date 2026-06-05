using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SemanticEngine.V2.Contracts;
using UglyToad.PdfPig;

namespace SemanticEngine.V2.Pdf;

public static class PdfTextExtractor
{
    private static readonly Regex MultiSpace = new(@"[ \t]+", RegexOptions.Compiled);
    private static readonly Regex MultiNewline = new(@"\n{3,}", RegexOptions.Compiled);

    public static List<PdfPageText> ExtractTextFromPdfPages(
        byte[] pdfBytes,
        int maxPages = 0,
        int maxCharsPerPage = 0)
    {
        if (pdfBytes == null || pdfBytes.Length == 0)
            throw new ArgumentException("PDF bytes are empty.", nameof(pdfBytes));

        using var stream = new System.IO.MemoryStream(pdfBytes);
        using var document = PdfDocument.Open(stream);

        int pageCount = document.NumberOfPages;
        int pagesToRead = (maxPages > 0) ? Math.Min(maxPages, pageCount) : pageCount;

        var result = new List<PdfPageText>(capacity: pagesToRead);

        for (int pageNumber = 1; pageNumber <= pagesToRead; pageNumber++)
        {
            var page = document.GetPage(pageNumber);
            string raw = page.Text ?? string.Empty;

            string normalized = NormalizePdfText(raw);

            if (maxCharsPerPage > 0 && normalized.Length > maxCharsPerPage)
                normalized = normalized.Substring(0, maxCharsPerPage);

            if (string.IsNullOrWhiteSpace(normalized))
                continue;

            result.Add(new PdfPageText
            {
                PageNumber = pageNumber,
                Text = normalized
            });
        }

        return result;
    }

    // Optional helper if you still want a single string output somewhere
    public static string ExtractTextFromPdfWithPageMarkers(
        byte[] pdfBytes,
        int maxPages = 0,
        int maxCharsPerPage = 0)
    {
        var pages = ExtractTextFromPdfPages(pdfBytes, maxPages, maxCharsPerPage);

        var sb = new StringBuilder();
        foreach (var p in pages)
        {
            sb.AppendLine($"===PAGE:{p.PageNumber}===");
            sb.AppendLine(p.Text);
            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }

    private static string NormalizePdfText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        var lines = text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0);

        var rebuilt = string.Join("\n", lines);

        rebuilt = MultiSpace.Replace(rebuilt, " ");
        rebuilt = MultiNewline.Replace(rebuilt, "\n\n");

        return rebuilt.Trim();
    }
}
