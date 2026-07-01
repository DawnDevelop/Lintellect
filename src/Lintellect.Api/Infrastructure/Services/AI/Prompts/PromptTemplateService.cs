using System.Reflection;
using System.Text;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Infrastructure.Services.AI.Prompts;

/// <summary>
/// Service for loading and rendering prompt templates from embedded resources.
/// Supports variable interpolation and template composition.
/// </summary>
internal sealed class PromptTemplateService
{
    private static string TemplateResourcePrefix => $"{typeof(PromptBuilder).Namespace}.Templates";
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

        return variables is null || variables.Count == 0 ? template : ReplaceVariables(template, variables);
    }

    /// <summary>
    /// Loads a language-specific template from embedded resources using EProgrammingLanguage enum.
    /// </summary>
    /// <param name="templateName">Name of the template file (without .md extension)</param>
    /// <param name="language">Programming language enum</param>
    /// <param name="variables">Dictionary of variable names to values for interpolation</param>
    /// <returns>Rendered template with variables replaced</returns>
    public string RenderLanguageTemplate(
        LanguagePromptTemplates languagePromptTemplate,
        EProgrammingLanguage language,
        Dictionary<string, string>? variables = null,
        bool enableGlobalInstructions = false)
    {
        var templateName = AvailablePrompts.LanguagePrompts[languagePromptTemplate];
        var template = LoadLanguageTemplate(templateName, language);

        if (enableGlobalInstructions)
        {
            var globalInstructionsName = AvailablePrompts.GeneralPrompts[GeneralPromptTemplates.GlobalInstructionsPrompt];
            var globalInstructionsPrompt = RenderTemplate(globalInstructionsName);
            template = $"{globalInstructionsPrompt}\n\n{template}";
        }

        // Inline templates share a chunk of boilerplate (suggestion budget, JSON format rules,
        // diff-extraction rules) that we keep in one shared file, placed via a {{commonInlineRules}}
        // placeholder. Inject it into the template BEFORE variable substitution so the placeholders
        // the shared file itself contains (e.g. {{totalFilesInPR}}) are resolved in the same pass.
        if (languagePromptTemplate == LanguagePromptTemplates.InlineSuggestionsSystemPrompt)
        {
            var commonRulesName = AvailablePrompts.GeneralPrompts[GeneralPromptTemplates.InlineSuggestionsCommonRules];
            var commonRules = LoadTemplate(commonRulesName);
            template = template.Replace("{{commonInlineRules}}", commonRules);
        }

        return variables is null || variables.Count == 0 ? template : ReplaceVariables(template, variables);
    }

    /// <summary>
    /// Gets all available embedded resource names for debugging.
    /// </summary>
    public IEnumerable<string> GetAvailableResources()
    {
        return _assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(TemplateResourcePrefix))
            .OrderBy(name => name);
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
    /// Loads a language-specific template from embedded resources using EProgrammingLanguage enum.
    /// </summary>
    private string LoadLanguageTemplate(string templateName, EProgrammingLanguage language)
    {
        // Check if this language has specific templates
        if (!HasSpecificTemplates(language))
        {
            // Fallback to default template for unsupported languages
            return LoadTemplate(templateName);
        }

        // Get the folder name for this language
        var folderName = ToFolderName(language);

        // Try language-specific template first
        var languageResourceName = $"{TemplateResourcePrefix}.{folderName}.{templateName}.md";
        var languageStream = _assembly.GetManifestResourceStream(languageResourceName);

        if (languageStream != null)
        {
            using var reader = new StreamReader(languageStream);
            return reader.ReadToEnd();
        }

        // Fallback to default template
        return LoadTemplate(templateName);
    }

    /// <summary>
    /// Maps EProgrammingLanguage enum to the folder name used in embedded resources.
    /// </summary>
    private static string ToFolderName(EProgrammingLanguage language)
    {
        return language.ToString();
    }

    /// <summary>
    /// Checks if a programming language has specific templates available.
    /// </summary>
    private static bool HasSpecificTemplates(EProgrammingLanguage language)
    {
        return language switch
        {
            EProgrammingLanguage.CSharp => true,
            EProgrammingLanguage.JavaScript => true,
            EProgrammingLanguage.Python => true,
            EProgrammingLanguage.TypeScript => true,
            EProgrammingLanguage.Java => true,
            _ => false // Go, Ruby, PHP, Swift, Kotlin fall back to generic templates
        };
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
