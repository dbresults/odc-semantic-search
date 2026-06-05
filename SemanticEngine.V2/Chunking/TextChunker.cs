// TextChunker.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SemanticEngine.V2.Contracts;

namespace SemanticEngine.V2.Chunking;

public static class TextChunker
{
    private static readonly Regex DeHyphenate    = new(@"(\w+)-\n(\w+)", RegexOptions.Compiled);
    private static readonly Regex SingleNewline  = new(@"(?<!\n)\n(?!\n)", RegexOptions.Compiled);
    private static readonly Regex HorizontalWs   = new(@"[ \t]{2,}", RegexOptions.Compiled);
    public static List<TextChunk> ChunkPages(
        List<PdfPageText> pages,
        int chunkSize = 1000,
        int overlap = 150)
    {
        if (pages == null) throw new ArgumentNullException(nameof(pages));
        if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
        
        var chunks = new List<TextChunk>();

        foreach (var page in pages)
        {
            if (page == null || string.IsNullOrWhiteSpace(page.Text))
                continue;

            var normalizedText = NormalizeText(page.Text);
            var paragraphs = SplitIntoParagraphs(normalizedText);

            var buffer = new StringBuilder();
            int chunkIndex = 0;

            foreach (var paragraph in paragraphs)
            {
                if (paragraph.Length > chunkSize)
                {
                    FlushBuffer(chunks, page.PageNumber, ref buffer, ref chunkIndex);

                    ChunkOversizedParagraph(
                        chunks,
                        page.PageNumber,
                        paragraph,
                        ref chunkIndex,
                        chunkSize,
                        overlap);

                    continue;
                }

                if (buffer.Length + paragraph.Length + 1 > chunkSize)
                {
                    FlushBuffer(chunks, page.PageNumber, ref buffer, ref chunkIndex);
                }

                buffer.AppendLine(paragraph);
            }

            FlushBuffer(chunks, page.PageNumber, ref buffer, ref chunkIndex);
        }

        return chunks;
    }

    private static void FlushBuffer(
        List<TextChunk> chunks,
        int pageNumber,
        ref StringBuilder buffer,
        ref int chunkIndex)
    {
        if (buffer.Length == 0) return;

        chunks.Add(new TextChunk
        {
            PageNumber = pageNumber,
            ChunkIndex = chunkIndex++,
            Text = buffer.ToString().Trim()
        });

        buffer.Clear();
    }

    private static void ChunkOversizedParagraph(
        List<TextChunk> chunks,
        int pageNumber,
        string text,
        ref int chunkIndex,
        int chunkSize,
        int overlap)
    {
        int start = 0;
        while (start < text.Length)
        {
            int end = Math.Min(start + chunkSize, text.Length);

            if (end < text.Length)
            {
                // 1. END BOUNDARY FIX: Look back for a space so we don't slice a word
                int lastSpace = text.LastIndexOf(' ', end, end - start);
                if (lastSpace > start)
                {
                    end = lastSpace;
                }
            }

            var slice = text.Substring(start, end - start).Trim();
            if (!string.IsNullOrWhiteSpace(slice))
            {
                chunks.Add(new TextChunk
                {
                    PageNumber = pageNumber,
                    ChunkIndex = chunkIndex++,
                    Text = slice
                });
            }

            if (end >= text.Length) break;

            // 2. START BOUNDARY FIX (Fixes "letion"):
            // Start at the mathematical overlap point...
            int nextStart = end - overlap;
            if (nextStart < 0) nextStart = 0;

            // ...then search FORWARD for the next space to ensure the overlap starts with a full word
            if (nextStart < end)
            {
                int nextSpace = text.IndexOf(' ', nextStart, end - nextStart);
                if (nextSpace != -1)
                {
                    nextStart = nextSpace + 1; // Start right after the space
                }
            }

            start = nextStart;

            // Final safety: if we aren't making forward progress, force a jump to the end boundary
            if (start <= (end - chunkSize) || start >= end) 
            {
                start = end;
            }
        }
    }

    private static List<string> SplitIntoParagraphs(string text)
    {
        return text
            .Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
        normalized = DeHyphenate.Replace(normalized, "$1$2");    // rejoin hyphenated line breaks
        normalized = SingleNewline.Replace(normalized, " ");     // single \n → space; \n\n preserved
        normalized = HorizontalWs.Replace(normalized, " ");      // collapse runs of spaces/tabs

        return normalized.Trim();
    }
}