using CourseManagement.Model.DTOs;
using System.Net.Http.Json;

namespace BlazorAppSecure.Sevices.AdminAnalytics
{
    public sealed class AdminAnalyticsClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminAnalyticsClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<AdminAnalyticsResponse> GetAsync(
            DateTime fromDate,
            DateTime toDate,
            int top = 5,
            CancellationToken cancellationToken = default)
        {
            var client = _httpClientFactory.CreateClient("Auth");
            var url = "/api/admin/analytics" +
                $"?FromDate={fromDate:yyyy-MM-dd}" +
                $"&ToDate={toDate:yyyy-MM-dd}" +
                $"&Top={Math.Clamp(top, 1, 20)}";

            var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(
                    cancellationToken);
                throw new HttpRequestException(string.IsNullOrWhiteSpace(body)
                    ? $"Không thể tải thống kê ({(int)response.StatusCode})."
                    : body);
            }

            return await response.Content
                .ReadFromJsonAsync<AdminAnalyticsResponse>(cancellationToken)
                ?? throw new InvalidOperationException(
                    "API không trả về dữ liệu thống kê.");
        }
    }
}
