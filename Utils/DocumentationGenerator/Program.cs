using Utils.DocumentationGenerator;

namespace Utils.DocumentationGenerator;

/// <summary>
/// Console application to generate Azure Functions documentation
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("🔧 Azure Functions Documentation Generator");
            Console.WriteLine("==========================================");
            Console.WriteLine();

            // Get the project root directory
            var currentDir = Directory.GetCurrentDirectory();
            var projectRoot = FindProjectRoot(currentDir);
            
            if (projectRoot == null)
            {
                Console.WriteLine("❌ Could not find project root directory");
                return;
            }

            Console.WriteLine($"📁 Project root: {projectRoot}");

            // Define paths
            var functionsDirectory = Path.Combine(projectRoot, "src", "Functions");
            var outputDirectory = Path.Combine(projectRoot, "docs", "functions");

            Console.WriteLine($"📁 Functions directory: {functionsDirectory}");
            Console.WriteLine($"📁 Output directory: {outputDirectory}");
            Console.WriteLine();

            if (!Directory.Exists(functionsDirectory))
            {
                Console.WriteLine($"❌ Functions directory not found: {functionsDirectory}");
                return;
            }

            // Create output directory if it doesn't exist
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
                Console.WriteLine($"✅ Created output directory: {outputDirectory}");
            }

            // Parse functions
            Console.WriteLine("🔍 Parsing Azure Functions...");
            var parser = new FunctionParser();
            var functions = await parser.ParseFunctionsAsync(functionsDirectory);

            Console.WriteLine($"✅ Found {functions.Count} HTTP-triggered functions");
            
            foreach (var function in functions.OrderBy(f => f.Category).ThenBy(f => f.FunctionName))
            {
                var methods = string.Join(", ", function.HttpMethods);
                Console.WriteLine($"   - {function.Category}/{function.FunctionName} ({methods})");
            }
            Console.WriteLine();

            // Generate documentation
            Console.WriteLine("📝 Generating Markdown documentation...");
            var generator = new MarkdownGenerator();
            await generator.GenerateDocumentationAsync(functions, outputDirectory);

            Console.WriteLine();
            Console.WriteLine($"✅ Documentation generated successfully!");
            Console.WriteLine($"📂 Output location: {outputDirectory}");
            Console.WriteLine("🎉 Ready for import to Notion or other documentation platforms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error generating documentation: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static string? FindProjectRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        
        while (current != null)
        {
            // Look for the solution file or specific project structure
            if (current.GetFiles("*.sln").Any() || 
                (current.GetDirectories("src").Any() && current.GetDirectories("Utils").Any()))
            {
                return current.FullName;
            }
            
            current = current.Parent;
        }
        
        return null;
    }
}