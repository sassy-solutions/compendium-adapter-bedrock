// -----------------------------------------------------------------------
// <copyright file="BedrockErrorMappingTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Compendium.Adapters.Bedrock.Tests.TestSupport;

namespace Compendium.Adapters.Bedrock.Tests.Services;

public class BedrockErrorMappingTests
{
    [Fact]
    public void Map_NullException_Throws() =>
        FluentActions.Invoking(() => BedrockErrorMapping.Map(null!, "m"))
            .Should().Throw<ArgumentNullException>();

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "AI.InvalidApiKey")]
    [InlineData(HttpStatusCode.Forbidden, "AI.InvalidApiKey")]
    [InlineData(HttpStatusCode.PaymentRequired, "AI.InsufficientCredits")]
    [InlineData(HttpStatusCode.NotFound, "AI.ModelNotFound")]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, "AI.TokenLimitExceeded")]
    [InlineData(HttpStatusCode.TooManyRequests, "AI.RateLimitExceeded")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "AI.ProviderUnavailable")]
    public void Map_KnownStatuses_MapToTypedErrors(HttpStatusCode status, string expectedCode)
    {
        var error = BedrockErrorMapping.Map(BedrockTestFactories.BedrockException(status), "m");
        error.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Map_UnknownStatus_FallsBackToProviderError()
    {
        var error = BedrockErrorMapping.Map(
            BedrockTestFactories.BedrockException(HttpStatusCode.BadGateway, code: "BadGateway", message: "upstream"),
            "m");

        error.Code.Should().Be("AI.ProviderError");
    }

    [Fact]
    public void Map_NotFound_CarriesModelIdIntoMessage()
    {
        var error = BedrockErrorMapping.Map(
            BedrockTestFactories.BedrockException(HttpStatusCode.NotFound),
            modelId: "anthropic.claude-3-haiku-20240307-v1:0");

        error.Message.Should().Contain("anthropic.claude-3-haiku-20240307-v1:0");
    }
}
