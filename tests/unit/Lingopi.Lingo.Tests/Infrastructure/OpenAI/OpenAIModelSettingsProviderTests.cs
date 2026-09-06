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
        var provider = new OpenAIModelSettingsProvider();

        var settings = provider.Get(null);

        Assert.Equal(OpenAIModels.Gpt56Luna, settings.ModelId);
        Assert.Equal(4, provider.GetAll().Count);
    }

    [Fact]
    public void Replace_ShouldAllowRuntimeModelSettingsChanges()
    {
        var provider = new OpenAIModelSettingsProvider();

        provider.Replace(
            [
                new OpenAIModelSettings(OpenAIModels.Gpt56Luna, 1m, 2m),
                new OpenAIModelSettings("custom-model", 3m, 4m)
            ],
            "custom-model");

        var settings = provider.Get(null);

        Assert.Equal("custom-model", settings.ModelId);
        Assert.Equal(3m, settings.InputCostPerMillionTokens);
        Assert.Equal(4m, settings.OutputCostPerMillionTokens);
        Assert.Throws<ArgumentException>(() => provider.Get(OpenAIModels.Gpt5Nano));
    }

    [Fact]
    public void Constructor_WhenPricingIsConfigured_ShouldExposeConfiguredPricing()
    {
        var provider = new OpenAIModelSettingsProvider(
            Options.Create(new OpenAIConfig
            {
                DefaultModel = "custom-model",
                Models =
                [
                    new OpenAIModelSettings("custom-model", 1m, 2m)
                ]
            }));

        var settings = provider.Get(null);

        Assert.Equal("custom-model", settings.ModelId);
        Assert.Equal(1m, settings.InputCostPerMillionTokens);
        Assert.Equal(2m, settings.OutputCostPerMillionTokens);
    }
}
