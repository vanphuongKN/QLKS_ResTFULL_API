using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize(Roles = AppRoles.Finance)]
    public class DashboardController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public DashboardController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>Số liệu tổng quan: doanh thu, số đặt phòng, số khách, doanh thu tuần và theo tháng của năm.</summary>
        [HttpGet]
        public async Task<ActionResult<DashboardDto>> Get([FromQuery] int? year)
        {
            var promotions = await _adminService.GetAllPromotionsAsync();
            return new DashboardDto
            {
                TongDoanhThu = await _adminService.GetTotalRevenueAsync(),
                TongDatPhong = await _adminService.GetTotalBookingsAsync(),
                TongKhachHang = await _adminService.GetTotalCustomersAsync(),
                DoanhThuTuan = await _adminService.GetWeeklyRevenueAsync(),
                DoanhThuTheoThang = await _adminService.GetYearlyRevenueHistoryAsync(year ?? DateTime.Now.Year),
                KhuyenMais = promotions.Select(k => k.ToDto()).ToList()
            };
        }
    }
}
