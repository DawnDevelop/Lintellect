using System.Reflection;
using System.Text;

namespace devops_pr_analyzer.Services.AI.Prompts;

/// <summary>
/// Service for loading and rendering prompt templates from embedded resources.
/// Supports variable interpolation and template composition.
/// </summary>
internal sealed class PromptTemplateService
{
    private const string TemplateResourcePrefix = "devops_pr_analyzer.Services.AI.Prompts.Templates";
    private readonly Assembly _assembly = typeof(PromptTemplateService).Assembly;

    /// <summary>
    /// Loads a template from embedded resources and replaces variables.
    /// </summary>
    /// <param name="templateName">Name of the template file (without .md extension)</param>
    /// <param name="variables">Dictionary of variable names to values for interpolation</param>
    /// <returns>Rendered template with variables replaced</returns>
    public string RenderTemplate(string templateName, Dictionary<string, string>? variables = null)
    {
        var template = LoadTemplate(templateName);
        
        if (variables is null || variables.Count == 0)
            return template;

        return ReplaceVariables(template, variables);
    }

    /// <summary>
    /// Loads a template from embedded resources.
    /// </summary>
    private string LoadTemplate(string templateName)
    {
        var resourceName = $"{TemplateResourcePrefix}.{templateName}.md";
        
        using var stream = _assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Template '{templateName}' not found in embedded resources.");
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Replaces {{variableName}} placeholders with actual values.
    /// </summary>
    private static string ReplaceVariables(string template, Dictionary<string, string> variables)
    {
        var result = template;
        
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{{{key}}}}}", value ?? string.Empty);
        }
        
        return result;
    }

    /// <summary>
    /// Builds a structured prompt by combining multiple sections.
    /// </summary>
    public string BuildPrompt(params string[] sections)
    {
        var builder = new StringBuilder();
        
        foreach (var section in sections)
        {
            if (!string.IsNullOrWhiteSpace(section))
            {
                builder.AppendLine(section);
                builder.AppendLine();
            }
        }
        
        return builder.ToString().TrimEnd();
    }
}
