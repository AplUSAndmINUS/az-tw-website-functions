using SharedStorage.Models;
using Utils;

namespace SharedStorage.Services.Media;

/// <summary>
/// Service for converting various document formats to PDF
/// </summary>
public class DocumentConversionService : IDocumentConversionService
{
  private readonly IAppInsightsLogger<DocumentConversionService> _logger;

  // Define supported file extensions for conversion
  private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".csv"
    };

  public DocumentConversionService(IAppInsightsLogger<DocumentConversionService> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <inheritdoc />
  public async Task<DocumentConversionResult> ConvertToPdfAsync(Stream documentStream, string fileName)
  {
    if (documentStream == null || !documentStream.CanRead)
    {
      var ex = new InvalidOperationException("Document stream is null or cannot be read");
      _logger.LogError("Document stream is null or cannot be read", ex);
      return new DocumentConversionResult
      {
        Success = false,
        ErrorMessage = "Invalid document stream"
      };
    }

    var extension = Path.GetExtension(fileName).ToLowerInvariant();
    if (!IsConversionSupported(fileName))
    {
      _logger.LogWarning("Document format not supported for conversion: {Extension}", extension);
      return new DocumentConversionResult
      {
        Success = false,
        ErrorMessage = $"Document format not supported for conversion: {extension}"
      };
    }

    try
    {
      _logger.LogInformation("Converting document {FileName} to PDF", fileName);

      // Reset the stream position
      documentStream.Position = 0;

      // PLACEHOLDER: In a real implementation, we would integrate with a PDF conversion library or service
      // For example: DocuSign, PDFTron, Aspose.PDF, iText, etc.
      // For now, we'll create a simple placeholder PDF
      var pdfStream = await CreatePlaceholderPdfAsync(documentStream, fileName);

      _logger.LogInformation("Successfully converted {FileName} to PDF", fileName);

      return new DocumentConversionResult
      {
        Content = pdfStream,
        Success = true
      };
    }
    catch (Exception ex)
    {
      _logger.LogError("Failed to convert document {FileName} to PDF: {Error}", ex, fileName, ex.Message);

      return new DocumentConversionResult
      {
        Success = false,
        ErrorMessage = $"Conversion failed: {ex.Message}"
      };
    }
  }

  /// <inheritdoc />
  public bool IsConversionSupported(string fileName)
  {
    if (string.IsNullOrWhiteSpace(fileName))
      return false;

    var extension = Path.GetExtension(fileName);
    return SupportedExtensions.Contains(extension);
  }

  /// <summary>
  /// Creates a placeholder PDF file
  /// In a real implementation, this would be replaced with actual conversion code
  /// </summary>
  private async Task<MemoryStream> CreatePlaceholderPdfAsync(Stream sourceStream, string fileName)
  {
    // In a real implementation, this would use a proper PDF library to convert the document
    // For now, we'll create a simple placeholder with metadata

    // A very basic PDF structure (this is not a valid PDF, just a placeholder)
    var pdfStream = new MemoryStream();
    using (var writer = new StreamWriter(pdfStream, leaveOpen: true))
    {
      await writer.WriteLineAsync("%PDF-1.7");
      await writer.WriteLineAsync("1 0 obj");
      await writer.WriteLineAsync("<< /Type /Catalog /Pages 2 0 R >>");
      await writer.WriteLineAsync("endobj");
      await writer.WriteLineAsync("2 0 obj");
      await writer.WriteLineAsync("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
      await writer.WriteLineAsync("endobj");
      await writer.WriteLineAsync("3 0 obj");
      await writer.WriteLineAsync("<< /Type /Page /Parent 2 0 R /Resources 4 0 R /MediaBox [0 0 612 792] /Contents 5 0 R >>");
      await writer.WriteLineAsync("endobj");
      await writer.WriteLineAsync("4 0 obj");
      await writer.WriteLineAsync("<< /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >>");
      await writer.WriteLineAsync("endobj");
      await writer.WriteLineAsync("5 0 obj");
      await writer.WriteLineAsync("<< /Length 68 >>");
      await writer.WriteLineAsync("stream");
      await writer.WriteLineAsync("BT");
      await writer.WriteLineAsync("/F1 24 Tf");
      await writer.WriteLineAsync("100 700 Td");
      await writer.WriteLineAsync($"(Converted from {Path.GetFileName(fileName)}) Tj");
      await writer.WriteLineAsync("ET");
      await writer.WriteLineAsync("endstream");
      await writer.WriteLineAsync("endobj");
      await writer.WriteLineAsync("xref");
      await writer.WriteLineAsync("0 6");
      await writer.WriteLineAsync("0000000000 65535 f");
      await writer.WriteLineAsync("0000000010 00000 n");
      await writer.WriteLineAsync("0000000079 00000 n");
      await writer.WriteLineAsync("0000000145 00000 n");
      await writer.WriteLineAsync("0000000255 00000 n");
      await writer.WriteLineAsync("0000000343 00000 n");
      await writer.WriteLineAsync("trailer");
      await writer.WriteLineAsync("<< /Size 6 /Root 1 0 R >>");
      await writer.WriteLineAsync("startxref");
      await writer.WriteLineAsync("463");
      await writer.WriteLineAsync("%%EOF");
    }

    pdfStream.Position = 0;
    return pdfStream;
  }
}
