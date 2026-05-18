// -----------------------------------------------------------------------
// <copyright file="BedrockModelCatalogTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Bedrock.Tests.Services;

public class BedrockModelCatalogTests
{
    [Fact]
    public void KnownModels_IncludeClaudeLlamaMistralNovaTitanCohere()
    {
        var ids = BedrockModelCatalog.KnownModels.Select(m => m.Id).ToList();
        ids.Should().Contain("anthropic.claude-3-5-sonnet-20241022-v2:0");
        ids.Should().Contain("anthropic.claude-3-haiku-20240307-v1:0");
        ids.Should().Contain("meta.llama3-1-70b-instruct-v1:0");
        ids.Should().Contain("mistral.mistral-large-2407-v1:0");
        ids.Should().Contain("amazon.nova-pro-v1:0");
        ids.Should().Contain("amazon.titan-embed-text-v2:0");
        ids.Should().Contain("cohere.embed-english-v3");
    }

    [Fact]
    public void EveryModel_HasProviderBedrock()
    {
        BedrockModelCatalog.KnownModels.Should().OnlyContain(m => m.Provider == "bedrock");
    }

    [Fact]
    public void EmbeddingModels_AdvertiseEmbeddingSupport()
    {
        var titan = BedrockModelCatalog.KnownModels.First(m => m.Id == "amazon.titan-embed-text-v2:0");
        titan.SupportsEmbeddings.Should().BeTrue();
        titan.SupportsStreaming.Should().BeFalse();

        var cohere = BedrockModelCatalog.KnownModels.First(m => m.Id == "cohere.embed-english-v3");
        cohere.SupportsEmbeddings.Should().BeTrue();
    }

    [Fact]
    public void ChatModels_AdvertiseStreamingAndTools()
    {
        var sonnet = BedrockModelCatalog.KnownModels.First(m => m.Id == "anthropic.claude-3-5-sonnet-20241022-v2:0");
        sonnet.SupportsStreaming.Should().BeTrue();
        sonnet.SupportsEmbeddings.Should().BeFalse();
    }
}
