using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Enums;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly IAdminService _adminService;
        private readonly IRoomAvailabilityService _availability;
        private readonly ICheckoutService _checkoutService;
        private readonly IPricingService _pricing;

        public RoomsController(IRoomService roomService, IAdminService adminService,
            IRoomAvailabilityService availability, ICheckoutService checkoutService, IPricingService pricing)
        {
            _roomService = roomService;
            _adminService = adminService;
            _availability = availability;
            _checkoutService = checkoutService;
            _pricing = pricing;
        }

        /// <summary>Danh sách phòng (kèm khách đang đặt), có thể lọc theo trạng thái.</summary>
        [HttpGet]
        [Authorize(Roles = AppRoles.AllStaff)]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetAll([FromQuery] RoomStatus? trangThai)
        {
            var rooms = await _roomService.GetAllRoomsAsync();
            return Ok(rooms
                .Where(p => trangThai == null || p.TrangThai == trangThai)
                .OrderBy(p => p.SoPhong)
                .Select(p => p.ToDto()));
        }

        /// <summary>
        /// Phòng còn trống trong khoảng ngày [checkIn, checkOut) và đủ sức chứa (mặc định: hôm nay → ngày mai).
        /// Dùng cho cả khách hàng đặt online và lễ tân nhận khách vãng lai.
        /// </summary>
        [HttpGet("available")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetAvailable([FromQuery] DateTime? checkIn, [FromQuery] DateTime? checkOut, [FromQuery] int? soNguoi)
        {
            var from = (checkIn ?? DateTime.Today).Date;
            var to = (checkOut ?? from.AddDays(1)).Date;
            if (to <= from)
                throw new BusinessException("Ngày trả phòng phải sau ngày nhận phòng.");

            var rooms = await _availability.GetAvailableRoomsAsync(from, to, soNguoi);
            // Giá dự kiến cho khoảng ngày đã chọn (áp dụng giá mùa/cuối tuần/lễ)
            var quotes = await _pricing.QuoteRoomsAsync(rooms, from, to);
            return Ok(rooms.Select(room =>
            {
                var dto = room.ToDto();
                dto.TongTienDuKien = quotes[room.MaPhong].TongTien;
                dto.GiaTrungBinhDem = quotes[room.MaPhong].GiaTrungBinhDem;
                return dto;
            }));
        }

        /// <summary>Báo giá phòng cho khoảng ngày: giá từng đêm sau khi áp dụng quy tắc giá mùa/cuối tuần/lễ.</summary>
        [HttpGet("{maPhong}/quote")]
        [AllowAnonymous]
        public async Task<ActionResult<PriceQuoteDto>> Quote(string maPhong, [FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut)
        {
            return await _pricing.QuoteAsync(maPhong, checkIn, checkOut);
        }

        /// <summary>Chi tiết phòng (tiện ích, hình ảnh, khách đang đặt).</summary>
        [HttpGet("{maPhong}")]
        [Authorize(Roles = AppRoles.AllStaff)]
        public async Task<ActionResult<RoomDetailDto>> GetById(string maPhong)
        {
            var room = await _roomService.GetRoomByIdAsync(maPhong);
            if (room == null) return NotFound();
            return room.ToDetailDto();
        }

        /// <summary>Phiếu đặt đang lưu trú (đã check-in) của phòng, kèm dịch vụ và hoá đơn — dùng cho màn hình trả phòng.</summary>
        [HttpGet("{maPhong}/current-stay")]
        [Authorize(Roles = AppRoles.Cashier)]
        public async Task<ActionResult<BookingDto>> GetCurrentStay(string maPhong)
        {
            var booking = await _checkoutService.GetBookingDetailsByRoomAsync(maPhong);
            if (booking == null)
                throw new NotFoundException("Không tìm thấy phòng hoặc phòng chưa check-in.");
            return booking.ToDto();
        }

        /// <summary>Thêm phòng mới (mã phòng = "P" + số phòng).</summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Thêm phòng", "Phòng")]
        public async Task<ActionResult<RoomDetailDto>> Create(RoomRequest request)
        {
            var maPhong = "P" + request.SoPhong.Trim();
            await _adminService.AddRoomAsync(request.ToEntity(maPhong));

            var created = await _roomService.GetRoomByIdAsync(maPhong);
            return CreatedAtAction(nameof(GetById), new { maPhong }, created.ToDetailDto());
        }

        /// <summary>Cập nhật số phòng / loại phòng.</summary>
        [HttpPut("{maPhong}")]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Sửa phòng", "Phòng")]
        public async Task<IActionResult> Update(string maPhong, RoomRequest request)
        {
            await _adminService.UpdateRoomAsync(request.ToEntity(maPhong));
            return NoContent();
        }

        /// <summary>Đặt lại tiện nghi của phòng: body { "MaTienIchs": [...], "ApDungCaHang": false }. ApDungCaHang = áp cho mọi phòng cùng hạng.</summary>
        [HttpPut("{maPhong}/amenities")]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Gán tiện nghi phòng", "Phòng")]
        public async Task<ActionResult<object>> SetAmenities(string maPhong, RoomAmenitiesRequest request,
            [FromServices] IAmenityService amenityService)
        {
            var soPhong = await amenityService.SetRoomAmenitiesAsync(maPhong, request.MaTienIchs, request.ApDungCaHang);
            return Ok(new { SoPhongDaCapNhat = soPhong });
        }

        /// <summary>Xoá phòng.</summary>
        [HttpDelete("{maPhong}")]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Xoá phòng", "Phòng")]
        public async Task<IActionResult> Delete(string maPhong)
        {
            await _adminService.DeleteRoomAsync(maPhong);
            return NoContent();
        }

        /// <summary>
        /// Đổi trạng thái phòng thủ công: body { "TrangThai": "Available" | "Cleaning" | "Maintenance" }.
        /// "Đang ở" / "Đã đặt" do check-in và phiếu đặt quyết định (400); phòng đang có khách → 409.
        /// </summary>
        [HttpPatch("{maPhong}")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Đổi trạng thái phòng", "Phòng")]
        public async Task<IActionResult> UpdateStatus(string maPhong, UpdateRoomStatusRequest request)
        {
            await _roomService.UpdateRoomStatusAsync(maPhong, request.TrangThai!.Value);
            return NoContent();
        }
    }
}
