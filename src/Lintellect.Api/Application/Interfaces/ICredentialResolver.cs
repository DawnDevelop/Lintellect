
using Lintellect.Shared.Models;

namespace Lintellect.Api.Application.Interfaces;
/// <summary>
/// Resolves Git provider credentials for a given analysis context.
/// Prefers request-supplied credentials, otherwise falls back to configured defaults.
/// </summary>
public interface ICredentialResolver
{
    /// <summary>
    /// Resolves an access token and optional organization URI (for Azure DevOps) for the given request.
    /// </summary>
    /// <param name="request">The analysis request containing provider context.</param>
    /// <returns>Tuple of (accessToken, orgUri). For GitHub, orgUri will be null.</returns>
    (string? accessToken, Uri? orgUri) Resolve(AnalysisRequest request);
}


