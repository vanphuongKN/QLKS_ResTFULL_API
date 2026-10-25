using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/guest-requests")]
    [Authorize(Roles = AppRoles.FrontDesk)]
    public class GuestRequestsController : ControllerBase
    {
        private readonly IGuestRequestService _guestRequestService;

        public GuestRequestsController(IGuestRequestService guestRequestService)
        {
            _guestRequestService = guestRequestService;
        }

        /// <summary>Sổ yêu cầu khách. loc = mo (mới / đang xử lý), xong (hoàn thành / huỷ trong 7 ngày), trống: tất cả 30 ngày; lọc theo phiếu đặt.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GuestRequestDto>>> GetAll([FromQuery] string loc, [FromQuery] string maDatPhong)
        {
            return Ok(await _guestRequestService.ListAsync(loc, maDatPhong));
        }

        /// <summary>Ghi yêu cầu của khách (báo thức, đặt xe, nhắn gửi, đồ thất lạc, buồng phòng, kỹ thuật…), có thể kèm giờ hẹn.</summary>
        [HttpPost]
        [Audit("Ghi yêu cầu khách", "Yêu cầu khách")]
        public async Task<ActionResult<GuestRequestDto>> Create(GuestRequestCreateRequest request)
        {
            var created = await _guestRequestService.CreateAsync(request, User.GetMaNhanVien());
            return Created($"/api/guest-requests/{created.MaYeuCau}", created);
        }

        /// <summary>Chuyển trạng thái: đang xử lý / hoàn thành / huỷ (huỷ phải có lý do); yêu cầu đã xong không đổi được nữa.</summary>
        [HttpPatch("{maYeuCau:int}")]
        [Audit("Cập nhật yêu cầu khách", "Yêu cầu khách")]
        public async Task<ActionResult<GuestRequestDto>> UpdateStatus(int maYeuCau, GuestRequestUpdateRequest request)
        {
            return await _guestRequestService.UpdateStatusAsync(maYeuCau, request.TrangThai!.Value, request.KetQua, User.GetMaNhanVien());
        }
    }
}
