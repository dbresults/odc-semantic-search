using System;
using System.IO;
using SemanticEngine.V2;

// Set OPENAI_API_KEY (and optionally OPENAI_ENDPOINT, OPENAI_MODEL, OPENAI_IS_AZURE) before running.
string pdfPath   = "File.pdf";
string endpoint  = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? "https://api.openai.com";
string apiKey    = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                   ?? throw new InvalidOperationException("Set the OPENAI_API_KEY environment variable before running.");
string model     = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "text-embedding-3-small";
bool   isAzure   = string.Equals(Environment.GetEnvironmentVariable("OPENAI_IS_AZURE"), "true", StringComparison.OrdinalIgnoreCase);

// HashText smoke test — no API key needed
const string expectedHash = "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824";
string actualHash = SemanticEngineV2Facade.HashText("hello");
if (actualHash != expectedHash)
    throw new Exception($"HashText sanity check failed. Expected: {expectedHash} Got: {actualHash}");
Console.WriteLine($"HashText OK: {actualHash}");

// 2. Load the file
byte[] pdfBytes = File.ReadAllBytes(pdfPath);

try 
{
    Console.WriteLine("Starting Full Ingestion Pipeline Test...");

    // 3. Call the Facade directly (this is what ODC calls) 
    var results = SemanticEngineV2Facade.PrepareVectorsFromPdf(
        pdfBytes,
        endpoint,
        apiKey,
        model,
        isAzure,
        maxPages: 2,         // Keep it small for debugging
        maxCharsPerPage: 4000,
        chunkSize: 1000,
        overlap: 150,
        maxChunksTotal: 10   // Limit chunks to avoid timeouts while debugging
    );

    Console.WriteLine($"\nSuccess! Generated {results.Count} vector records.");
    
    foreach (var record in results)
    {
        Console.WriteLine($"Page {record.PageNumber} | Vector Length: {record.VectorJson.Length} chars");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"--- DEBUGGER CAUGHT ERROR ---");
    Console.WriteLine($"Message: {ex.Message}");
    Console.WriteLine($"Source: {ex.Source}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
}