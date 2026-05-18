// -----------------------------------------------------------------------
// <copyright file="BedrockAIProviderStreamingTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime.EventStreams;
using Compendium.Adapters.Bedrock.Tests.TestSupport;

namespace Compendium.Adapters.Bedrock.Tests.Services;

public class BedrockAIProviderStreamingTests
{
    // ---------- TranslateEventsAsync (the heart of the streaming surface) ----------

    [Fact]
    public async Task Translate_TextDeltas_EmitChunksWithMonotonicIndex()
    {
        // Arrange
        var events = new IEventStreamEvent[]
        {
            DeltaEvent("Hello"),
            DeltaEvent(" world"),
            new MessageStopEvent { StopReason = StopReason.End_turn },
            new ConverseStreamMetadataEvent
            {
                Usage = new TokenUsage { InputTokens = 12, OutputTokens = 4 },
            },
        };

        // Act
        var chunks = await Collect(BedrockAIProvider.TranslateEventsAsync(
            requestId: "rid",
            modelId: "model",
            events: BedrockTestFactories.AsAsyncEvents(events),
            cancellationToken: CancellationToken.None));

        // Assert : two text deltas + one final synthetic chunk.
        chunks.Should().HaveCount(3);
        chunks[0].Value.ContentDelta.Should().Be("Hello");
        chunks[0].Value.Index.Should().Be(0);
        chunks[0].Value.IsFinal.Should().BeFalse();
        chunks[1].Value.ContentDelta.Should().Be(" world");
        chunks[1].Value.Index.Should().Be(1);
        chunks[2].Value.IsFinal.Should().BeTrue();
        chunks[2].Value.FinishReason.Should().Be(FinishReason.Stop);
        chunks[2].Value.Usage!.PromptTokens.Should().Be(12);
        chunks[2].Value.Usage!.CompletionTokens.Should().Be(4);
    }

    [Fact]
    public async Task Translate_EmptyDelta_IsSkipped()
    {
        // Arrange
        var events = new IEventStreamEvent[]
        {
            DeltaEvent(string.Empty),
            DeltaEvent("ok"),
            new MessageStopEvent { StopReason = StopReason.End_turn },
        };

        // Act
        var chunks = await Collect(BedrockAIProvider.TranslateEventsAsync(
            "rid", "model", BedrockTestFactories.AsAsyncEvents(events), CancellationToken.None));

        // Assert
        chunks.Should().HaveCount(2);
        chunks[0].Value.ContentDelta.Should().Be("ok");
        chunks[1].Value.IsFinal.Should().BeTrue();
    }

    [Fact]
    public async Task Translate_MaxTokens_ReportsLengthFinishReason()
    {
        // Arrange
        var events = new IEventStreamEvent[]
        {
            DeltaEvent("partial"),
            new MessageStopEvent { StopReason = StopReason.Max_tokens },
        };

        // Act
        var chunks = await Collect(BedrockAIProvider.TranslateEventsAsync(
            "rid", "m", BedrockTestFactories.AsAsyncEvents(events), CancellationToken.None));

        // Assert
        chunks[^1].Value.FinishReason.Should().Be(FinishReason.Length);
    }

    [Fact]
    public async Task Translate_NoStopReasonEvent_StillEmitsFinalChunkAsStop()
    {
        // Arrange : stream ends without a MessageStopEvent.
        var events = new IEventStreamEvent[]
        {
            DeltaEvent("just text"),
        };

        // Act
        var chunks = await Collect(BedrockAIProvider.TranslateEventsAsync(
            "rid", "m", BedrockTestFactories.AsAsyncEvents(events), CancellationToken.None));

        // Assert
        chunks.Should().HaveCount(2);
        chunks[^1].Value.IsFinal.Should().BeTrue();
        chunks[^1].Value.FinishReason.Should().Be(FinishReason.Stop);
    }

    [Fact]
    public async Task Translate_BedrockExceptionMidStream_YieldsMappedFailureAndStops()
    {
        // Arrange
        var ex = BedrockTestFactories.BedrockException(HttpStatusCode.TooManyRequests);

        // Act
        var chunks = await Collect(BedrockAIProvider.TranslateEventsAsync(
            "rid", "m", BedrockTestFactories.ThrowingEvents(ex), CancellationToken.None));

        // Assert
        chunks.Should().ContainSingle();
        chunks[0].IsFailure.Should().BeTrue();
        chunks[0].Error.Code.Should().Be("AI.RateLimitExceeded");
    }

