// -----------------------------------------------------------------------
// <copyright file="BedrockEmbeddingPayloads.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Bedrock.Services;

/// <summary>
/// Embedding-model family detection + per-family request / response shapes.
/// </summary>
/// <remarks>
/// <para>
/// Bedrock's <c>Converse</c> API does <em>not</em> cover embeddings, so this adapter
/// branches to <c>InvokeModel</c> with a JSON body whose shape depends on the model
/// family — Titan uses <c>{ inputText }</c> (single string), Cohere uses
/// <c>{ texts: [...] }</c> (batch).
/// </para>
/// </remarks>
internal static class BedrockEmbeddingPayloads
{
    /// <summary>
    /// Detected embedding model family.
    /// </summary>
    public enum Family
    {
        /// <summary>Amazon Titan family (<c>amazon.titan-embed-*</c>).</summary>
        Titan,

        /// <summary>Cohere Embed family (<c>cohere.embed-*</c>).</summary>
        Cohere,
    }

    /// <summary>
    /// Detect the embedding family from a Bedrock model id.
    /// </summary>
    /// <param name="modelId">A Bedrock model id (e.g. <c>amazon.titan-embed-text-v2:0</c>).</param>
    /// <returns>
    /// The matched <see cref="Family"/>, or <see langword="null"/> when the id does not
    /// match any known embedding prefix.
    /// </returns>
    public static Family? Detect(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return null;
        }

        if (modelId.StartsWith("amazon.titan-embed", StringComparison.OrdinalIgnoreCase))
        {
            return Family.Titan;
        }

        if (modelId.StartsWith("cohere.embed", StringComparison.OrdinalIgnoreCase))
        {
            return Family.Cohere;
        }

        return null;
    }

    /// <summary>Titan single-text request : <c>{ "inputText": "...", "dimensions": 1024 }</c>.</summary>
    public sealed class TitanRequest
    {
        /// <summary>The text to embed.</summary>
        [JsonPropertyName("inputText")]
        public string InputText { get; set; } = string.Empty;

        /// <summary>Output vector dimensions (Titan v2 supports 256 / 512 / 1024).</summary>
        [JsonPropertyName("dimensions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Dimensions { get; set; }

        /// <summary>Whether to normalize the output vector.</summary>
        [JsonPropertyName("normalize")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Normalize { get; set; }
    }

    /// <summary>Titan response : <c>{ embedding: float[], inputTextTokenCount: int }</c>.</summary>
    public sealed class TitanResponse
    {
        /// <summary>The embedding vector.</summary>
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = [];

        /// <summary>Tokens billed for the input text.</summary>
        [JsonPropertyName("inputTextTokenCount")]
        public int InputTextTokenCount { get; set; }
    }

    /// <summary>Cohere batch request : <c>{ "texts": [...], "input_type": "search_document" }</c>.</summary>
    public sealed class CohereRequest
    {
        /// <summary>Texts to embed.</summary>
        [JsonPropertyName("texts")]
        public List<string> Texts { get; set; } = [];

        /// <summary>One of <c>search_document</c>, <c>search_query</c>, <c>classification</c>, <c>clustering</c>.</summary>
        [JsonPropertyName("input_type")]
        public string InputType { get; set; } = "search_document";

        /// <summary>How to truncate inputs that exceed the model's max sequence length.</summary>
        [JsonPropertyName("truncate")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Truncate { get; set; } = "END";
    }

    /// <summary>Cohere response : <c>{ embeddings: float[][], ... }</c>.</summary>
    public sealed class CohereResponse
    {
        /// <summary>One embedding vector per input text.</summary>
        [JsonPropertyName("embeddings")]
        public float[][] Embeddings { get; set; } = [];

        /// <summary>An optional response id from Cohere.</summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
