// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Bedrock.DependencyInjection;
using Compendium.Adapters.Bedrock.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.Bedrock.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCompendiumBedrockAdapter_WithConfiguration_RegistersAdapterAndOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Compendium:Adapters:Bedrock:BaseUrl"] = "https://api.example.com",
                ["Compendium:Adapters:Bedrock:ApiKey"] = "k1",
            })
            .Build();

        // Act
        var actual = services.AddCompendiumBedrockAdapter(configuration);
        var sp = actual.BuildServiceProvider();

        // Assert
        actual.Should().BeSameAs(services);
        sp.GetRequiredService<BedrockAdapter>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumBedrockAdapter_WithCallback_RegistersAdapterAndOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCompendiumBedrockAdapter(o =>
        {
            o.BaseUrl = "https://api.example.com";
            o.ApiKey = "k1";
        });
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<BedrockAdapter>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumBedrockAdapter_NullServices_Throws()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddCompendiumBedrockAdapter(_ => { });

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
