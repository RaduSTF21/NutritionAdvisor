using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NutritionAdvisor.Application.AI;
using NutritionAdvisor.Infrastructure.Options;
using NutritionAdvisor.Infrastructure.Services;
using Xunit;

namespace NutritionAdvisor.Tests.Infrastructure;

public class PythonAiServiceTests
{
    private class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }

    [Fact]
    public async Task GetRecommendationsAsync_ReturnsModel_OnSuccess()
    {
        var model = new AiRecommendationResponseModel(Guid.NewGuid(), "mode", new List<AiRecommendationItemModel>
        {
            new AiRecommendationItemModel("id","one", null, new List<string>{"i"}, "reason", false, 5)
        });
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(model, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }), System.Text.Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var options = Options.Create(new PythonAiOptions { BaseUrl = "http://localhost", TimeoutSeconds = 30 });
        var svc = new PythonAiService(client, options);

        var request = new AiRecommendationRequestModel("user", null, new List<string>(), new List<string>(), 10, new List<AiRecipeDto>());
        var result = await svc.GetRecommendationsAsync(request);

        Assert.NotNull(result);
        Assert.Contains(result.Recommendations, r => r.Title == "one");
    }

    [Fact]
    public async Task GetRecommendationsAsync_Throws_OnServerError()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var options = Options.Create(new PythonAiOptions { BaseUrl = "http://localhost", TimeoutSeconds = 30 });
        var svc = new PythonAiService(client, options);
        var badRequest = new AiRecommendationRequestModel("user", null, new List<string>(), new List<string>(), 1, new List<AiRecipeDto>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.GetRecommendationsAsync(badRequest));
    }

    [Fact]
    public async Task PrewarmAsync_IgnoresExceptions()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("network"));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var options = Options.Create(new PythonAiOptions { BaseUrl = "http://localhost", TimeoutSeconds = 1 });
        var svc = new PythonAiService(client, options);

        var prewarmReq = new AiMealPlanRequestModel("user", null, 1, new List<string>(), new List<string>(), new List<AiRecipeDto>());
        // should not throw
        await svc.PrewarmAsync(prewarmReq);
    }
}
