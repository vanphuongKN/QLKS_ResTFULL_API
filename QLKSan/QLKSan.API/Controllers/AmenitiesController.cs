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
    [Route("api/amenities")]
    [Authorize(Roles = AppRoles.Admin)]
    public class AmenitiesController : ControllerBase
    {
        private readonly IAmenityService _amenityService;

        public AmenitiesController(IAmenityService amenityService)
        {
            _amenityService = amenityService;
        }

        /// <summary>Danh mục tiện nghi kèm các phòng đang có.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AmenityDto>>> GetAll()
        {
            return Ok((await _amenityService.GetAllAsync()).Select(t => t.ToDto()));
        }

        /// <summary>Thêm tiện nghi (mã tự sinh TIxx).</summary>
        [HttpPost]
        [Audit("Thêm tiện nghi", "Tiện nghi")]
        public async Task<ActionResult<AmenityDto>> Create(AmenityRequest request)
        {
            var amenity = await _amenityService.CreateAsync(request);
            return Created("/api/amenities", amenity.ToDto());
        }

        /// <summary>Sửa tên / biểu tượng tiện nghi.</summary>
        [HttpPut("{maTienIch}")]
        [Audit("Sửa tiện nghi", "Tiện nghi")]
        public async Task<ActionResult<AmenityDto>> Update(string maTienIch, AmenityRequest request)
        {
            return (await _amenityService.UpdateAsync(maTienIch, request)).ToDto();
        }

        /// <summary>Xoá tiện nghi (gỡ khỏi mọi phòng đang có).</summary>
        [HttpDelete("{maTienIch}")]
        [Audit("Xoá tiện nghi", "Tiện nghi")]
        public async Task<IActionResult> Delete(string maTienIch)
        {
            await _amenityService.DeleteAsync(maTienIch);
            return NoContent();
        }
    }
}
