// -----------------------------------------------------------------------
// <copyright file="BedrockTestFactories.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using Amazon.Runtime.EventStreams;
using BedrockMessage = Amazon.BedrockRuntime.Model.Message;

namespace Compendium.Adapters.Bedrock.Tests.TestSupport;

internal static class BedrockTestFactories
{
    public static BedrockOptions DefaultOptions() => new()
    {
        Region = "us-east-1",
        DefaultModelId = "anthropic.claude-3-haiku-20240307-v1:0",
        EmbeddingModelId = "amazon.titan-embed-text-v2:0",
        DefaultMaxTokens = 1024,
        Timeout = TimeSpan.FromSeconds(30),
        MaxRetries = 2,
    };

    public static BedrockAIProvider CreateProvider(
        IAmazonBedrockRuntime client,
        BedrockOptions? options = null) =>
        new(
            client,
            Microsoft.Extensions.Options.Options.Create(options ?? DefaultOptions()),
            NullLogger<BedrockAIProvider>.Instance);

    public static CompletionRequest SimpleCompletionRequest(string? userText = "Hello") => new()
    {
        Model = null!,
        Messages =
        [
            Message.User(userText ?? string.Empty),
        ],
    };

    public static ConverseResponse SuccessConverseResponse(
        string text = "Hello world",
        int promptTokens = 12,
        int completionTokens = 3,
        StopReason? stopReason = null,
        string requestId = "req-abc") =>
        new()
        {
            Output = new ConverseOutput
            {
                Message = new BedrockMessage
                {
                    Role = ConversationRole.Assistant,
                    Content = [new ContentBlock { Text = text }],
                },
            },
            StopReason = stopReason ?? StopReason.End_turn,
            Usage = new TokenUsage { InputTokens = promptTokens, OutputTokens = completionTokens },
            ResponseMetadata = new ResponseMetadata { RequestId = requestId },
        };

    public static AmazonBedrockRuntimeException BedrockException(
        HttpStatusCode status,
        string code = "TestError",
        string message = "boom") =>
        new(message)
        {
            StatusCode = status,
            ErrorCode = code,
        };

    public static InvokeModelResponse TitanEmbedResponse(float[] vector, int tokenCount = 7)
    {
        var json = JsonSerializer.Serialize(new { embedding = vector, inputTextTokenCount = tokenCount });
        return new InvokeModelResponse
        {
            Body = new MemoryStream(Encoding.UTF8.GetBytes(json)),
            ContentType = "application/json",
        };
    }

    public static InvokeModelResponse CohereEmbedResponse(float[][] embeddings)
    {
        var json = JsonSerializer.Serialize(new { embeddings, id = "coh-1" });
        return new InvokeModelResponse
        {
            Body = new MemoryStream(Encoding.UTF8.GetBytes(json)),
            ContentType = "application/json",
        };
    }

    public static async IAsyncEnumerable<IEventStreamEvent> AsAsyncEvents(IEnumerable<IEventStreamEvent> events)
    {
        foreach (var e in events)
        {
            await Task.Yield();
            yield return e;
        }
    }

    public static async IAsyncEnumerable<IEventStreamEvent> ThrowingEvents(Exception toThrow)
    {
        await Task.Yield();
        throw toThrow;
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}
