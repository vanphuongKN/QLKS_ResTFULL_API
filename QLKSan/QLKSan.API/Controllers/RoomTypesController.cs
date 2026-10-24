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

        public RoomTypesController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>Danh sách loại phòng (công khai: trang chủ giới thiệu hạng phòng và giá niêm yết).</summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<RoomTypeDto>>> GetAll()
        {
            var roomTypes = await _adminService.GetAllRoomTypesAsync();
            return Ok(roomTypes.Select(l => l.ToDto()));
        }

        /// <summary>Chi tiết loại phòng.</summary>
        [HttpGet("{maLoai}")]
        public async Task<ActionResult<RoomTypeDto>> GetById(string maLoai)
        {
            var roomType = await _adminService.GetRoomTypeByIdAsync(maLoai);
            if (roomType == null) return NotFound();
            return roomType.ToDto();
        }

        /// <summary>Thêm loại phòng (mã tự sinh LPxx).</summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
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
        public async Task<IActionResult> Update(string maLoai, RoomTypeRequest request)
        {
            await _adminService.UpdateRoomTypeAsync(request.ToEntity(maLoai));
            return NoContent();
        }

        /// <summary>Xoá loại phòng (không xoá được nếu còn phòng thuộc loại này).</summary>
        [HttpDelete("{maLoai}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Delete(string maLoai)
        {
            await _adminService.DeleteRoomTypeAsync(maLoai);
            return NoContent();
        }
    }
}
