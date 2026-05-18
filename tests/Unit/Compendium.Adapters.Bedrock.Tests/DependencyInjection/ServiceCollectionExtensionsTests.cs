// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Amazon.BedrockRuntime;
using Compendium.Adapters.Bedrock.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.Bedrock.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCompendiumBedrock_WithConfiguration_NullServices_Throws() =>
        FluentActions.Invoking(() => ServiceCollectionExtensions.AddCompendiumBedrock(
                services: null!,
                configuration: new ConfigurationBuilder().Build()))
            .Should().Throw<ArgumentNullException>();

    [Fact]
    public void AddCompendiumBedrock_WithConfiguration_NullConfiguration_Throws() =>
        FluentActions.Invoking(() => new ServiceCollection()
                .AddCompendiumBedrock(configuration: null!))
            .Should().Throw<ArgumentNullException>();

    [Fact]
    public void AddCompendiumBedrock_WithCallback_NullServices_Throws() =>
        FluentActions.Invoking(() => ServiceCollectionExtensions.AddCompendiumBedrock(
                services: null!,
                configure: _ => { }))
            .Should().Throw<ArgumentNullException>();

    [Fact]
    public void AddCompendiumBedrock_WithCallback_NullCallback_Throws() =>
        FluentActions.Invoking(() => new ServiceCollection()
                .AddCompendiumBedrock(configure: null!))
            .Should().Throw<ArgumentNullException>();

    [Fact]
    public void AddCompendiumBedrock_WithCallback_RegistersProviderAndClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCompendiumBedrock(opt =>
        {
            opt.Region = "us-east-1";
            opt.DefaultModelId = "anthropic.claude-3-haiku-20240307-v1:0";
            opt.EmbeddingModelId = "amazon.titan-embed-text-v2:0";
        });

        // Assert
        var sp = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IAIProvider>();
        provider.Should().BeOfType<BedrockAIProvider>();
        provider.ProviderId.Should().Be("bedrock");

        var client = sp.GetRequiredService<IAmazonBedrockRuntime>();
        client.Should().NotBeNull();
        sp.GetRequiredService<BedrockAIProvider>().Should().BeSameAs(provider);
    }

    [Fact]
    public void AddCompendiumBedrock_WithConfiguration_BindsAndResolves()
    {
        // Arrange
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Compendium:Adapters:Bedrock:Region"] = "eu-west-3",
            ["Compendium:Adapters:Bedrock:DefaultModelId"] = "anthropic.claude-3-haiku-20240307-v1:0",
            ["Compendium:Adapters:Bedrock:EmbeddingModelId"] = "amazon.titan-embed-text-v2:0",
        }).Build();

        var services = new ServiceCollection().AddLogging();

        // Act
        services.AddCompendiumBedrock(cfg);

        // Assert
        var sp = services.BuildServiceProvider();
        var opt = sp.GetRequiredService<IOptions<BedrockOptions>>().Value;
        opt.Region.Should().Be("eu-west-3");
        sp.GetRequiredService<IAIProvider>().ProviderId.Should().Be("bedrock");
    }

    [Fact]
    public void AddCompendiumBedrock_WithMissingRequiredFields_ValidationFiresOnResolve()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            // Missing Region.
        }).Build();

        var services = new ServiceCollection().AddLogging();
        services.AddCompendiumBedrock(cfg);

        var sp = services.BuildServiceProvider();
        FluentActions.Invoking(() => sp.GetRequiredService<IOptions<BedrockOptions>>().Value)
            .Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void CreateClient_WithRegionOnly_UsesDefaultCredentialChain()
    {
        // Act
        var client = ServiceCollectionExtensions.CreateClient(new BedrockOptions
        {
            Region = "us-east-1",
            DefaultModelId = "x",
            EmbeddingModelId = "y",
        });

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void CreateClient_WithAccessKeyAndSecret_BuildsBasicCredentialsClient()
    {
        var client = ServiceCollectionExtensions.CreateClient(new BedrockOptions
        {
            Region = "us-east-1",
            AccessKey = "AKIATEST",
            SecretKey = "secret",
            DefaultModelId = "x",
            EmbeddingModelId = "y",
        });

        client.Should().NotBeNull();
    }

    [Fact]
    public void CreateClient_WithSessionToken_BuildsSessionCredentialsClient()
    {
        var client = ServiceCollectionExtensions.CreateClient(new BedrockOptions
        {
            Region = "us-east-1",
            AccessKey = "AKIATEST",
            SecretKey = "secret",
            SessionToken = "session",
            DefaultModelId = "x",
            EmbeddingModelId = "y",
        });

        client.Should().NotBeNull();
    }

    [Fact]
    public void CreateClient_NullOptions_Throws() =>
        FluentActions.Invoking(() => ServiceCollectionExtensions.CreateClient(null!))
            .Should().Throw<ArgumentNullException>();
}
