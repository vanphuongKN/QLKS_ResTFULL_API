using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/front-desk")]
    [Authorize(Roles = AppRoles.FrontDesk)]
    public class FrontDeskController : ControllerBase
    {
        private readonly IFrontDeskService _frontDeskService;

        public FrontDeskController(IFrontDeskService frontDeskService)
        {
            _frontDeskService = frontDeskService;
        }

        /// <summary>
        /// Màn hình đầu ca: khách đến / khách đi (kể cả quá hạn trả) / đang lưu trú hôm nay, phiếu sắp đến,
        /// tình trạng phòng và việc buồng phòng chưa xong. Mỗi phiếu kèm VIP, sở thích, hạn chế, yêu cầu riêng.
        /// </summary>
        [HttpGet("today")]
        public async Task<ActionResult<FrontDeskTodayDto>> Today()
        {
            return await _frontDeskService.GetTodayAsync();
        }

        /// <summary>
        /// Ca hiện tại của người đang đăng nhập: tiền thu / chi theo phương thức (thanh toán, hoàn tiền hoá đơn, thu / hoàn cọc)
        /// từ lần chốt ca gần nhất trong 24 giờ (hoặc từ 0h hôm nay) tới hiện tại; không tính phần trả bằng điểm thưởng.
        /// </summary>
        [HttpGet("shift")]
        public async Task<ActionResult<ShiftSummaryDto>> Shift()
        {
            return await _frontDeskService.GetShiftSummaryAsync(User.GetMaNhanVien());
        }

        /// <summary>Chốt ca: lưu tổng thu theo phương thức, tiền mặt hệ thống so với tiền đếm thực tế (chênh lệch) và ghi chú bàn giao.</summary>
        [HttpPost("shift/close")]
        [Audit("Chốt ca / bàn giao", "Giao ca")]
        public async Task<ActionResult<ShiftHandoverDto>> CloseShift(CloseShiftRequest request)
        {
            return await _frontDeskService.CloseShiftAsync(User.GetMaNhanVien(), request.TienMatThucTe, request.GhiChuBanGiao);
        }

        /// <summary>Các lần chốt ca / bàn giao trong số ngày gần đây (mặc định 7, tối đa 90).</summary>
        [HttpGet("shifts")]
        public async Task<ActionResult<IEnumerable<ShiftHandoverDto>>> Shifts([FromQuery] int soNgay = 7)
        {
            return Ok(await _frontDeskService.GetHandoversAsync(soNgay));
        }
    }
}
