using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Models.DTOs;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize(Roles = AppRoles.Finance)]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Bảng điều khiển điều hành: công suất / khách đến / khách đi hôm nay, doanh thu thuần – ADR – RevPAR 30 ngày
        /// (so với 30 ngày trước), công suất đã đặt 14 ngày tới, doanh thu theo tháng, việc cần xử lý, đánh giá mới.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<DashboardDto>> Get()
        {
            return await _dashboardService.GetAsync();
        }
    }
}
