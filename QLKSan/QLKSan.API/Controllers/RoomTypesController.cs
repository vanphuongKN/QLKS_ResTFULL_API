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
    [Route("api/room-types")]
    [Authorize]
    public class RoomTypesController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IRoomTypeService _roomTypeService;
        private readonly IRoomTypeImageService _imageService;

        public RoomTypesController(IAdminService adminService, IRoomTypeService roomTypeService, IRoomTypeImageService imageService)
        {
            _adminService = adminService;
            _roomTypeService = roomTypeService;
            _imageService = imageService;
        }

        /// <summary>Danh sách hạng phòng kèm điểm đánh giá (công khai: trang chủ, trang đặt phòng).</summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<RoomTypeDto>>> GetAll()
        {
            return Ok(await _roomTypeService.GetAllAsync());
        }

        /// <summary>Hạng phòng còn trống trong khoảng ngày [checkIn, checkOut), đủ sức chứa, kèm giá từng đêm (công khai).</summary>
        [HttpGet("available")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<RoomTypeAvailabilityDto>>> GetAvailable([FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut, [FromQuery] int? soNguoi)
        {
            return Ok(await _roomTypeService.GetAvailableAsync(checkIn, checkOut, soNguoi));
        }

        /// <summary>Chi tiết hạng phòng: bộ ảnh, tiện nghi, đánh giá gần đây (công khai).</summary>
        [HttpGet("{maLoai}")]
        [AllowAnonymous]
        public async Task<ActionResult<RoomTypeDetailDto>> GetById(string maLoai)
        {
            return await _roomTypeService.GetDetailAsync(maLoai);
        }

        /// <summary>Đánh giá của khách cho hạng phòng, mới nhất trước (công khai, ẩn danh tính khách).</summary>
        [HttpGet("{maLoai}/reviews")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews(string maLoai)
        {
            return Ok(await _roomTypeService.GetReviewsAsync(maLoai));
        }

        /// <summary>Thêm loại phòng (mã tự sinh LPxx).</summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Thêm loại phòng", "Loại phòng")]
        public async Task<ActionResult<RoomTypeDto>> Create(RoomTypeRequest request)
        {
            var maLoai = await _adminService.GenerateNextRoomTypeIdAsync();
            var roomType = request.ToEntity(maLoai);
            await _adminService.AddRoomTypeAsync(roomType);
            return CreatedAtAction(nameof(GetById), new { maLoai }, roomType.ToDto());
        }

        /// <summary>Cập nhật loại phòng.</summary>
        [HttpPut("{maLoai}")]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Sửa loại phòng", "Loại phòng")]
        public async Task<IActionResult> Update(string maLoai, RoomTypeRequest request)
        {
            await _adminService.UpdateRoomTypeAsync(request.ToEntity(maLoai));
            return NoContent();
        }

        /// <summary>
        /// Tải ảnh lên bộ ảnh của hạng phòng (multipart, trường "file"; JPG/PNG/WebP, tối đa 5 MB).
        /// Ảnh đầu tiên của bộ ảnh là ảnh đại diện.
        /// </summary>
        [HttpPost("{maLoai}/images")]
        [Authorize(Roles = AppRoles.Admin)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(RoomTypeImageService.DungLuongToiDa + 64 * 1024)]
        [Audit("Tải ảnh loại phòng", "Loại phòng")]
        public async Task<ActionResult<RoomTypeImageDto>> UploadImage(string maLoai, IFormFile file)
        {
            if (file == null) throw new BusinessException("Vui lòng chọn ảnh cần tải lên.");
            using var stream = file.OpenReadStream();
            var image = await _imageService.AddAsync(maLoai, stream, file.FileName);
            return CreatedAtAction(nameof(GetById), new { maLoai }, image.ToDto());
        }

        /// <summary>Xoá một ảnh khỏi bộ ảnh của hạng phòng.</summary>
        [HttpDelete("{maLoai}/images/{maHinhAnh}")]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Xoá ảnh loại phòng", "Loại phòng")]
        public async Task<IActionResult> DeleteImage(string maLoai, string maHinhAnh)
        {
            await _imageService.DeleteAsync(maLoai, maHinhAnh);
            return NoContent();
        }

        /// <summary>Xoá loại phòng (không xoá được nếu còn phòng thuộc loại này).</summary>
        [HttpDelete("{maLoai}")]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Xoá loại phòng", "Loại phòng")]
        public async Task<IActionResult> Delete(string maLoai)
        {
            await _adminService.DeleteRoomTypeAsync(maLoai);
            return NoContent();
        }
    }
}
