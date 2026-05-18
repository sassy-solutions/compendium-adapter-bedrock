// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI;
using Compendium.Abstractions.AI.Models;
using Compendium.Adapters.Bedrock.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Reads AWS credentials from the standard credential chain (env vars, shared profile,
// EC2 instance profile, ...). To use explicit access keys for dev/CI, set
// AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY / AWS_SESSION_TOKEN before launching, or
// configure BedrockOptions.AccessKey / SecretKey here. Never hard-code credentials.
var region = Environment.GetEnvironmentVariable("AWS_REGION")
             ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION")
             ?? "us-east-1";

var services = new ServiceCollection();
services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
services.AddCompendiumBedrock(o =>
{
    o.Region = region;
    o.DefaultModelId = "anthropic.claude-3-haiku-20240307-v1:0"; // cheapest Claude on Bedrock
    o.DefaultMaxTokens = 256;
});

await using var provider = services.BuildServiceProvider();
var ai = provider.GetRequiredService<IAIProvider>();

var request = new CompletionRequest
{
    Model = "anthropic.claude-3-haiku-20240307-v1:0",
    SystemPrompt = "You are a terse assistant. Answer in one sentence.",
    Messages = new List<Message>
    {
        Message.User("Give me a one-sentence summary of AWS Bedrock."),
    },
};

Console.WriteLine($"Region: {region}");
Console.WriteLine();
Console.WriteLine("--- Non-streaming completion ---");
var result = await ai.CompleteAsync(request);
if (result.IsSuccess)
{
    Console.WriteLine(result.Value.Content);
    Console.WriteLine($"({result.Value.Usage.PromptTokens} in / {result.Value.Usage.CompletionTokens} out)");
}
else
{
    Console.Error.WriteLine($"Failed: {result.Error.Code} — {result.Error.Message}");
    return 1;
}

Console.WriteLine();
Console.WriteLine("--- Streaming completion ---");
await foreach (var chunk in ai.StreamCompleteAsync(request))
{
    if (chunk.IsFailure)
    {
        Console.Error.WriteLine($"Stream failed: {chunk.Error.Code} — {chunk.Error.Message}");
        return 1;
    }

    Console.Write(chunk.Value.ContentDelta);
    if (chunk.Value.IsFinal)
    {
        Console.WriteLine();
        Console.WriteLine($"(stopped: {chunk.Value.FinishReason})");
    }
}

return 0;
