using Markdig;

namespace Utils.Markdown;

/// <summary>
/// Utility class for processing markdown content into HTML
/// </summary>
public static class MarkdownProcessor
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions() // Includes tables, task lists, etc.
        .UseAutoIdentifiers() // Auto-generate IDs for headers
        .UseSoftlineBreakAsHardlineBreak() // Convert line breaks to <br>
        .Build();

    /// <summary>
    /// Converts markdown text to HTML
    /// </summary>
    /// <param name="markdownText">The markdown text to convert</param>
    /// <returns>HTML representation of the markdown</returns>
    public static string ToHtml(string? markdownText)
    {
        if (string.IsNullOrWhiteSpace(markdownText))
        {
            return string.Empty;
        }

        try
        {
            return Markdig.Markdown.ToHtml(markdownText, Pipeline);
        }
        catch (Exception)
        {
            // If markdown processing fails, return the original text
            // This ensures graceful degradation
            return markdownText;
        }
    }

    /// <summary>
    /// Converts markdown text to plain text (strips HTML and markdown formatting)
    /// </summary>
    /// <param name="markdownText">The markdown text to convert</param>
    /// <returns>Plain text representation of the markdown</returns>
    public static string ToPlainText(string? markdownText)
    {
        if (string.IsNullOrWhiteSpace(markdownText))
        {
            return string.Empty;
        }

        try
        {
            return Markdig.Markdown.ToPlainText(markdownText, Pipeline);
        }
        catch (Exception)
        {
            // If markdown processing fails, return the original text
            return markdownText ?? string.Empty;
        }
    }
}