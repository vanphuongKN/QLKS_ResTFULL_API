using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/night-audit")]
    [Authorize(Roles = AppRoles.Cashier)]
    public class NightAuditController : ControllerBase
    {
        private readonly INightAuditService _nightAuditService;

        public NightAuditController(INightAuditService nightAuditService)
        {
            _nightAuditService = nightAuditService;
        }

        /// <summary>Tình trạng chốt ngày: ngày chốt gần nhất, ngày cần chốt tiếp theo, số ngày đã qua chưa chốt.</summary>
        [HttpGet("status")]
        public async Task<ActionResult<NightAuditStatusDto>> Status()
        {
            return await _nightAuditService.GetStatusAsync();
        }

        /// <summary>Xem trước số liệu một ngày (mặc định: ngày cần chốt tiếp theo), khách không đến và cảnh báo.</summary>
        [HttpGet("preview")]
        public async Task<ActionResult<NightAuditPreviewDto>> Preview([FromQuery] DateTime? ngay)
        {
            return await _nightAuditService.PreviewAsync(ngay);
        }

        /// <summary>
        /// Chốt một ngày (lần lượt, chỉ ngày đã qua). Phiếu chưa nhận phòng tới ngày đó bị huỷ, giữ cọc —
        /// trừ các phiếu trong GiuLai (khách báo đến muộn).
        /// </summary>
        [HttpPost("close")]
        [Audit("Chốt ngày", "Chốt ngày")]
        public async Task<ActionResult<DayCloseDto>> Close(CloseDayRequest request)
        {
            var created = await _nightAuditService.CloseAsync(request.Ngay!.Value, request.GiuLai, request.GhiChu, User.GetMaNhanVien());
            return Created($"/api/night-audit/{created.Ngay:yyyy-MM-dd}", created);
        }

        /// <summary>Các ngày đã chốt trong khoảng (mặc định 30 ngày gần nhất) — báo cáo doanh thu theo đêm.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DayCloseDto>>> GetAll([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
        {
            var den = denNgay ?? DateTime.Today;
            return Ok(await _nightAuditService.ListAsync(tuNgay ?? den.AddDays(-30), den));
        }

        /// <summary>Số liệu một ngày đã chốt (ngày dạng yyyy-MM-dd).</summary>
        [HttpGet("{ngay}")]
        public async Task<ActionResult<DayCloseDto>> Get(string ngay)
        {
            return await _nightAuditService.GetAsync(ParseNgay(ngay));
        }

        /// <summary>Quản lý mở lại ngày chốt gần nhất (bắt buộc lý do, ghi nhật ký) để sửa rồi chốt lại.</summary>
        [HttpPost("{ngay}/reopen")]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Mở lại chốt ngày", "Chốt ngày")]
        public async Task<IActionResult> Reopen(string ngay, ReopenDayRequest request)
        {
            await _nightAuditService.ReopenAsync(ParseNgay(ngay), request.LyDo, User.Identity?.Name);
            return NoContent();
        }

        private static DateTime ParseNgay(string ngay)
            => DateTime.TryParseExact(ngay, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d
                : throw new BusinessException("Ngày phải có dạng yyyy-MM-dd.");
    }
}
