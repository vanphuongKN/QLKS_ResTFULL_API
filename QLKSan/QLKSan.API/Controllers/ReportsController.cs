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
        private readonly IFrontDeskService _frontDeskService;

        public ReportsController(IAccountingService accountingService, IFrontDeskService frontDeskService)
        {
            _accountingService = accountingService;
            _frontDeskService = frontDeskService;
        }

        // Mặc định: từ đầu tháng hiện tại đến hôm nay
        private static (DateTime From, DateTime To) Range(DateTime? tuNgay, DateTime? denNgay)
            => ((tuNgay ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date, (denNgay ?? DateTime.Today).Date);

        /// <summary>Báo cáo doanh thu: tổng hợp, theo ngày, loại phòng, dịch vụ, phương thức thanh toán, nhân viên thu.</summary>
        /// <summary>
        /// Tổng quan kế toán: doanh thu thuần, ADR, RevPAR, công suất của kỳ (mặc định từ đầu tháng) so với kỳ liền trước cùng độ dài;
        /// doanh thu theo kênh đặt; việc cần đối soát (hoá đơn chưa thu, cọc đang giữ, cọc giữ lại khi huỷ, HĐ công ty chưa xuất, ca lệch tiền).
        /// </summary>
        [HttpGet("overview")]
        [Authorize(Roles = AppRoles.Finance)]
        public async Task<ActionResult<AccountingOverviewDto>> Overview([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
        {
            var (from, to) = Range(tuNgay, denNgay);
            return await _accountingService.GetOverviewAsync(from, to);
        }

        /// <summary>Sổ cọc: cọc đang giữ cho phiếu chưa trả phòng, giao dịch thu / hoàn cọc trong kỳ theo phương thức, cọc giữ lại khi huỷ.</summary>
        [HttpGet("deposits")]
        [Authorize(Roles = AppRoles.Finance)]
        public async Task<ActionResult<DepositReportDto>> Deposits([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
        {
            var (from, to) = Range(tuNgay, denNgay);
            return await _accountingService.GetDepositReportAsync(from, to);
        }

        /// <summary>Các lần chốt ca / bàn giao của lễ tân trong kỳ (tiền mặt hệ thống / thực tế, chênh lệch, theo phương thức).</summary>
        [HttpGet("shifts")]
        [Authorize(Roles = AppRoles.Finance)]
        public async Task<ActionResult<IEnumerable<ShiftHandoverDto>>> Shifts([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
        {
            var (from, to) = Range(tuNgay, denNgay);
            return Ok(await _frontDeskService.GetHandoversAsync(from, to));
        }

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
