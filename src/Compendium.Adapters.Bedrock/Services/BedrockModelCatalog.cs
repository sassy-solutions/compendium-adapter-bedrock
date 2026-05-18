// -----------------------------------------------------------------------
// <copyright file="BedrockModelCatalog.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Bedrock.Services;

/// <summary>
/// Static, hand-curated catalog of Bedrock-hosted foundation models known at adapter
/// ship time.
/// </summary>
/// <remarks>
/// <para>
/// Bedrock exposes <c>ListFoundationModels</c> on the management plane
/// (<c>AWSSDK.Bedrock</c>) — a separate SDK from the runtime plane
/// (<c>AWSSDK.BedrockRuntime</c>) that the data-path adapter binds against. Pulling
/// the live list would require an extra control-plane permission (<c>bedrock:ListFoundationModels</c>),
/// which is a strong policy ask for a runtime adapter. This curated list keeps the IAM
/// surface minimal (<c>bedrock:InvokeModel</c> + <c>bedrock:Converse*</c>).
/// </para>
/// <para>
/// Callers can still target any model id via <see cref="CompletionRequest.Model"/> —
/// the catalog is informational, not enforcement.
/// </para>
/// </remarks>
internal static class BedrockModelCatalog
{
    /// <summary>Curated list returned from <see cref="IAIProvider.ListModelsAsync"/>.</summary>
    public static readonly IReadOnlyList<AIModel> KnownModels = new AIModel[]
    {
        new()
        {
            Id = "anthropic.claude-3-5-sonnet-20241022-v2:0",
            Name = "Claude 3.5 Sonnet v2",
            Provider = "bedrock",
            ContextWindow = 200_000,
            MaxOutputTokens = 8_192,
            SupportsStreaming = true,
            SupportsEmbeddings = false,
            SupportsVision = true,
            SupportsTools = true,
            PricingInputPerMillion = 3m,
            PricingOutputPerMillion = 15m,
        },
        new()
        {
            Id = "anthropic.claude-3-haiku-20240307-v1:0",
            Name = "Claude 3 Haiku",
            Provider = "bedrock",
            ContextWindow = 200_000,
            MaxOutputTokens = 4_096,
            SupportsStreaming = true,
            SupportsEmbeddings = false,
            SupportsVision = true,
            SupportsTools = true,
            PricingInputPerMillion = 0.25m,
            PricingOutputPerMillion = 1.25m,
        },
        new()
        {
            Id = "meta.llama3-1-70b-instruct-v1:0",
            Name = "Llama 3.1 70B Instruct",
            Provider = "bedrock",
            ContextWindow = 128_000,
            MaxOutputTokens = 2_048,
            SupportsStreaming = true,
            SupportsEmbeddings = false,
            SupportsVision = false,
            SupportsTools = true,
            PricingInputPerMillion = 0.99m,
            PricingOutputPerMillion = 0.99m,
        },
        new()
        {
            Id = "mistral.mistral-large-2407-v1:0",
            Name = "Mistral Large 2 (24.07)",
            Provider = "bedrock",
            ContextWindow = 128_000,
            MaxOutputTokens = 8_192,
            SupportsStreaming = true,
            SupportsEmbeddings = false,
            SupportsVision = false,
            SupportsTools = true,
            PricingInputPerMillion = 2m,
            PricingOutputPerMillion = 6m,
        },
        new()
        {
            Id = "amazon.nova-pro-v1:0",
            Name = "Amazon Nova Pro",
            Provider = "bedrock",
            ContextWindow = 300_000,
            MaxOutputTokens = 5_120,
            SupportsStreaming = true,
            SupportsEmbeddings = false,
            SupportsVision = true,
            SupportsTools = true,
            PricingInputPerMillion = 0.80m,
            PricingOutputPerMillion = 3.20m,
        },
        new()
        {
            Id = "amazon.titan-embed-text-v2:0",
            Name = "Amazon Titan Embed Text v2",
            Provider = "bedrock",
            ContextWindow = 8_192,
            MaxOutputTokens = null,
            SupportsStreaming = false,
            SupportsEmbeddings = true,
            SupportsVision = false,
            SupportsTools = false,
            PricingInputPerMillion = 0.02m,
            PricingOutputPerMillion = null,
        },
        new()
        {
            Id = "cohere.embed-english-v3",
            Name = "Cohere Embed English v3",
            Provider = "bedrock",
            ContextWindow = 512,
            MaxOutputTokens = null,
            SupportsStreaming = false,
            SupportsEmbeddings = true,
            SupportsVision = false,
            SupportsTools = false,
            PricingInputPerMillion = 0.10m,
            PricingOutputPerMillion = null,
        },
    };
}
