using CourseManagement.Business.Services.IService;
using CourseManagement.Model.Constant;
using CourseManagement.Model.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagementAPI.Controllers
{
    [ApiController]
    [Authorize(Roles = Role.Role_User_Admin)]
    [Route("api/admin/analytics")]
    public sealed class AdminAnalyticsController : ControllerBase
    {
        private readonly IAdminAnalyticsService _analyticsService;

        public AdminAnalyticsController(
            IAdminAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// Tổng hợp số liệu người dùng, khóa học, đăng ký và doanh thu.
        /// Nếu không truyền ngày, API sử dụng 30 ngày gần nhất.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(
            typeof(AdminAnalyticsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AdminAnalyticsResponse>> Get(
            [FromQuery] AdminAnalyticsRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _analyticsService.GetDashboardAsync(
                    request,
                    cancellationToken);

                return Ok(result);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new
                {
                    message = exception.Message
                });
            }
        }
    }
}
