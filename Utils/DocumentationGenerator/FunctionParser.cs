using System.Text.RegularExpressions;

namespace Utils.DocumentationGenerator;

/// <summary>
/// Parses Azure Function C# files to extract metadata
/// </summary>
public class FunctionParser
{
    public async Task<List<FunctionMetadata>> ParseFunctionsAsync(string functionsDirectory)
    {
        var functions = new List<FunctionMetadata>();
        var functionFiles = Directory.GetFiles(functionsDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"));

        foreach (var file in functionFiles)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var functionMetadata = ParseFunctionFile(content, file);
                if (functionMetadata != null)
                {
                    functions.Add(functionMetadata);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing {file}: {ex.Message}");
            }
        }

        return functions;
    }

    private FunctionMetadata? ParseFunctionFile(string content, string filePath)
    {
        // Check if file contains HttpTrigger (we only want HTTP-triggered functions)
        if (!content.Contains("HttpTrigger"))
        {
            return null;
        }

        var metadata = new FunctionMetadata
        {
            FilePath = filePath,
            Category = ExtractCategoryFromPath(filePath)
        };

        // Extract class name
        var classMatch = Regex.Match(content, @"public class (\w+)");
        if (classMatch.Success)
        {
            metadata.ClassName = classMatch.Groups[1].Value;
        }

        // Find all Function attributes and their associated HttpTrigger attributes
        var functionPattern = @"\[Function\(""([^""]+)""\)\]\s*public[^{]*(\[HttpTrigger[^\]]*\])";
        var functionMatches = Regex.Matches(content, functionPattern, RegexOptions.Singleline);

        foreach (Match match in functionMatches)
        {
            var functionName = match.Groups[1].Value;
            var httpTriggerAttr = match.Groups[2].Value;

            // This is a valid function, use the first one found
            metadata.FunctionName = functionName;
            
            // Extract route from HttpTrigger attribute
            var routeMatch = Regex.Match(httpTriggerAttr, @"Route\s*=\s*""([^""]+)""");
            if (routeMatch.Success)
            {
                metadata.Route = routeMatch.Groups[1].Value;
                ExtractRouteParameters(metadata);
            }
            else
            {
                // If no route specified, function name might be the route
                metadata.Route = functionName.ToLower();
            }

            // Extract HTTP methods from HttpTrigger attribute
            var httpTriggerContent = httpTriggerAttr;
            var methodMatches = Regex.Matches(httpTriggerContent, @"""(get|post|put|delete|patch)""", RegexOptions.IgnoreCase);
            foreach (Match methodMatch in methodMatches)
            {
                var method = methodMatch.Groups[1].Value.ToUpper();
                if (!metadata.HttpMethods.Contains(method))
                {
                    metadata.HttpMethods.Add(method);
                }
            }

            break; // Use the first valid function found
        }

        // If no methods found, try to infer from function name
        if (!metadata.HttpMethods.Any() && !string.IsNullOrEmpty(metadata.FunctionName))
        {
            if (metadata.FunctionName.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
                metadata.HttpMethods.Add("GET");
            else if (metadata.FunctionName.StartsWith("Upsert", StringComparison.OrdinalIgnoreCase))
            {
                metadata.HttpMethods.Add("PUT");
                metadata.HttpMethods.Add("POST");
            }
            else if (metadata.FunctionName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase))
                metadata.HttpMethods.Add("DELETE");
            else if (metadata.FunctionName.StartsWith("Create", StringComparison.OrdinalIgnoreCase))
                metadata.HttpMethods.Add("POST");
        }

        // Extract validation rules and required fields
        ExtractValidationRules(content, metadata);

        // Extract description from comments
        metadata.Description = ExtractDescription(content, metadata.FunctionName, metadata.ClassName);

        // Only return if we found essential information
        if (!string.IsNullOrEmpty(metadata.FunctionName) && metadata.HttpMethods.Any())
        {
            return metadata;
        }

        return null;
    }

