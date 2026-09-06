using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Configs;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.Services;
using Lingopi.Lingo.Infrastructure.OpenAI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenAI;
using OpenAI.Chat;
using Xunit;

#pragma warning disable OPENAI001

namespace Lingopi.Lingo.Tests.Infrastructure.OpenAI;

public class OpenAITranslationServiceTests
{
    [Fact]
    public async Task TranslateAsync_WhenOpenAIReturnsStructuredTranslation_ShouldReturnTranslationAndUsage()
    {
        var model = OpenAIModels.Gpt56Luna;
        var chatClient = Substitute.For<ChatClient>(
            model,
            new ApiKeyCredential("test-key"));
        var openAiClient = Substitute.For<OpenAIClient>(
            new ApiKeyCredential("test-key"));
        openAiClient.GetChatClient(model).Returns(chatClient);
        IEnumerable<ChatMessage> receivedMessages = null!;
        ChatCompletionOptions receivedOptions = null!;
        chatClient.CompleteChatAsync(
                Arg.Do<IEnumerable<ChatMessage>>(messages => receivedMessages = messages),
                Arg.Do<ChatCompletionOptions>(options => receivedOptions = options),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                ClientResult.FromValue(
                    CreateCompletion(
                        "{\"translation\":\"یخ را شکستن\",\"expression\":\"break the ice\",\"pattern\":\"break the ice\",\"senseKey\":\"break_ice\",\"domains\":[\"communication\"],\"isOffensive\":false,\"meaning\":\"to start a conversation\",\"examples\":[{\"text\":\"They used the game to break the ice.\",\"translation\":\"آنها از بازی برای شروع گفتگو استفاده کردند.\"},{\"text\":\"A joke helped break the ice.\",\"translation\":\"یک شوخی به شکستن یخ کمک کرد.\"},{\"text\":\"We played an activity to break the ice.\",\"translation\":\"ما فعالیتی برای شروع صمیمیت انجام دادیم.\"},{\"text\":\"The meeting had not broken the ice yet.\",\"translation\":\"جلسه هنوز باعث صمیمیت نشده بود.\"},{\"text\":\"Will this question break the ice?\",\"translation\":\"آیا این پرسش یخ گفتگو را می‌شکند؟\"}],\"commonMistakes\":[\"Using it for ending a conversation\"],\"tags\":[\"conversation\",\"social\",\"idiom\"],\"type\":\"idiom\",\"registers\":[\"casual\"]}",
                        "req-123",
                        model,
                        inputTokens: 100,
                        outputTokens: 20,
                        totalTokens: 120),
                    Substitute.For<PipelineResponse>())));
        var modelSettings = new OpenAIModelSettingsProvider();
        modelSettings.Replace(
            [new OpenAIModelSettings(model, 1m, 2m)],
            model);
        var service = CreateService(openAiClient, modelSettings);

        var result = await service.TranslateAsync(
            new TranslationRequest("break the ice", "en-US", "fa-IR", model),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("یخ را شکستن", result.Value!.Translation);
        Assert.Equal("req-123", result.Value.RequestId);
        Assert.Equal(model, result.Value.Model);
        Assert.Equal("translation-v1", result.Value.PromptVersion);
        Assert.Equal(100, result.Value.InputTokens);
        Assert.Equal(20, result.Value.OutputTokens);
        Assert.Equal(0.00014m, result.Value.EstimatedCost);
        Assert.Equal("break the ice", result.Value.Expression);
        Assert.Equal("break_ice", result.Value.SenseKey);
        Assert.Equal([LingoDomain.Communication], result.Value.Domains);
        Assert.False(result.Value.IsOffensive);
        Assert.Equal(["Using it for ending a conversation"], result.Value.CommonMistakes);
        Assert.Equal(LingoType.Idiom, result.Value.Type);
        Assert.Equal([LingoRegister.Casual], result.Value.Registers);

        var messages = receivedMessages!.ToArray();
        Assert.Equal(2, messages.Length);
        Assert.Contains(
            "Source locale: en-US",
            messages[1].Content[0].Text,
            StringComparison.Ordinal);
        Assert.Contains(
            "Target locale: fa-IR",
            messages[1].Content[0].Text,
            StringComparison.Ordinal);
        Assert.Contains(
            "target language",
            messages[0].Content[0].Text,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "casual",
            messages[0].Content[0].Text,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "Every learner-facing generated field",
            messages[0].Content[0].Text,
            StringComparison.Ordinal);
        Assert.Contains(
            "square brackets",
            messages[0].Content[0].Text,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "definition/meaning must remain in the source language",
            messages[0].Content[0].Text,
            StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(receivedOptions!.ResponseFormat);
    }

    [Fact]
    public async Task TranslateAsync_WhenOpenAIReturnsInvalidJson_ShouldReturnFailure()
    {
        var model = OpenAIModels.Gpt5Nano;
        var chatClient = Substitute.For<ChatClient>(
            model,
            new ApiKeyCredential("test-key"));
        var openAiClient = Substitute.For<OpenAIClient>(
            new ApiKeyCredential("test-key"));
        openAiClient.GetChatClient(model).Returns(chatClient);
        chatClient.CompleteChatAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatCompletionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                ClientResult.FromValue(
                    CreateCompletion("not-json", "req-123", model),
                    Substitute.For<PipelineResponse>())));
        var service = CreateService(openAiClient, new OpenAIModelSettingsProvider());

