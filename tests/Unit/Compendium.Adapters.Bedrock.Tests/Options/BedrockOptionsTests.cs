// -----------------------------------------------------------------------
// <copyright file="BedrockOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Compendium.Adapters.Bedrock.Tests.Options;

public class BedrockOptionsTests
{
    [Fact]
    public void Defaults_AreSensible()
    {
        var opts = new BedrockOptions();

        opts.DefaultModelId.Should().Be(BedrockOptions.DefaultClaude35SonnetV2ModelId);
        opts.EmbeddingModelId.Should().Be(BedrockOptions.DefaultTitanEmbedTextV2ModelId);
        opts.DefaultMaxTokens.Should().Be(4096);
        opts.Timeout.Should().Be(TimeSpan.FromSeconds(120));
        opts.MaxRetries.Should().Be(3);
        opts.Region.Should().BeEmpty();
        opts.AccessKey.Should().BeNull();
        opts.SecretKey.Should().BeNull();
        opts.SessionToken.Should().BeNull();
    }

    [Fact]
    public void IsValid_RequiresRegionAndModelIds()
    {
        var opts = new BedrockOptions();
        opts.IsValid().Should().BeFalse();

        opts.Region = "us-east-1";
        opts.IsValid().Should().BeTrue();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("us-east-1", true)]
    public void IsValid_RegionMustBeNonBlank(string region, bool expected)
    {
        var opts = new BedrockOptions { Region = region };
        opts.IsValid().Should().Be(expected);
    }

    [Fact]
    public void IsValid_RejectsZeroMaxTokensAndNegativeTimeouts()
    {
        var opts = new BedrockOptions { Region = "us-east-1", DefaultMaxTokens = 0 };
        opts.IsValid().Should().BeFalse();

        opts.DefaultMaxTokens = 10;
        opts.Timeout = TimeSpan.Zero;
        opts.IsValid().Should().BeFalse();

        opts.Timeout = TimeSpan.FromSeconds(5);
        opts.MaxRetries = -1;
        opts.IsValid().Should().BeFalse();
    }

    [Fact]
    public void BindsFromConfigurationSection()
    {
        var dict = new Dictionary<string, string?>
        {
            ["Compendium:Adapters:Bedrock:Region"] = "eu-west-1",
            ["Compendium:Adapters:Bedrock:AccessKey"] = "AKIAEXAMPLE",
            ["Compendium:Adapters:Bedrock:SecretKey"] = "secret",
            ["Compendium:Adapters:Bedrock:SessionToken"] = "session",
            ["Compendium:Adapters:Bedrock:DefaultModelId"] = "anthropic.claude-3-haiku-20240307-v1:0",
            ["Compendium:Adapters:Bedrock:EmbeddingModelId"] = "cohere.embed-english-v3",
            ["Compendium:Adapters:Bedrock:DefaultMaxTokens"] = "777",
            ["Compendium:Adapters:Bedrock:MaxRetries"] = "5",
            ["Compendium:Adapters:Bedrock:Timeout"] = "00:00:30",
        };
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

        var opts = new BedrockOptions();
        cfg.GetSection(BedrockOptions.SectionName).Bind(opts);

        opts.Region.Should().Be("eu-west-1");
        opts.AccessKey.Should().Be("AKIAEXAMPLE");
        opts.SecretKey.Should().Be("secret");
        opts.SessionToken.Should().Be("session");
        opts.DefaultModelId.Should().Be("anthropic.claude-3-haiku-20240307-v1:0");
        opts.EmbeddingModelId.Should().Be("cohere.embed-english-v3");
        opts.DefaultMaxTokens.Should().Be(777);
        opts.MaxRetries.Should().Be(5);
        opts.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        opts.IsValid().Should().BeTrue();
    }
}
