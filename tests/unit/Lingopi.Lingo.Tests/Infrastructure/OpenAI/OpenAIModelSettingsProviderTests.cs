using System;
using Lingopi.Lingo.Application.Models.Configs;
using Lingopi.Lingo.Infrastructure.OpenAI;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lingopi.Lingo.Tests.Infrastructure.OpenAI;

public class OpenAIModelSettingsProviderTests
{
    [Fact]
    public void Get_WhenModelIsNotSpecified_ShouldUseConfiguredDefault()
    {
        var provider = CreateProvider();

        var settings = provider.Get(null);

        Assert.Equal("economy-model", settings.ModelId);
        Assert.Equal(3, provider.GetAll().Count);
    }

    [Fact]
    public void Get_WhenRoleIsConfigured_ShouldReturnItsModelSettings()
    {
        var provider = CreateProvider();

        var settings = provider.Get(OpenAIModelRoles.Premium);

        Assert.Equal("premium-model", settings.ModelId);
    }

    [Fact]
    public void Replace_ShouldAllowRuntimeModelSettingsChanges()
    {
        var provider = CreateProvider();

        provider.Replace(
            [
                new OpenAIModelSettings(OpenAIModelRoles.Economy, "economy-model", 1m, 2m),
                new OpenAIModelSettings("Custom", "custom-model", 3m, 4m)
            ],
            "Custom");

        var settings = provider.Get(null);

        Assert.Equal("custom-model", settings.ModelId);
        Assert.Equal(3m, settings.InputCostPerMillionTokens);
        Assert.Equal(4m, settings.OutputCostPerMillionTokens);
        Assert.Throws<ArgumentException>(() => provider.Get("Unconfigured"));
    }

    [Fact]
    public void Constructor_WhenPricingIsConfigured_ShouldExposeConfiguredPricing()
    {
        var provider = new OpenAIModelSettingsProvider(
            Options.Create(new OpenAIConfig
            {
                DefaultModelRole = OpenAIModelRoles.Economy,
                Models =
                [
                    new OpenAIModelSettings(OpenAIModelRoles.Economy, "economy-model", 1m, 2m)
                ]
            }));

        var settings = provider.Get(null);

        Assert.Equal("economy-model", settings.ModelId);
        Assert.Equal(1m, settings.InputCostPerMillionTokens);
        Assert.Equal(2m, settings.OutputCostPerMillionTokens);
    }

    private static OpenAIModelSettingsProvider CreateProvider()
    {
        return new OpenAIModelSettingsProvider(
            Options.Create(new OpenAIConfig
            {
                DefaultModelRole = OpenAIModelRoles.Economy,
                Models =
                [
                    new OpenAIModelSettings(OpenAIModelRoles.Economy, "economy-model", 0.10m, 0.50m),
                    new OpenAIModelSettings(OpenAIModelRoles.Premium, "premium-model", 2m, 10m),
                    new OpenAIModelSettings(OpenAIModelRoles.Embedding, "embedding-model", 0.02m)
                ]
            }));
    }
}
