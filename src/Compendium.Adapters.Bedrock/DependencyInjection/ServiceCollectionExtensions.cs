// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Bedrock.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.Bedrock.DependencyInjection;

/// <summary>
/// DI registration helpers for the Bedrock adapter.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="BedrockAdapter"/> and its options.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configuration">Source configuration; section <see cref="BedrockOptions.SectionName"/> is bound.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddCompendiumBedrockAdapter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<BedrockOptions>()
            .Bind(configuration.GetSection(BedrockOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<BedrockAdapter>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="BedrockAdapter"/> with an inline configuration callback.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configure">Callback to mutate <see cref="BedrockOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddCompendiumBedrockAdapter(
        this IServiceCollection services,
        Action<BedrockOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<BedrockOptions>()
            .Configure(configure)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<BedrockAdapter>();

        return services;
    }
}
