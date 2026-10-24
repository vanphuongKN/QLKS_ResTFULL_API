using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Models.DTOs;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IAccountingService _accountingService;

        public ReportsController(IAccountingService accountingService)
        {
            _accountingService = accountingService;
        }

        // Mặc định: từ đầu tháng hiện tại đến hôm nay
        private static (DateTime From, DateTime To) Range(DateTime? tuNgay, DateTime? denNgay)
            => ((tuNgay ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date, (denNgay ?? DateTime.Today).Date);

        /// <summary>Báo cáo doanh thu: tổng hợp, theo ngày, loại phòng, dịch vụ, phương thức thanh toán, nhân viên thu.</summary>
        [HttpGet("revenue")]
        [Authorize(Roles = AppRoles.Finance)]
        public async Task<ActionResult<RevenueReportDto>> Revenue([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
        {
            var (from, to) = Range(tuNgay, denNgay);
            return await _accountingService.GetRevenueReportAsync(from, to);
        }

        /// <summary>Công suất phòng theo ngày (số phòng có khách / tổng số phòng).</summary>
        [HttpGet("occupancy")]
        [Authorize(Roles = AppRoles.Finance)]
        public async Task<ActionResult<OccupancyReportDto>> Occupancy([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
        {
            var (from, to) = Range(tuNgay, denNgay);
            return await _accountingService.GetOccupancyReportAsync(from, to);
        }

        /// <summary>Sổ khách lưu trú theo khoảng ngày (họ tên, CCCD, quốc tịch, phòng, ngày đến/đi) — phục vụ thông báo lưu trú.</summary>
        [HttpGet("guests")]
        [Authorize(Roles = AppRoles.Cashier)]
        public async Task<ActionResult<IEnumerable<GuestRegisterDto>>> Guests([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
        {
            var from = (tuNgay ?? DateTime.Today).Date;
            var to = (denNgay ?? from).Date;
            return Ok(await _accountingService.GetGuestRegisterAsync(from, to));
        }

        /// <summary>Xuất sổ khách lưu trú ra Excel.</summary>
        [HttpGet("guests/export")]
        [Authorize(Roles = AppRoles.Cashier)]
        public async Task<IActionResult> ExportGuests([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
        {
            var from = (tuNgay ?? DateTime.Today).Date;
            var to = (denNgay ?? from).Date;
            var file = await _accountingService.ExportGuestRegisterExcelAsync(from, to);
            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SoKhachLuuTru_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
        }

        /// <summary>Xuất báo cáo doanh thu ra file Excel (.xlsx).</summary>
        [HttpGet("revenue/export")]
        [Authorize(Roles = AppRoles.Finance)]
        public async Task<IActionResult> ExportRevenue([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
        {
            var (from, to) = Range(tuNgay, denNgay);
            var file = await _accountingService.ExportRevenueExcelAsync(from, to);
            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"BaoCaoDoanhThu_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
        }
    }
}
