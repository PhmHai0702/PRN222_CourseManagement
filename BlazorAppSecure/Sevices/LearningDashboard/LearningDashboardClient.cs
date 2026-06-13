using CourseManagement.Model.DTOs;
using System.Net.Http.Json;

namespace BlazorAppSecure.Sevices.LearningDashboard
{
    public sealed class LearningDashboardClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LearningDashboardClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<LearningDashboardResponse> GetAsync(
            bool includeCompleted = true,
            int take = 10,
            CancellationToken cancellationToken = default)
        {
            var safeTake = Math.Clamp(take, 1, 20);
            var client = _httpClientFactory.CreateClient("Auth");
            var url = "/api/learning-dashboard" +
                $"?IncludeCompleted={includeCompleted.ToString().ToLowerInvariant()}" +
                $"&Take={safeTake}";

            var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(
                    cancellationToken);
                throw new HttpRequestException(string.IsNullOrWhiteSpace(body)
                    ? $"Không thể tải dashboard ({(int)response.StatusCode})."
                    : body);
            }

            return await response.Content
                .ReadFromJsonAsync<LearningDashboardResponse>(cancellationToken)
                ?? throw new InvalidOperationException(
                    "API không trả về dữ liệu tiến độ.");
        }
    }
}