    private string ExtractCategoryFromPath(string filePath)
    {
        var parts = filePath.Split('/', '\\');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i] == "Functions" && i + 1 < parts.Length)
            {
                return parts[i + 1];
            }
        }
        return "General";
    }

    private void ExtractRouteParameters(FunctionMetadata metadata)
    {
        var routeParamMatch = Regex.Match(metadata.Route, @"\{(\w+)\??}");
        if (routeParamMatch.Success)
        {
            metadata.RouteParameter = routeParamMatch.Groups[1].Value;
            metadata.Parameters.Add(new FunctionParameter
            {
                Name = metadata.RouteParameter,
                Type = "string",
                IsRequired = !metadata.Route.Contains("?}"),
                Description = $"Unique identifier for the {metadata.Category.ToLower()}",
                Source = "route"
            });
        }
    }

    private void ExtractValidationRules(string content, FunctionMetadata metadata)
    {
        // Extract required field validations
        var requiredMatches = Regex.Matches(content, @"if \(string\.IsNullOrWhiteSpace\(model\.(\w+)\)\)");
        foreach (Match match in requiredMatches)
        {
            metadata.RequiredFields.Add(match.Groups[1].Value);
        }

        // Extract error messages for validation rules
        var errorMatches = Regex.Matches(content, @"errors\.Add\(""([^""]+)""\)");
        foreach (Match match in errorMatches)
        {
            metadata.ValidationRules.Add(match.Groups[1].Value);
        }
    }

    private string ExtractDescription(string content, string functionName, string className)
    {
        // Look for XML documentation comments above the function
        var functionPattern = $@"/// <summary>\s*/// ([^<]+)\s*/// </summary>\s*.*?\[Function\(""{Regex.Escape(functionName)}""\)";
        var xmlDocMatch = Regex.Match(content, functionPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (xmlDocMatch.Success)
        {
            return xmlDocMatch.Groups[1].Value.Trim().Replace("///", "").Trim();
        }

        // Look for class-level XML documentation
        var classPattern = $@"/// <summary>\s*/// ([^<]+)\s*/// </summary>\s*.*?public class {Regex.Escape(className)}";
        var classDocMatch = Regex.Match(content, classPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (classDocMatch.Success)
        {
            return classDocMatch.Groups[1].Value.Trim().Replace("///", "").Trim();
        }

        // Generate description based on function name and category
        var category = ExtractCategoryFromPath("");
        if (functionName.StartsWith("Upsert", StringComparison.OrdinalIgnoreCase))
        {
            var entity = ExtractEntityFromFunctionName(functionName);
            return $"Creates or updates a {entity.ToLower()} based on the provided parameters. Supports both new creation and modification of existing records.";
        }
        else if (functionName.StartsWith("Get") && functionName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            var entity = ExtractEntityFromFunctionName(functionName);
            return $"Retrieves a list of {entity.ToLower()}s with optional filtering by various criteria including category, author, and publication status.";
        }
        else if (functionName.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
        {
            var entity = ExtractEntityFromFunctionName(functionName);
            return $"Retrieves a specific {entity.ToLower()} by its unique identifier (slug) with optional media information.";
        }
        else if (functionName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase))
        {
            var entity = ExtractEntityFromFunctionName(functionName);
            return $"Permanently deletes a specific {entity.ToLower()} by its unique identifier (slug).";
        }
        else if (functionName.Contains("Media") || functionName.Contains("Image"))
        {
            return $"Manages media attachments and relationships for content items. Supports upload, association, and removal operations.";
        }
        else if (functionName.Contains("Contact"))
        {
            return "Processes contact form submissions and sends notifications to the appropriate recipients.";
        }
        else if (functionName.Contains("GitHub"))
        {
            return "Integrates with GitHub API to retrieve and display repository information and activity data.";
        }

        return $"Azure Function endpoint for {functionName} operations";
    }

    private string ExtractEntityFromFunctionName(string functionName)
    {
        var cleanName = functionName
            .Replace("Upsert", "")
            .Replace("Get", "")
            .Replace("Delete", "")
            .Replace("Function", "")
            .Replace("Async", "");
        
        // Handle specific known entities
        if (cleanName.Contains("BlogPost"))
            return "blog post";
        if (cleanName.Contains("Author"))
            return "author";
        if (cleanName.Contains("Book"))
            return "book";
        if (cleanName.Contains("Portfolio"))
            return "portfolio piece";
        if (cleanName.Contains("Media") || cleanName.Contains("Image"))
            return "media item";
        
        // Remove trailing 's' for plurals
        cleanName = cleanName.TrimEnd('s');
        
        return string.IsNullOrEmpty(cleanName) ? "item" : cleanName.ToLower();
    }
}