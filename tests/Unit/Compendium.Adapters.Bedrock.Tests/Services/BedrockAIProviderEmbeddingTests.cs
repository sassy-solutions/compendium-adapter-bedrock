// -----------------------------------------------------------------------
// <copyright file="BedrockAIProviderEmbeddingTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Compendium.Adapters.Bedrock.Tests.TestSupport;

namespace Compendium.Adapters.Bedrock.Tests.Services;

public class BedrockAIProviderEmbeddingTests
{
    [Fact]
    public async Task EmbedAsync_NullRequest_Throws()
    {
        var sut = BedrockTestFactories.CreateProvider(Substitute.For<IAmazonBedrockRuntime>());
        await FluentActions.Invoking(() => sut.EmbedAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EmbedAsync_EmptyInputs_Fails()
    {
        var sut = BedrockTestFactories.CreateProvider(Substitute.For<IAmazonBedrockRuntime>());
        var result = await sut.EmbedAsync(new EmbeddingRequest { Model = null!, Inputs = Array.Empty<string>() });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.InvalidRequest");
    }

    [Fact]
    public async Task EmbedAsync_UnknownModelFamily_Fails()
    {
        var sut = BedrockTestFactories.CreateProvider(Substitute.For<IAmazonBedrockRuntime>());
        var result = await sut.EmbedAsync(new EmbeddingRequest
        {
            Model = "anthropic.claude-3-5-sonnet-20241022-v2:0", // not an embedding family
            Inputs = ["hello"],
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.InvalidRequest");
        result.Error.Message.Should().Contain("Unsupported embedding model id");
    }

    [Fact]
    public async Task EmbedAsync_Titan_SinglePerCallBatchAggregatesTokens()
    {
        // Arrange
        var client = Substitute.For<IAmazonBedrockRuntime>();
        var captured = new List<InvokeModelRequest>();
        client.InvokeModelAsync(Arg.Do<InvokeModelRequest>(r =>
            {
                // Drain the body so we can capture the bytes; AWS SDK uses MemoryStream so this is cheap.
                using var ms = new MemoryStream();
                r.Body!.CopyTo(ms);
                captured.Add(new InvokeModelRequest
                {
                    ModelId = r.ModelId,
                    ContentType = r.ContentType,
                    Accept = r.Accept,
                    Body = new MemoryStream(ms.ToArray()),
                });
            }), Arg.Any<CancellationToken>())
            .Returns(
                _ => BedrockTestFactories.TitanEmbedResponse([0.1f, 0.2f, 0.3f], tokenCount: 5),
                _ => BedrockTestFactories.TitanEmbedResponse([0.4f, 0.5f, 0.6f], tokenCount: 11));

        var sut = BedrockTestFactories.CreateProvider(client);

        // Act
        var result = await sut.EmbedAsync(new EmbeddingRequest
        {
            Model = "amazon.titan-embed-text-v2:0",
            Inputs = ["one", "two"],
            Dimensions = 1024,
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Model.Should().Be("amazon.titan-embed-text-v2:0");
        result.Value.Embeddings.Should().HaveCount(2);
        result.Value.Embeddings[0].Index.Should().Be(0);
        result.Value.Embeddings[0].Vector.Should().Equal(0.1f, 0.2f, 0.3f);
        result.Value.Embeddings[1].Index.Should().Be(1);
        result.Value.Embeddings[1].Vector.Should().Equal(0.4f, 0.5f, 0.6f);
        result.Value.Usage.PromptTokens.Should().Be(16);

        // Verify each Titan call carries one input + the dimensions hint.
        captured.Should().HaveCount(2);
        foreach (var req in captured)
        {
            req.ModelId.Should().Be("amazon.titan-embed-text-v2:0");
            req.ContentType.Should().Be("application/json");
            using var doc = JsonDocument.Parse(req.Body!);
            doc.RootElement.GetProperty("inputText").GetString().Should().BeOneOf("one", "two");
            doc.RootElement.GetProperty("dimensions").GetInt32().Should().Be(1024);
        }
    }

    [Fact]
    public async Task EmbedAsync_FallsBackToConfiguredEmbeddingModelWhenRequestModelMissing()
    {
        // Arrange
        var client = Substitute.For<IAmazonBedrockRuntime>();
        InvokeModelRequest? captured = null;
        client.InvokeModelAsync(Arg.Do<InvokeModelRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(BedrockTestFactories.TitanEmbedResponse([0.1f]));
        var sut = BedrockTestFactories.CreateProvider(client);

        // Act
        var result = await sut.EmbedAsync(new EmbeddingRequest { Model = null!, Inputs = ["x"] });

        // Assert
        result.IsSuccess.Should().BeTrue();
        captured!.ModelId.Should().Be(BedrockOptions.DefaultTitanEmbedTextV2ModelId);
    }

    [Fact]
    public async Task EmbedAsync_Cohere_BatchesAllInputsInOneCall()
    {
        // Arrange
        var client = Substitute.For<IAmazonBedrockRuntime>();
        InvokeModelRequest? captured = null;
        client.InvokeModelAsync(Arg.Do<InvokeModelRequest>(r =>
            {
                using var ms = new MemoryStream();
                r.Body!.CopyTo(ms);
                captured = new InvokeModelRequest
                {
                    ModelId = r.ModelId,
                    ContentType = r.ContentType,
                    Accept = r.Accept,
                    Body = new MemoryStream(ms.ToArray()),
                };
            }), Arg.Any<CancellationToken>())
            .Returns(BedrockTestFactories.CohereEmbedResponse(
            [
                [0.11f, 0.22f],
                [0.33f, 0.44f],
            ]));

        var sut = BedrockTestFactories.CreateProvider(client);

        // Act
        var result = await sut.EmbedAsync(new EmbeddingRequest
        {
            Model = "cohere.embed-english-v3",
            Inputs = ["alpha", "beta"],
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Embeddings.Should().HaveCount(2);
        result.Value.Embeddings[0].Vector.Should().Equal(0.11f, 0.22f);
        result.Value.Embeddings[1].Vector.Should().Equal(0.33f, 0.44f);

        captured!.ModelId.Should().Be("cohere.embed-english-v3");
        using var doc = JsonDocument.Parse(captured.Body!);
        var texts = doc.RootElement.GetProperty("texts").EnumerateArray().Select(e => e.GetString()).ToList();
        texts.Should().BeEquivalentTo(new[] { "alpha", "beta" });
        doc.RootElement.GetProperty("input_type").GetString().Should().Be("search_document");

        // One Cohere call for the whole batch, vs N for Titan.
        await client.Received(1).InvokeModelAsync(Arg.Any<InvokeModelRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmbedAsync_OnBedrockException_MapsError()
    {
        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.InvokeModelAsync(Arg.Any<InvokeModelRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(BedrockTestFactories.BedrockException(HttpStatusCode.TooManyRequests));
        var sut = BedrockTestFactories.CreateProvider(client);

        var result = await sut.EmbedAsync(new EmbeddingRequest { Model = null!, Inputs = ["x"] });
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.RateLimitExceeded");
    }

    [Fact]
    public async Task EmbedAsync_PropagatesCancellation()
    {
        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.InvokeModelAsync(Arg.Any<InvokeModelRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        var sut = BedrockTestFactories.CreateProvider(client);

        await FluentActions.Invoking(() => sut.EmbedAsync(new EmbeddingRequest { Model = null!, Inputs = ["x"] }))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task EmbedAsync_Titan_HandlesNullInputsGracefully()
    {
        // Arrange
        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.InvokeModelAsync(Arg.Any<InvokeModelRequest>(), Arg.Any<CancellationToken>())
            .Returns(BedrockTestFactories.TitanEmbedResponse([0.5f]));
        var sut = BedrockTestFactories.CreateProvider(client);

        // Act
        var result = await sut.EmbedAsync(new EmbeddingRequest { Model = null!, Inputs = new string[] { null! } });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Embeddings.Should().ContainSingle();
    }

    [Fact]
    public void BedrockEmbeddingPayloads_Detect_MatchesTitanAndCohereCaseInsensitive()
    {
        BedrockEmbeddingPayloads.Detect("Amazon.Titan-Embed-Text-V2:0")
            .Should().Be(BedrockEmbeddingPayloads.Family.Titan);
        BedrockEmbeddingPayloads.Detect("COHERE.embed-english-v3")
            .Should().Be(BedrockEmbeddingPayloads.Family.Cohere);
        BedrockEmbeddingPayloads.Detect("meta.llama3-1-70b-instruct-v1:0")
            .Should().BeNull();
        BedrockEmbeddingPayloads.Detect(null!).Should().BeNull();
        BedrockEmbeddingPayloads.Detect(string.Empty).Should().BeNull();
    }
}