        var result = await service.TranslateAsync(
            new TranslationRequest("hello", "en-US", "fa-IR", model),
            TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.False(result.Succeeded);
        Assert.Contains("invalid translation JSON", result.Error!.Messages[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task TranslateAsync_WhenTranslationIsOffensive_ShouldReturnNoExamples()
    {
        var model = OpenAIModels.Gpt5Nano;
        var chatClient = Substitute.For<ChatClient>(
            model,
            new ApiKeyCredential("test-key"));
        var openAiClient = Substitute.For<OpenAIClient>(
            new ApiKeyCredential("test-key"));
        openAiClient.GetChatClient(model).Returns(chatClient);
        chatClient.CompleteChatAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatCompletionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                ClientResult.FromValue(
                    CreateCompletion(
                        "{\"translation\":\"...\",\"expression\":\"bad expression\",\"pattern\":\"bad expression\",\"senseKey\":\"bad_expression\",\"domains\":[\"communication\"],\"isOffensive\":true,\"meaning\":\"offensive expression\",\"examples\":[{\"text\":\"Bad example one\",\"translation\":\"ترجمه یک\"},{\"text\":\"Bad example two\",\"translation\":\"ترجمه دو\"},{\"text\":\"Bad example three\",\"translation\":\"ترجمه سه\"}],\"commonMistakes\":[\"Using this expression in ordinary conversation\"],\"tags\":[\"slang\",\"offensive\",\"communication\"],\"type\":\"idiom\",\"registers\":[\"slang\"]}",
                        "req-offensive",
                        model,
                        inputTokens: 10,
                        outputTokens: 5,
                        totalTokens: 15),
                    Substitute.For<PipelineResponse>())));
        var service = CreateService(openAiClient, new OpenAIModelSettingsProvider());

        var result = await service.TranslateAsync(
            new TranslationRequest("bad expression", "en-US", "fa-IR", model),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Error?.Messages[0]);
        Assert.True(result.Value!.IsOffensive);
        Assert.Empty(result.Value.Examples!);
    }

    [Fact]
    public async Task CaptureAnalysisAsync_ShouldAlwaysUseGpt5Nano()
    {
        var chatClient = Substitute.For<ChatClient>(
            OpenAIModels.Gpt5Nano,
            new ApiKeyCredential("test-key"));
        var openAiClient = Substitute.For<OpenAIClient>(
            new ApiKeyCredential("test-key"));
        openAiClient.GetChatClient(OpenAIModels.Gpt5Nano).Returns(chatClient);
        IEnumerable<ChatMessage> receivedMessages = null!;
        chatClient.CompleteChatAsync(
                Arg.Do<IEnumerable<ChatMessage>>(messages => receivedMessages = messages),
                Arg.Any<ChatCompletionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                ClientResult.FromValue(
                    CreateCompletion(
                        "{\"canonicalExpression\":\"make a point\",\"meaning\":\"express or emphasize an idea or argument\",\"senseKey\":\"express_main_idea\",\"sourceLanguageCode\":\"en\",\"expressionType\":\"phrase\"}",
                        "req-duplicate",
                        OpenAIModels.Gpt5Nano,
                        inputTokens: 10,
                        outputTokens: 5,
                        totalTokens: 15),
                    Substitute.For<PipelineResponse>())));

        var service = new OpenAICaptureAnalysisService(
            openAiClient,
            new OpenAIModelSettingsProvider(),
            NullLogger<OpenAICaptureAnalysisService>.Instance);

        var result = await service.AnalyzeCaptureAsync(
            "The point I'm trying to make",
            "en-US",
            "fa-IR",
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("make a point", result.Value!.CanonicalExpression);
        Assert.Equal("express or emphasize an idea or argument", result.Value.Meaning);
        Assert.Equal("express_main_idea", result.Value.SenseKey);
        Assert.Equal("req-duplicate", result.Value.TrackingId);
        Assert.Equal(OpenAIModels.Gpt5Nano, result.Value.Model);
        Assert.Equal(10, result.Value.InputTokens);
        Assert.Equal(5, result.Value.OutputTokens);
        Assert.DoesNotContain(
            "Workplace",
            receivedMessages.ToArray()[1].Content[0].Text,
            StringComparison.Ordinal);
        openAiClient.Received(1).GetChatClient(OpenAIModels.Gpt5Nano);
        openAiClient.DidNotReceive().GetChatClient(OpenAIModels.Gpt56Luna);
    }

    private static OpenAITranslationService CreateService(
        OpenAIClient openAiClient,
        IOpenAIModelSettingsProvider modelSettingsProvider)
    {
        return new OpenAITranslationService(
            openAiClient,
            modelSettingsProvider,
            Options.Create(new OpenAIConfig
            {
                ApiKey = "test-key",
                PromptVersion = "translation-v1"
            }),
            NullLogger<OpenAITranslationService>.Instance);
    }

    private static ChatCompletion CreateCompletion(
        string content,
        string requestId,
        string model,
        int inputTokens = 0,
        int outputTokens = 0,
        int totalTokens = 0)
    {
        return OpenAIChatModelFactory.ChatCompletion(
            id: requestId,
            finishReason: ChatFinishReason.Stop,
            content: new ChatMessageContent(ChatMessageContentPart.CreateTextPart(content)),
            model: model,
            usage: OpenAIChatModelFactory.ChatTokenUsage(
                outputTokenCount: outputTokens,
                inputTokenCount: inputTokens,
                totalTokenCount: totalTokens));
    }

}
