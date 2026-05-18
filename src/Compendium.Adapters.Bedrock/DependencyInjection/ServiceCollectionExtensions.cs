// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Amazon;
using Amazon.BedrockRuntime;
using Amazon.Runtime;
using Compendium.Adapters.Bedrock.Options;
using Compendium.Adapters.Bedrock.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.Bedrock.DependencyInjection;

/// <summary>
/// DI registration helpers for the Bedrock <see cref="IAIProvider"/> adapter.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the Bedrock <see cref="IAIProvider"/>, binding configuration from the
    /// <see cref="BedrockOptions.SectionName"/> section.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configuration">Source configuration.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddCompendiumBedrock(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<BedrockOptions>()
            .Bind(configuration.GetSection(BedrockOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return RegisterCore(services);
    }

    /// <summary>
    /// Register the Bedrock <see cref="IAIProvider"/> with an inline options callback.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configure">Callback to populate <see cref="BedrockOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddCompendiumBedrock(
        this IServiceCollection services,
        Action<BedrockOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<BedrockOptions>()
            .Configure(configure)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return RegisterCore(services);
    }

    private static IServiceCollection RegisterCore(IServiceCollection services)
    {
        services.AddSingleton<IAmazonBedrockRuntime>(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<BedrockOptions>>().Value;
            return CreateClient(opt);
        });

        services.AddSingleton<BedrockAIProvider>();
        services.AddSingleton<IAIProvider>(sp => sp.GetRequiredService<BedrockAIProvider>());

        return services;
    }

    /// <summary>
    /// Builds the configured <see cref="IAmazonBedrockRuntime"/> for the supplied
    /// options. Exposed for testing.
    /// </summary>
    /// <param name="opt">Adapter options.</param>
    /// <returns>Configured Bedrock runtime client.</returns>
    internal static IAmazonBedrockRuntime CreateClient(BedrockOptions opt)
    {
        ArgumentNullException.ThrowIfNull(opt);

        var config = new AmazonBedrockRuntimeConfig
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(opt.Region),
            Timeout = opt.Timeout,
            MaxErrorRetry = opt.MaxRetries,
        };

        if (!string.IsNullOrWhiteSpace(opt.AccessKey) && !string.IsNullOrWhiteSpace(opt.SecretKey))
        {
            AWSCredentials credentials = string.IsNullOrWhiteSpace(opt.SessionToken)
                ? new BasicAWSCredentials(opt.AccessKey, opt.SecretKey)
                : new SessionAWSCredentials(opt.AccessKey, opt.SecretKey, opt.SessionToken);
            return new AmazonBedrockRuntimeClient(credentials, config);
        }

        return new AmazonBedrockRuntimeClient(config);
    }
}
