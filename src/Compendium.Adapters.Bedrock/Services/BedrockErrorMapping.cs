// -----------------------------------------------------------------------
// <copyright file="BedrockErrorMapping.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Amazon.BedrockRuntime;

namespace Compendium.Adapters.Bedrock.Services;

/// <summary>
/// Maps <see cref="AmazonBedrockRuntimeException"/> to the Compendium
/// <see cref="Error"/> shape via <see cref="AIErrors"/>.
/// </summary>
/// <remarks>
/// Status-code matrix :
/// <list type="bullet">
///   <item><c>401/403</c> → <see cref="AIErrors.InvalidApiKey"/></item>
///   <item><c>402</c> → <see cref="AIErrors.InsufficientCredits"/></item>
///   <item><c>404</c> → <see cref="AIErrors.ModelNotFound"/></item>
///   <item><c>413</c> → <see cref="AIErrors.TokenLimitExceeded"/></item>
///   <item><c>429</c> → <see cref="AIErrors.RateLimitExceeded"/></item>
///   <item><c>503</c> → <see cref="AIErrors.ProviderUnavailable"/></item>
///   <item>everything else → <see cref="AIErrors.ProviderError"/></item>
/// </list>
/// </remarks>
internal static class BedrockErrorMapping
{
    /// <summary>
    /// Maps a Bedrock exception to a Compendium <see cref="Error"/>.
    /// </summary>
    /// <param name="ex">Source exception.</param>
    /// <param name="modelId">Logical model id, used for <c>ModelNotFound</c>.</param>
    /// <returns>The mapped error.</returns>
    public static Error Map(AmazonBedrockRuntimeException ex, string modelId)
    {
        ArgumentNullException.ThrowIfNull(ex);

        return ex.StatusCode switch
        {
            HttpStatusCode.Unauthorized => AIErrors.InvalidApiKey(),
            HttpStatusCode.Forbidden => AIErrors.InvalidApiKey(),
            HttpStatusCode.PaymentRequired => AIErrors.InsufficientCredits(),
            HttpStatusCode.NotFound => AIErrors.ModelNotFound(modelId),
            HttpStatusCode.RequestEntityTooLarge => AIErrors.TokenLimitExceeded(0, 0),
            HttpStatusCode.TooManyRequests => AIErrors.RateLimitExceeded(),
            HttpStatusCode.ServiceUnavailable => AIErrors.ProviderUnavailable("bedrock"),
            _ => AIErrors.ProviderError(ex.Message, ex.ErrorCode),
        };
    }
}
