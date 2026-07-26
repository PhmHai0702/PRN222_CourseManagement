using CourseManagement.Model.DTOs;
using System.Net.Http.Json;

namespace BlazorAppSecure.Sevices.CourseComparison
{
    public sealed class CourseComparisonClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CourseComparisonClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IReadOnlyList<SearchCourseResponse>> GetPublishedCoursesAsync(
            CancellationToken cancellationToken = default)
        {
            var client = _httpClientFactory.CreateClient("Auth");
            var response = await client.GetAsync(
                "/api/Course/search?Statuss=1", cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            return await response.Content.ReadFromJsonAsync<List<SearchCourseResponse>>(
                cancellationToken) ?? new List<SearchCourseResponse>();
        }

        public async Task<CourseComparisonResponse> CompareAsync(
            IEnumerable<Guid> courseIds,
            CancellationToken cancellationToken = default)
        {
            var ids = courseIds.Distinct().Take(4).ToList();
            if (ids.Count < 2)
                throw new ArgumentException("Hãy chọn ít nhất hai khóa học.");

            var query = string.Join("&", ids.Select(id =>
                $"CourseIds={Uri.EscapeDataString(id.ToString())}"));
            var client = _httpClientFactory.CreateClient("Auth");
            var response = await client.GetAsync(
                $"/api/course-comparison?{query}", cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            return await response.Content.ReadFromJsonAsync<CourseComparisonResponse>(
                cancellationToken)
                ?? throw new InvalidOperationException(
                    "API không trả về dữ liệu so sánh.");
        }

        private static async Task EnsureSuccessAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
                return;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(string.IsNullOrWhiteSpace(body)
                ? $"API trả về lỗi {(int)response.StatusCode}."
                : body);
        }
    }
}
