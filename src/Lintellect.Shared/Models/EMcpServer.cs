namespace Lintellect.Shared.Models;

/// <summary>
/// Enumeration of supported MCP (Model Context Protocol) servers for code analysis.
/// Only includes coding-related MCP servers that are relevant for static code analysis.
/// </summary>
public enum EMcpServer
{
    /// <summary>
    /// No MCP server configured - use standard AI analysis only.
    /// </summary>
    None = 0,

    /// <summary>
    /// Context7 MCP server - provides access to comprehensive documentation and code examples.
    /// Useful for getting up-to-date documentation and best practices.
    /// </summary>
    Context7 = 1,

    /// <summary>
    /// Microsoft Docs MCP server - provides access to official Microsoft documentation.
    /// Useful for .NET, Azure, and Microsoft technology stack analysis.
    /// </summary>
    MicrosoftDocs = 2,

    /// <summary>
    /// GitHub MCP server - provides access to GitHub repositories, issues, and pull requests.
    /// Useful for analyzing code patterns, finding similar issues, and understanding project context.
    /// </summary>
    GitHub = 3,

    /// <summary>
    /// Stack Overflow MCP server - provides access to Stack Overflow Q&A data.
    /// Useful for finding solutions to common coding problems and best practices.
    /// </summary>
    StackOverflow = 4,

    /// <summary>
    /// Code Analysis MCP server - provides specialized code analysis tools and patterns.
    /// Useful for advanced static analysis, security scanning, and code quality metrics.
    /// </summary>
    CodeAnalysis = 5
}
