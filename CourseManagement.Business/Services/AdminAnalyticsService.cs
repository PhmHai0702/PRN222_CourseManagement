using CourseManagement.Business.Services.IService;
using CourseManagement.DataAccess.Data;
using CourseManagement.Model.Constant;
using CourseManagement.Model.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Business.Services
{
    /// <summary>
    /// Dịch vụ thống kê chỉ đọc dành cho trang quản trị.
    /// Các phép tổng hợp được thực hiện tại database khi có thể.
    /// </summary>
    public sealed class AdminAnalyticsService : IAdminAnalyticsService
    {
        private readonly CourseManagementDb _db;

        public AdminAnalyticsService(CourseManagementDb db)
        {
            _db = db;
        }

        public async Task<AdminAnalyticsResponse> GetDashboardAsync(
            AdminAnalyticsRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var today = DateTime.UtcNow.Date;
            var fromDate = (request.FromDate ?? today.AddDays(-29)).Date;
            var toDate = (request.ToDate ?? today).Date;

            if (fromDate > toDate)
                throw new ArgumentException("Khoảng thời gian không hợp lệ.");

            if ((toDate - fromDate).TotalDays > 366)
                throw new ArgumentException("Khoảng thống kê tối đa là 366 ngày.");

            var endExclusive = toDate.AddDays(1);
            var top = Math.Clamp(request.Top, 1, 20);

            var overview = await BuildOverviewAsync(
                fromDate,
                endExclusive,
                cancellationToken);

            var revenue = await BuildRevenueAsync(
                fromDate,
                endExclusive,
                cancellationToken);

            var dailyTrend = await BuildDailyTrendAsync(
                fromDate,
                toDate,
                endExclusive,
                cancellationToken);

            var topCourses = await BuildTopCoursesAsync(
                fromDate,
                endExclusive,
                top,
                overview.NewEnrollments,
                cancellationToken);

            var categories = await BuildCategoriesAsync(
                fromDate,
                endExclusive,
                overview.NewEnrollments,
                cancellationToken);

            return new AdminAnalyticsResponse
            {
                FromDate = fromDate,
                ToDate = toDate,
                GeneratedAtUtc = DateTime.UtcNow,
                Overview = overview,
                Revenue = revenue,
                DailyTrend = dailyTrend,
                TopCourses = topCourses,
                Categories = categories
            };
        }

        private async Task<AdminOverviewMetrics> BuildOverviewAsync(
            DateTime fromDate,
            DateTime endExclusive,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var totalUsers = await _db.AppUsers
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var activeVipUsers = await _db.AppUsers
                .AsNoTracking()
                .CountAsync(user =>
                    user.VipStatus == VipStatus.Premium &&
                    (!user.VipExpirationDate.HasValue ||
                     user.VipExpirationDate.Value > now),
                    cancellationToken);

            var publishedCourses = await _db.Courses
                .AsNoTracking()
                .CountAsync(course =>
                    course.Status == CourseStatus.Publish,
                    cancellationToken);

            var totalEnrollments = await _db.enrollments
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var newEnrollments = await _db.enrollments
                .AsNoTracking()
                .CountAsync(enrollment =>
                    enrollment.EnrollmentDate >= fromDate &&
                    enrollment.EnrollmentDate < endExclusive,
                    cancellationToken);

            var ordersInPeriod = await _db.Orders
                .AsNoTracking()
                .CountAsync(order =>
                    order.OrderDate >= fromDate &&
                    order.OrderDate < endExclusive,
                    cancellationToken);

            return new AdminOverviewMetrics
            {
                TotalUsers = totalUsers,
                ActiveVipUsers = activeVipUsers,
                PublishedCourses = publishedCourses,
                TotalEnrollments = totalEnrollments,
                NewEnrollments = newEnrollments,
                OrdersInPeriod = ordersInPeriod
            };
        }

        private async Task<AdminRevenueMetrics> BuildRevenueAsync(
            DateTime fromDate,
            DateTime endExclusive,
            CancellationToken cancellationToken)
        {
            var orders = await _db.Orders
                .AsNoTracking()
                .Where(order =>
                    order.OrderDate >= fromDate &&
                    order.OrderDate < endExclusive)
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Completed = group.Count(order =>
                        order.Status == OrderStatus.Completed),
                    Pending = group.Count(order =>
                        order.Status == OrderStatus.Pending),
                    Cancelled = group.Count(order =>
                        order.Status == OrderStatus.Cancelled),
                    Revenue = group
                        .Where(order =>
                            order.Status == OrderStatus.Completed)
                        .Sum(order => (decimal?)order.TotalAmount) ?? 0
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (orders is null)
                return new AdminRevenueMetrics();

            return new AdminRevenueMetrics
            {
                CompletedRevenue = orders.Revenue,
                AverageCompletedOrderValue = orders.Completed == 0
                    ? 0
                    : RoundMoney(orders.Revenue / orders.Completed),
                CompletedOrders = orders.Completed,
                PendingOrders = orders.Pending,
                CancelledOrders = orders.Cancelled,
                CompletionRatePercent = Percent(orders.Completed, orders.Total)
            };
        }

        private async Task<IReadOnlyList<AdminDailyMetric>> BuildDailyTrendAsync(
            DateTime fromDate,
            DateTime toDate,
            DateTime endExclusive,
            CancellationToken cancellationToken)
        {
            var enrollments = await _db.enrollments
                .AsNoTracking()
                .Where(item =>
                    item.EnrollmentDate >= fromDate &&
                    item.EnrollmentDate < endExclusive)
                .GroupBy(item => item.EnrollmentDate.Date)
                .Select(group => new
                {
                    Date = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(
                    item => item.Date,
                    item => item.Count,
                    cancellationToken);

            var orders = await _db.Orders
                .AsNoTracking()
                .Where(item =>
                    item.OrderDate >= fromDate &&
                    item.OrderDate < endExclusive &&
                    item.Status == OrderStatus.Completed)
                .GroupBy(item => item.OrderDate.Date)
                .Select(group => new
                {
                    Date = group.Key,
                    Count = group.Count(),
                    Revenue = group.Sum(item => item.TotalAmount)
                })
                .ToDictionaryAsync(
                    item => item.Date,
                    item => item,
                    cancellationToken);

            var result = new List<AdminDailyMetric>();
            for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            {
                orders.TryGetValue(date, out var orderMetric);

                result.Add(new AdminDailyMetric
                {
                    Date = date,
                    Enrollments = enrollments.GetValueOrDefault(date),
                    CompletedOrders = orderMetric?.Count ?? 0,
                    Revenue = orderMetric?.Revenue ?? 0
                });
            }

            return result;
        }

        private async Task<IReadOnlyList<AdminTopCourseMetric>>
            BuildTopCoursesAsync(
                DateTime fromDate,
                DateTime endExclusive,
                int top,
                int totalPeriodEnrollments,
                CancellationToken cancellationToken)
        {
            var courses = await _db.enrollments
                .AsNoTracking()
                .Where(item =>
                    item.EnrollmentDate >= fromDate &&
                    item.EnrollmentDate < endExclusive)
                .GroupBy(item => new
                {
                    item.CourseId,
                    item.Course.Title,
                    CategoryName = item.Course.Category.Name
                })
                .Select(group => new
                {
                    group.Key.CourseId,
                    group.Key.Title,
                    group.Key.CategoryName,
                    Enrollments = group.Count()
                })
                .OrderByDescending(item => item.Enrollments)
                .ThenBy(item => item.Title)
                .Take(top)
                .ToListAsync(cancellationToken);

            return courses
                .Select(item => new AdminTopCourseMetric
                {
                    CourseId = item.CourseId,
                    Title = item.Title,
                    CategoryName = item.CategoryName,
                    Enrollments = item.Enrollments,
                    SharePercent = Percent(
                        item.Enrollments,
                        totalPeriodEnrollments)
                })
                .ToList();
        }

        private async Task<IReadOnlyList<AdminCategoryMetric>>
            BuildCategoriesAsync(
                DateTime fromDate,
                DateTime endExclusive,
                int totalPeriodEnrollments,
                CancellationToken cancellationToken)
        {
            var categories = await _db.Categories
                .AsNoTracking()
                .Select(category => new
                {
                    category.Id,
                    category.Name,
                    PublishedCourses = category.Courses.Count(course =>
                        course.Status == CourseStatus.Publish),
                    Enrollments = category.Courses
                        .SelectMany(course => course.Enrollments)
                        .Count(enrollment =>
                            enrollment.EnrollmentDate >= fromDate &&
                            enrollment.EnrollmentDate < endExclusive)
                })
                .OrderByDescending(item => item.Enrollments)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken);

            return categories
                .Select(item => new AdminCategoryMetric
                {
                    CategoryId = item.Id,
                    CategoryName = item.Name,
                    PublishedCourses = item.PublishedCourses,
                    Enrollments = item.Enrollments,
                    EnrollmentSharePercent = Percent(
                        item.Enrollments,
                        totalPeriodEnrollments)
                })
                .ToList();
        }

        private static decimal Percent(int value, int total)
        {
            if (total <= 0)
                return 0;

            return Math.Round(
                value * 100m / total,
                2,
                MidpointRounding.AwayFromZero);
        }

        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }
}
