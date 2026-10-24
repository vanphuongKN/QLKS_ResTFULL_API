using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public CustomersController(IReceptionService receptionService, ICustomerService customerService)
        {
            _receptionService = receptionService;
            _customerService = customerService;
        }

        /// <summary>Danh sách khách hàng, tìm theo tên, CCCD hoặc số điện thoại.</summary>
        [HttpGet]
        [Authorize(Roles = AppRoles.Cashier)]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll([FromQuery] string search)
        {
            var customers = await _receptionService.GetAllCustomersAsync(search);
            return Ok(customers.Select(k => k.ToDto()));
        }

        /// <summary>Hồ sơ khách hàng kèm lịch sử đặt phòng.</summary>
        [HttpGet("{maKhachHang}")]
        [Authorize(Roles = AppRoles.Cashier)]
        public async Task<ActionResult<CustomerDetailDto>> GetById(string maKhachHang)
        {
            var customer = await _receptionService.GetCustomerDetailsAsync(maKhachHang);
            if (customer == null) return NotFound();
            return customer.ToDetailDto();
        }

        /// <summary>Tìm khách hàng theo số CCCD/Passport.</summary>
        [HttpGet("by-cccd/{cccd}")]
        [Authorize(Roles = AppRoles.Cashier)]
        public async Task<ActionResult<CustomerDto>> GetByCccd(string cccd)
        {
            var customer = await _receptionService.GetCustomerByCccdAsync(cccd);
            if (customer == null) return NotFound();
            return customer.ToDto();
        }

        /// <summary>Hồ sơ của khách hàng đang đăng nhập.</summary>
        [HttpGet("me")]
        [Authorize(Roles = AppRoles.Customer)]
        public async Task<ActionResult<CustomerDto>> GetMe()
        {
            var customer = await _customerService.GetCustomerByAccountIdAsync(User.GetMaDangNhap());
            if (customer == null) return NotFound();
            return customer.ToDto();
        }

        /// <summary>Khách hàng cập nhật liên hệ của mình: SĐT, email, địa chỉ, quốc gia.</summary>
        [HttpPut("me/contact")]
        [Authorize(Roles = AppRoles.Customer)]
        public async Task<ActionResult<CustomerDto>> UpdateMyContact(UpdateContactRequest request)
        {
            var customer = await _customerService.UpdateContactAsync(User.GetMaKhachHang(), request);
            return customer.ToDto();
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
