using SharedStorage.Models;

namespace SharedStorage.Services.Media;

/// <summary>
/// Interface for converting various document formats to PDF
/// </summary>
public interface IDocumentConversionService
{
  /// <summary>
  /// Converts a document to PDF format
  /// </summary>
  /// <param name="documentStream">Document content stream</param>
  /// <param name="fileName">Original file name with extension</param>
  /// <returns>Conversion result containing the PDF stream</returns>
  Task<DocumentConversionResult> ConvertToPdfAsync(Stream documentStream, string fileName);

  /// <summary>
  /// Checks if a document format is supported for conversion
  /// </summary>
  /// <param name="fileName">File name with extension</param>
  /// <returns>True if conversion is supported</returns>
  bool IsConversionSupported(string fileName);
}

/// <summary>
/// Result of document conversion process
/// </summary>
public class DocumentConversionResult
{
  /// <summary>
  /// Stream containing the converted document content
  /// </summary>
  public Stream Content { get; set; } = new MemoryStream();

  /// <summary>
  /// Whether the conversion was successful
  /// </summary>
  public bool Success { get; set; }

  /// <summary>
  /// Any error message if conversion failed
  /// </summary>
  public string? ErrorMessage { get; set; }
}
