using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IReceptionService _receptionService;
        private readonly ICustomerService _customerService;
        private readonly IServiceManager _serviceManager;
        private readonly IStayService _stayService;

        public BookingsController(IReceptionService receptionService, ICustomerService customerService, IServiceManager serviceManager, IStayService stayService)
        {
            _receptionService = receptionService;
            _customerService = customerService;
            _serviceManager = serviceManager;
            _stayService = stayService;
        }

        /// <summary>Lịch sử đặt phòng, tìm theo mã phiếu, tên khách hoặc số phòng (không dấu, không phân biệt hoa thường).</summary>
        [HttpGet]
        [Authorize(Roles = AppRoles.Cashier)]
        public async Task<ActionResult<IEnumerable<BookingDto>>> GetAll([FromQuery] string search)
        {
            var bookings = await _receptionService.GetAllBookingsAsync(search);
            return Ok(bookings.Select(p => p.ToDto()));
        }

        /// <summary>Phiếu đặt online đang chờ duyệt hoặc đã duyệt (chưa check-in).</summary>
        [HttpGet("online")]
        [Authorize(Roles = AppRoles.Cashier)]
        public async Task<ActionResult<IEnumerable<BookingDto>>> GetOnline()
        {
            var bookings = await _receptionService.GetOnlineBookingsAsync();
            return Ok(bookings.Select(p => p.ToDto()));
        }

        /// <summary>Các phiếu đang lưu trú (đã check-in), kèm dịch vụ đã dùng.</summary>
        [HttpGet("active")]
        [Authorize(Roles = AppRoles.Cashier)]
        public async Task<ActionResult<IEnumerable<BookingDto>>> GetActive()
        {
            var bookings = await _serviceManager.GetActiveStaysAsync();
            return Ok(bookings.Select(p => p.ToDto()));
        }

        /// <summary>Lịch phòng: các phiếu có thời gian lưu trú nằm trong khoảng ngày (mặc định 14 ngày từ hôm nay).</summary>
        [HttpGet("timeline")]
        [Authorize(Roles = AppRoles.Cashier)]
        public async Task<ActionResult<IEnumerable<TimelineBookingDto>>> Timeline([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
        {
            var from = (tuNgay ?? DateTime.Today).Date;
            var to = (denNgay ?? from.AddDays(13)).Date;
            if (to < from) throw new BusinessException("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
            if ((to - from).Days > 62) throw new BusinessException("Chỉ xem tối đa 2 tháng một lần.");
            return Ok(await _receptionService.GetTimelineAsync(from, to));
        }

        /// <summary>Chi tiết phiếu đặt phòng. Khách hàng chỉ xem được phiếu của chính mình.</summary>
        [HttpGet("{maDatPhong}")]
        public async Task<ActionResult<BookingDto>> GetById(string maDatPhong)
        {
            var booking = await _receptionService.GetBookingByIdAsync(maDatPhong);
            if (booking == null) return NotFound();

            if (User.IsInRole(AppRoles.Customer) && booking.MaKhachHang != User.GetMaKhachHang())
                return Forbid();
            if (User.IsInRole(AppRoles.Housekeeping))
                return Forbid();

            return booking.ToDto();
        }

        /// <summary>
        /// Khách hàng đặt phòng online theo hạng phòng (MaLoai, hệ thống tự xếp phòng trống) hoặc đích danh (MaPhong).
        /// Còn phòng thì phiếu được xác nhận ngay (Đã duyệt); hết phòng → 409.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.Customer)]
        [Audit("Đặt phòng online", "Phiếu đặt phòng")]
        public async Task<ActionResult<BookingDto>> Create(BookRoomRequest request)
        {
            var maDatPhong = await _customerService.CreateOnlineBookingAsync(User.GetMaKhachHang(), request);

            var created = await _receptionService.GetBookingByIdAsync(maDatPhong);
            return CreatedAtAction(nameof(GetById), new { maDatPhong }, created.ToDto());
        }

        /// <summary>
        /// Lễ tân đặt phòng cho ngày tới (khách gọi điện, email, đến quầy, đại lý): theo hạng phòng (MaLoai, tự xếp phòng) hoặc đích danh (MaPhong).
        /// Khách nhận diện theo CCCD (chưa có hồ sơ thì tạo mới). Phiếu được xác nhận ngay; TienCoc &gt; 0 thì ghi vào sổ cọc.
        /// </summary>
        [HttpPost("front-desk")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Đặt phòng qua quầy", "Phiếu đặt phòng")]
        public async Task<ActionResult<BookingDto>> CreateAtFrontDesk(FrontDeskBookingRequest request)
        {
            var maDatPhong = await _receptionService.CreateFrontDeskBookingAsync(request, User.GetMaNhanVien());

            var created = await _receptionService.GetBookingByIdAsync(maDatPhong);
            return CreatedAtAction(nameof(GetById), new { maDatPhong }, created.ToDto());
        }

        /// <summary>Thu thêm tiền cọc / đảm bảo cho phiếu chưa trả phòng (ghi vào sổ cọc, trừ vào hoá đơn khi trả phòng).</summary>
        [HttpPost("{maDatPhong}/deposits")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Thu tiền cọc", "Phiếu đặt phòng")]
        public async Task<ActionResult<BookingDto>> AddDeposit(string maDatPhong, DepositRequest request)
        {
            await _receptionService.AddDepositAsync(maDatPhong, request, User.GetMaNhanVien());
            var booking = await _receptionService.GetBookingByIdAsync(maDatPhong);
            return booking.ToDto();
        }

        /// <summary>Lễ tân đổi phòng đã xếp cho phiếu chưa nhận phòng (phòng mới phải trống trong khoảng ngày và đủ sức chứa).</summary>
        [HttpPost("{maDatPhong}/change-room")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Đổi phòng", "Phiếu đặt phòng")]
        public async Task<ActionResult<BookingDto>> ChangeRoom(string maDatPhong, ChangeRoomRequest request)
        {
            await _receptionService.ChangeRoomAsync(maDatPhong, request.MaPhong);
            var booking = await _receptionService.GetBookingByIdAsync(maDatPhong);
            return booking.ToDto();
        }

        /// <summary>Lễ tân tạo đặt phòng và check-in ngay cho khách vãng lai.</summary>
        [HttpPost("walk-in")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Nhận khách vãng lai", "Phiếu đặt phòng")]
        public async Task<ActionResult<BookingDto>> WalkIn(CheckInRequest request)
        {
            var maDatPhong = await _receptionService.ProcessCheckInAsync(request, User.GetMaNhanVien());

            var created = await _receptionService.GetBookingByIdAsync(maDatPhong);
            return CreatedAtAction(nameof(GetById), new { maDatPhong }, created.ToDto());
        }

        /// <summary>Duyệt phiếu đặt online đang chờ duyệt.</summary>
        [HttpPost("{maDatPhong}/approve")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Duyệt phiếu", "Phiếu đặt phòng")]
        public async Task<IActionResult> Approve(string maDatPhong)
        {
            await _receptionService.ApproveOnlineBookingAsync(maDatPhong);
            return NoContent();
        }

        /// <summary>
        /// Huỷ phiếu chưa nhận phòng. Khách hàng: chỉ phiếu của mình, trước ngày nhận phòng.
        /// Lễ tân/Admin: từ chối phiếu chờ duyệt hoặc huỷ phiếu đã duyệt.
        /// </summary>
        [HttpPost("{maDatPhong}/cancel")]
        [Audit("Huỷ phiếu", "Phiếu đặt phòng")]
        public async Task<IActionResult> Cancel(string maDatPhong, CancelBookingRequest request)
        {
            var laKhachHang = User.IsInRole(AppRoles.Customer);
            if (laKhachHang)
            {
                var booking = await _receptionService.GetBookingByIdAsync(maDatPhong);
                if (booking == null) return NotFound();
                if (booking.MaKhachHang != User.GetMaKhachHang()) return Forbid();
            }
            else if (!User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.Receptionist))
            {
                return Forbid();
            }

            var nguoiHuy = laKhachHang ? "Khách hàng" : $"Nhân viên {User.Identity?.Name}";
            await _receptionService.CancelBookingAsync(maDatPhong, request?.LyDo, nguoiHuy, laKhachHang, request?.HoanCoc ?? false,
                laKhachHang ? null : User.GetMaNhanVien());
            return NoContent();
        }

        /// <summary>
        /// Nhận phòng cho phiếu đã đặt (chuyển phòng sang Đang ở). Body tuỳ chọn: số khách thực tế, tiền cọc thu thêm và phương thức.
        /// </summary>
        [HttpPost("{maDatPhong}/check-in")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Nhận phòng", "Phiếu đặt phòng")]
        public async Task<IActionResult> CheckIn(string maDatPhong, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CheckInBookingRequest request = null)
        {
            await _receptionService.CheckInOnlineBookingAsync(maDatPhong, User.GetMaNhanVien(), request);
            return NoContent();
        }

        /// <summary>Báo giá khi gia hạn / rút ngắn / đổi ngày: tiền phòng cũ – mới theo giá từng đêm, phòng còn trống không.</summary>
        [HttpGet("{maDatPhong}/stay-dates/quote")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        public async Task<ActionResult<StayChangeQuoteDto>> QuoteStayDates(string maDatPhong, [FromQuery] DateTime? ngayNhan, [FromQuery] DateTime ngayTra)
        {
            return await _stayService.QuoteStayChangeAsync(maDatPhong, ngayNhan, ngayTra);
        }

        /// <summary>
        /// Gia hạn / rút ngắn kỳ nghỉ (khách đang ở: chỉ đổi ngày trả, không trước hôm nay) hoặc đổi ngày (phiếu chưa nhận phòng).
        /// Phòng phải trống trong khoảng ngày mới; ghi chú thay đổi vào phiếu.
        /// </summary>
        [HttpPut("{maDatPhong}/stay-dates")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Đổi ngày lưu trú", "Phiếu đặt phòng")]
        public async Task<ActionResult<BookingDto>> ChangeStayDates(string maDatPhong, StayDatesRequest request)
        {
            await _stayService.ChangeStayDatesAsync(maDatPhong, request.NgayNhan, request.NgayTra, $"Nhân viên {User.Identity?.Name}");
            return (await _receptionService.GetBookingByIdAsync(maDatPhong)).ToDto();
        }

        /// <summary>Đổi phòng khi khách đang ở: phòng mới phải trống, đã dọn; phòng cũ chuyển Đang dọn. GiuGiaCu = true giữ giá cũ.</summary>
        [HttpPost("{maDatPhong}/move")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Đổi phòng khi đang ở", "Phiếu đặt phòng")]
        public async Task<ActionResult<BookingDto>> MoveRoom(string maDatPhong, MoveRoomRequest request)
        {
            await _stayService.MoveRoomAsync(maDatPhong, request, User.GetMaNhanVien(), $"Nhân viên {User.Identity?.Name}");
            return (await _receptionService.GetBookingByIdAsync(maDatPhong)).ToDto();
        }

        /// <summary>Huỷ dịch vụ gọi nhầm (bớt số lượng hoặc huỷ hẳn) khi hoá đơn chưa thanh toán; lý do ghi vào phiếu.</summary>
        [HttpPost("{maDatPhong}/services/{maSDDV}/void")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Huỷ dịch vụ", "Phiếu đặt phòng")]
        public async Task<ActionResult<BookingDto>> VoidService(string maDatPhong, string maSDDV, VoidServiceRequest request)
        {
            await _stayService.VoidServiceAsync(maDatPhong, maSDDV, request.SoLuong, request.LyDo, $"Nhân viên {User.Identity?.Name}");
            return (await _receptionService.GetBookingByIdAsync(maDatPhong)).ToDto();
        }

        /// <summary>Khai báo người đi cùng (thay toàn bộ danh sách); số khách của phiếu tăng theo nếu cần, không vượt sức chứa.</summary>
        [HttpPut("{maDatPhong}/companions")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Khai báo người đi cùng", "Phiếu đặt phòng")]
        public async Task<ActionResult<BookingDto>> SetCompanions(string maDatPhong, CompanionsRequest request)
        {
            await _stayService.SetCompanionsAsync(maDatPhong, request.KhachDiCung);
            return (await _receptionService.GetBookingByIdAsync(maDatPhong)).ToDto();
        }

        /// <summary>Gọi dịch vụ cho khách đang lưu trú; trả về phiếu đặt sau khi cập nhật.</summary>
        [HttpPost("{maDatPhong}/services")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Gọi dịch vụ", "Phiếu đặt phòng")]
        public async Task<ActionResult<BookingDto>> OrderServices(string maDatPhong, OrderServiceRequest request)
        {
            foreach (var item in request.Services)
            {
                await _serviceManager.OrderServiceAsync(maDatPhong, item.MaDichVu, item.SoLuong);
            }

            var booking = await _receptionService.GetBookingByIdAsync(maDatPhong);
            return booking.ToDto();
        }
    }
}
