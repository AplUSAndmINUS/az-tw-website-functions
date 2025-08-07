namespace Utils.DocumentationGenerator;

/// <summary>
/// Represents metadata extracted from an Azure Function
/// </summary>
public class FunctionMetadata
{
    public string FunctionName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public List<string> HttpMethods { get; set; } = new();
    public string? RouteParameter { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<FunctionParameter> Parameters { get; set; } = new();
    public string? RequestModelType { get; set; }
    public string? ResponseModelType { get; set; }
    public List<string> RequiredFields { get; set; } = new();
    public List<string> ValidationRules { get; set; } = new();
    public bool RequiresAuth { get; set; } = true;
    public string FilePath { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Represents a parameter for an Azure Function
/// </summary>
public class FunctionParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // "route", "query", "body"
}