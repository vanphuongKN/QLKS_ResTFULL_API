using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/customers")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly IReceptionService _receptionService;
        private readonly ICustomerService _customerService;
        private readonly ICustomerProfileService _profileService;
        private readonly LoyaltyPolicy _loyalty;

        public CustomersController(IReceptionService receptionService, ICustomerService customerService, ICustomerProfileService profileService,
            IOptions<LoyaltyPolicy> loyalty)
        {
            _receptionService = receptionService;
            _customerService = customerService;
            _profileService = profileService;
            _loyalty = loyalty.Value;
        }

        /// <summary>Danh sách khách hàng, tìm theo tên, CCCD hoặc số điện thoại.</summary>
        [HttpGet]
        [Authorize(Roles = AppRoles.Cashier)]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll([FromQuery] string search)
        {
            var customers = await _receptionService.GetAllCustomersAsync(search);
            return Ok(customers.Select(k => k.ToDto().WithMembership(_loyalty)));
        }

        /// <summary>Hồ sơ khách hàng kèm lịch sử đặt phòng.</summary>
        [HttpGet("{maKhachHang}")]
        [Authorize(Roles = AppRoles.Cashier)]
        public async Task<ActionResult<CustomerDetailDto>> GetById(string maKhachHang)
        {
            var customer = await _receptionService.GetCustomerDetailsAsync(maKhachHang);
            if (customer == null) return NotFound();
            return customer.ToDetailDto().WithMembership(_loyalty);
        }

        /// <summary>Lễ tân / quản lý cập nhật hồ sơ khách: thông tin cá nhân, liên hệ, VIP, sở thích, ghi chú nội bộ, hạn chế đặt online.</summary>
        [HttpPut("{maKhachHang}")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Sửa hồ sơ khách", "Khách hàng")]
        public async Task<ActionResult<CustomerDto>> UpdateProfile(string maKhachHang, UpdateCustomerProfileRequest request)
        {
            var customer = await _profileService.UpdateProfileAsync(maKhachHang, request);
            return customer.ToDto().WithMembership(_loyalty);
        }

        /// <summary>Lịch sử điểm của khách (mới nhất trước).</summary>
        [HttpGet("{maKhachHang}/points")]
        [Authorize(Roles = AppRoles.Cashier)]
        public async Task<ActionResult<IEnumerable<PointHistoryDto>>> GetPointHistory(string maKhachHang)
        {
            return Ok((await _profileService.GetPointHistoryAsync(maKhachHang)).Select(l => l.ToDto()));
        }

        /// <summary>Quản lý cộng / trừ điểm thủ công kèm lý do: body { "SoDiem": ±n, "LyDo": ... }.</summary>
        [HttpPost("{maKhachHang}/point-adjustments")]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Điều chỉnh điểm", "Khách hàng")]
        public async Task<ActionResult<PointHistoryDto>> AdjustPoints(string maKhachHang, PointAdjustmentRequest request)
        {
            var row = await _profileService.AdjustPointsAsync(maKhachHang, request.SoDiem, request.LyDo, User.GetMaNhanVien());
            return Created($"/api/customers/{maKhachHang}/points", row.ToDto());
        }

        /// <summary>Tìm khách hàng theo số CCCD/Passport.</summary>
        [HttpGet("by-cccd/{cccd}")]
        [Authorize(Roles = AppRoles.Cashier)]
        public async Task<ActionResult<CustomerDto>> GetByCccd(string cccd)
        {
            var customer = await _receptionService.GetCustomerByCccdAsync(cccd);
            if (customer == null) return NotFound();
            return customer.ToDto().WithMembership(_loyalty);
        }

        /// <summary>Hồ sơ của khách hàng đang đăng nhập (kèm hạng thành viên và quyền lợi).</summary>
        [HttpGet("me")]
        [Authorize(Roles = AppRoles.Customer)]
        public async Task<ActionResult<CustomerDto>> GetMe()
        {
            var customer = await _customerService.GetCustomerByAccountIdAsync(User.GetMaDangNhap());
            if (customer == null) return NotFound();
            return customer.ToDto().WithMembership(_loyalty).WithoutInternalNotes();
        }

        /// <summary>Khách hàng cập nhật liên hệ của mình: SĐT, email, địa chỉ, quốc gia.</summary>
        [HttpPut("me/contact")]
        [Authorize(Roles = AppRoles.Customer)]
        public async Task<ActionResult<CustomerDto>> UpdateMyContact(UpdateContactRequest request)
        {
            var customer = await _customerService.UpdateContactAsync(User.GetMaKhachHang(), request);
            return customer.ToDto().WithMembership(_loyalty).WithoutInternalNotes();
        }

        /// <summary>Lịch sử điểm của khách hàng đang đăng nhập.</summary>
        [HttpGet("me/points")]
        [Authorize(Roles = AppRoles.Customer)]
        public async Task<ActionResult<IEnumerable<PointHistoryDto>>> GetMyPointHistory()
        {
            return Ok((await _profileService.GetPointHistoryAsync(User.GetMaKhachHang())).Select(l => l.ToDto()));
        }

        /// <summary>Lịch sử đặt phòng của khách hàng đang đăng nhập (kèm hoá đơn, thanh toán).</summary>
        [HttpGet("me/bookings")]
        [Authorize(Roles = AppRoles.Customer)]
        public async Task<ActionResult<IEnumerable<BookingDto>>> GetMyBookings()
        {
            var bookings = await _customerService.GetBookingsByCustomerAsync(User.GetMaKhachHang());
            return Ok(bookings.Select(p => p.ToDto()));
        }

        /// <summary>Các đánh giá khách hàng đang đăng nhập đã gửi.</summary>
        [HttpGet("me/reviews")]
        [Authorize(Roles = AppRoles.Customer)]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetMyReviews()
        {
            var reviews = await _customerService.GetReviewsByCustomerAsync(User.GetMaKhachHang());
            return Ok(reviews.Select(d => d.ToDto()));
        }
    }
}
