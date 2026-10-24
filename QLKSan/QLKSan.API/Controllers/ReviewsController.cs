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
    [Route("api/reviews")]
    public class ReviewsController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ICustomerService _customerService;

        public ReviewsController(IAdminService adminService, ICustomerService customerService)
        {
            _adminService = adminService;
            _customerService = customerService;
        }

        /// <summary>Tất cả đánh giá của khách hàng (mới nhất trước).</summary>
        [HttpGet]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetAll()
        {
            var reviews = await _adminService.GetAllReviewsAsync();
            return Ok(reviews.Select(d => d.ToDto()));
        }

        /// <summary>Khách hàng gửi đánh giá cho phòng đã ở (mỗi phòng chỉ đánh giá một lần).</summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.Customer)]
        public async Task<ActionResult<ReviewDto>> Create(ReviewRequest request)
        {
            var review = await _customerService.SubmitReviewAsync(
                User.GetMaKhachHang(), request.MaPhong, request.SoSao, request.NoiDung, request.AnDanh);
            return Created("/api/customers/me/reviews", review.ToDto());
        }
    }
}
