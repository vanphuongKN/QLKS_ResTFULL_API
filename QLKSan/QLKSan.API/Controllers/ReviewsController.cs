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
        private readonly IReviewService _reviewService;
        private readonly ICustomerService _customerService;
        private readonly IRoomTypeService _roomTypeService;

        public ReviewsController(IReviewService reviewService, ICustomerService customerService, IRoomTypeService roomTypeService)
        {
            _reviewService = reviewService;
            _customerService = customerService;
            _roomTypeService = roomTypeService;
        }

        /// <summary>Đánh giá của khách (mới nhất trước), lọc theo số sao / chưa phản hồi / đang ẩn. Quản trị thấy tên thật cả khi khách gửi ẩn danh.</summary>
        [HttpGet]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetAll([FromQuery] int? soSao, [FromQuery] bool? chuaPhanHoi, [FromQuery] bool? an)
        {
            var reviews = await _reviewService.GetAllAsync(soSao, chuaPhanHoi, an);
            return Ok(reviews.Select(d => d.ToDto()));
        }

        /// <summary>Phản hồi công khai của khách sạn (ghi đè phản hồi cũ): body { "NoiDung": ... }.</summary>
        [HttpPut("{maDanhGia}/reply")]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Phản hồi đánh giá", "Đánh giá")]
        public async Task<ActionResult<ReviewDto>> Reply(string maDanhGia, ReviewReplyRequest request)
        {
            return (await _reviewService.ReplyAsync(maDanhGia, request.NoiDung, User.GetMaNhanVien())).ToDto();
        }

        /// <summary>Gỡ phản hồi.</summary>
        [HttpDelete("{maDanhGia}/reply")]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Gỡ phản hồi đánh giá", "Đánh giá")]
        public async Task<IActionResult> DeleteReply(string maDanhGia)
        {
            await _reviewService.DeleteReplyAsync(maDanhGia);
            return NoContent();
        }

        /// <summary>Ẩn / hiện đánh giá trên trang khách: body { "An": true | false }. Đánh giá ẩn vẫn tính vào điểm trung bình.</summary>
        [HttpPatch("{maDanhGia}")]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Ẩn/hiện đánh giá", "Đánh giá")]
        public async Task<ActionResult<ReviewDto>> SetVisibility(string maDanhGia, UpdateReviewVisibilityRequest request)
        {
            return (await _reviewService.SetHiddenAsync(maDanhGia, request.An!.Value)).ToDto();
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
