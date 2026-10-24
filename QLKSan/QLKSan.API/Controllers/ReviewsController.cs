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
        private readonly IRoomTypeService _roomTypeService;

        public ReviewsController(IAdminService adminService, ICustomerService customerService, IRoomTypeService roomTypeService)
        {
            _adminService = adminService;
            _customerService = customerService;
            _roomTypeService = roomTypeService;
        }

        /// <summary>Tất cả đánh giá của khách hàng (mới nhất trước).</summary>
        [HttpGet]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetAll()
        {
            var reviews = await _adminService.GetAllReviewsAsync();
            return Ok(reviews.Select(d => d.ToDto()));
        }

        /// <summary>Đánh giá tốt gần đây (từ 4 sao) để giới thiệu trên trang chủ — công khai, ẩn danh tính khách.</summary>
        [HttpGet("featured")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> Featured([FromQuery] int soLuong = 3)
        {
            return Ok(await _roomTypeService.GetFeaturedReviewsAsync(Math.Clamp(soLuong, 1, 12)));
        }

        /// <summary>Khách hàng đánh giá một kỳ nghỉ đã trả phòng (MaDatPhong; mỗi lần lưu trú đánh giá một lần).</summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.Customer)]
        public async Task<ActionResult<ReviewDto>> Create(ReviewRequest request)
        {
            var review = await _customerService.SubmitReviewAsync(
                User.GetMaKhachHang(), request.MaDatPhong, request.MaPhong, request.SoSao, request.NoiDung, request.AnDanh);
            return Created("/api/customers/me/reviews", review.ToDto());
        }
    }
}
