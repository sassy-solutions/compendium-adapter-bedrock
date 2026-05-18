// -----------------------------------------------------------------------
// <copyright file="BedrockOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Compendium.Adapters.Bedrock.Options;

/// <summary>
/// Configuration for the AWS Bedrock <see cref="Compendium.Abstractions.AI.IAIProvider"/> adapter.
/// </summary>
/// <remarks>
/// <para>
/// One adapter, many model families : Anthropic Claude, Meta Llama, Mistral, Amazon Nova,
/// Amazon Titan, Cohere — all reached through the unified <c>Converse</c> API. Embeddings
/// (Titan Embed, Cohere Embed) go through <c>InvokeModel</c>.
/// </para>
/// <para>
/// Credentials are optional. When <see cref="AccessKey"/> / <see cref="SecretKey"/> are
/// left empty, the AWS SDK's standard credential chain kicks in (env vars, shared
/// profile, EC2 instance profile, EKS pod identity, IMDSv2, ...). Prefer IAM roles
/// in production.
/// </para>
/// </remarks>
public sealed class BedrockOptions
{
    /// <summary>
    /// Default <see cref="DefaultModelId"/> : Claude 3.5 Sonnet v2 on Bedrock.
    /// </summary>
    public const string DefaultClaude35SonnetV2ModelId = "anthropic.claude-3-5-sonnet-20241022-v2:0";

    /// <summary>
    /// Default <see cref="EmbeddingModelId"/> : Amazon Titan Embed Text v2.
    /// </summary>
    public const string DefaultTitanEmbedTextV2ModelId = "amazon.titan-embed-text-v2:0";

    /// <summary>
    /// Configuration section name used by <c>IConfiguration.GetSection(...)</c>.
    /// </summary>
    public const string SectionName = "Compendium:Adapters:Bedrock";

    /// <summary>
    /// AWS region (e.g. <c>us-east-1</c>, <c>eu-west-3</c>). Required.
    /// </summary>
    /// <remarks>
    /// Bedrock model availability varies by region. Cross-region inference profiles
    /// (e.g. <c>us.anthropic.claude-3-5-sonnet-20241022-v2:0</c>) can be passed as the
    /// model id for higher throughput.
    /// </remarks>
    [Required]
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// AWS access key id. Leave null to use the SDK's default credential chain.
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// AWS secret access key. Leave null to use the SDK's default credential chain.
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Optional STS session token for temporary credentials (e.g. role assumption).
    /// </summary>
    public string? SessionToken { get; set; }

    /// <summary>
    /// Default Bedrock model id used when <see cref="CompletionRequest.Model"/> is unset.
    /// Defaults to <see cref="DefaultClaude35SonnetV2ModelId"/>.
    /// </summary>
    /// <remarks>
    /// Examples — text completions :
    /// <list type="bullet">
    ///   <item><c>anthropic.claude-3-5-sonnet-20241022-v2:0</c></item>
    ///   <item><c>anthropic.claude-3-haiku-20240307-v1:0</c> (cheapest Claude)</item>
    ///   <item><c>meta.llama3-1-70b-instruct-v1:0</c></item>
    ///   <item><c>mistral.mistral-large-2407-v1:0</c></item>
    ///   <item><c>amazon.nova-pro-v1:0</c></item>
    /// </list>
    /// </remarks>
    [Required]
    public string DefaultModelId { get; set; } = DefaultClaude35SonnetV2ModelId;

    /// <summary>
    /// Bedrock embedding model id used by <see cref="IAIProvider.EmbedAsync"/>
    /// when <see cref="EmbeddingRequest.Model"/> is unset. Defaults to
    /// <see cref="DefaultTitanEmbedTextV2ModelId"/>.
    /// </summary>
    /// <remarks>
    /// Supported families (auto-detected by id prefix) :
    /// <list type="bullet">
    ///   <item><c>amazon.titan-embed-*</c> — request body <c>{ inputText: "..." }</c></item>
    ///   <item><c>cohere.embed-*</c> — request body <c>{ texts: [...], input_type: "search_document" }</c></item>
    /// </list>
    /// </remarks>
    [Required]
    public string EmbeddingModelId { get; set; } = DefaultTitanEmbedTextV2ModelId;

    /// <summary>
    /// Default <c>max_tokens</c> applied to outgoing requests when the caller does not
    /// supply one. Defaults to <c>4096</c>.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int DefaultMaxTokens { get; set; } = 4096;

    /// <summary>
    /// Per-request timeout. Default : 120 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Maximum number of retry attempts the underlying AWS SDK should perform for
    /// transient errors. Default : <c>3</c>.
    /// </summary>
    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Returns <see langword="true"/> when all required values are populated.
    /// </summary>
    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(Region)
        && !string.IsNullOrWhiteSpace(DefaultModelId)
        && !string.IsNullOrWhiteSpace(EmbeddingModelId)
        && DefaultMaxTokens > 0
        && Timeout > TimeSpan.Zero
        && MaxRetries >= 0;
}