    [Fact]
    public async Task Translate_GenericExceptionMidStream_YieldsStreamInterruptedAndStops()
    {
        // Arrange
        var ex = new InvalidOperationException("event-stream parse error");

        // Act
        var chunks = await Collect(BedrockAIProvider.TranslateEventsAsync(
            "rid", "m", BedrockTestFactories.ThrowingEvents(ex), CancellationToken.None));

        // Assert
        chunks.Should().ContainSingle();
        chunks[0].IsFailure.Should().BeTrue();
        chunks[0].Error.Code.Should().Be("AI.StreamInterrupted");
    }

    [Fact]
    public async Task Translate_OperationCanceledPropagates()
    {
        // Arrange
        var ex = new OperationCanceledException();

        // Act
        var act = async () => await Collect(BedrockAIProvider.TranslateEventsAsync(
            "rid", "m", BedrockTestFactories.ThrowingEvents(ex), CancellationToken.None));

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ---------- StreamCompleteAsync handshake-failure path ----------

    [Fact]
    public async Task StreamCompleteAsync_NullRequest_Throws()
    {
        var sut = BedrockTestFactories.CreateProvider(Substitute.For<IAmazonBedrockRuntime>());
        var act = async () =>
        {
            await foreach (var _ in sut.StreamCompleteAsync(null!, CancellationToken.None))
            {
            }
        };
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StreamCompleteAsync_HandshakeBedrockException_YieldsMappedFailure()
    {
        // Arrange
        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.ConverseStreamAsync(Arg.Any<ConverseStreamRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(BedrockTestFactories.BedrockException(HttpStatusCode.NotFound));
        var sut = BedrockTestFactories.CreateProvider(client);

        // Act
        var results = new List<Result<CompletionChunk>>();
        await foreach (var r in sut.StreamCompleteAsync(BedrockTestFactories.SimpleCompletionRequest()))
        {
            results.Add(r);
        }

        // Assert
        results.Should().ContainSingle();
        results[0].IsFailure.Should().BeTrue();
        results[0].Error.Code.Should().Be("AI.ModelNotFound");
    }

    [Fact]
    public async Task StreamCompleteAsync_HandshakeCancellation_Propagates()
    {
        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.ConverseStreamAsync(Arg.Any<ConverseStreamRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        var sut = BedrockTestFactories.CreateProvider(client);

        var act = async () =>
        {
            await foreach (var _ in sut.StreamCompleteAsync(BedrockTestFactories.SimpleCompletionRequest()))
            {
            }
        };
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StreamCompleteAsync_HandshakeSucceedsButStreamIsNull_YieldsNothing()
    {
        // Arrange
        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.ConverseStreamAsync(Arg.Any<ConverseStreamRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ConverseStreamResponse()); // Stream property null
        var sut = BedrockTestFactories.CreateProvider(client);

        var results = new List<Result<CompletionChunk>>();
        await foreach (var r in sut.StreamCompleteAsync(BedrockTestFactories.SimpleCompletionRequest()))
        {
            results.Add(r);
        }

        results.Should().BeEmpty();
    }

    [Fact]
    public void BuildConverseStreamRequest_PreservesModelAndInferenceConfig()
    {
        // Arrange
        var sut = BedrockTestFactories.CreateProvider(Substitute.For<IAmazonBedrockRuntime>());

        // Act
        var req = sut.BuildConverseStreamRequest(new CompletionRequest
        {
            Model = null!,
            Messages = [Message.User("hi")],
            MaxTokens = 50,
        }, modelId: "amazon.nova-pro-v1:0");

        // Assert
        req.ModelId.Should().Be("amazon.nova-pro-v1:0");
        req.InferenceConfig.MaxTokens.Should().Be(50);
        req.Messages.Should().ContainSingle();
    }

    // ---------- helpers ----------

    private static ContentBlockDeltaEvent DeltaEvent(string text) => new()
    {
        Delta = new ContentBlockDelta { Text = text },
        ContentBlockIndex = 0,
    };

    private static async Task<List<Result<CompletionChunk>>> Collect(IAsyncEnumerable<Result<CompletionChunk>> stream)
    {
        var list = new List<Result<CompletionChunk>>();
        await foreach (var item in stream)
        {
            list.Add(item);
        }

        return list;
    }
}
