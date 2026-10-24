using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/promotions")]
    [Authorize]
    public class PromotionsController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public PromotionsController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        /// <summary>Danh sách khuyến mãi kèm điều kiện áp dụng và số lượt đã dùng.</summary>
        [HttpGet]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<IEnumerable<PromotionDto>>> GetAll()
        {
            return Ok(await _promotionService.GetAllAsync());
        }

        /// <summary>Kiểm tra mã khuyến mãi: 200 nếu đang áp dụng, 404 nếu không tồn tại, 400 nếu hết hạn / đã tắt / hết lượt.</summary>
        [HttpGet("{maKhuyenMai}")]
        public async Task<ActionResult<PromotionDto>> GetActive(string maKhuyenMai)
        {
            return await _promotionService.GetActiveAsync(maKhuyenMai);
        }

        /// <summary>Thêm khuyến mãi (mã viết hoa, không trùng).</summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Thêm khuyến mãi", "Khuyến mãi")]
        public async Task<ActionResult<PromotionDto>> Create(PromotionRequest request)
        {
            var promotion = await _promotionService.CreateAsync(request);
            return Created($"/api/promotions/{promotion.MaKhuyenMai}", promotion);
        }

        /// <summary>Sửa khuyến mãi (mã trong đường dẫn; mã trong body bị bỏ qua).</summary>
        [HttpPut("{maKhuyenMai}")]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Sửa khuyến mãi", "Khuyến mãi")]
        public async Task<ActionResult<PromotionDto>> Update(string maKhuyenMai, PromotionRequest request)
        {
            return await _promotionService.UpdateAsync(maKhuyenMai, request);
        }

        /// <summary>Bật hoặc tắt mã: body { "KichHoat": true | false }.</summary>
        [HttpPatch("{maKhuyenMai}")]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Bật/tắt khuyến mãi", "Khuyến mãi")]
        public async Task<ActionResult<PromotionDto>> SetStatus(string maKhuyenMai, UpdatePromotionStatusRequest request)
        {
            return await _promotionService.SetActiveAsync(maKhuyenMai, request.KichHoat!.Value);
        }

        /// <summary>Xoá mã chưa từng dùng (đã dùng → 409, nên tắt thay vì xoá).</summary>
        [HttpDelete("{maKhuyenMai}")]
        [Authorize(Roles = AppRoles.Admin)]
        [Audit("Xoá khuyến mãi", "Khuyến mãi")]
        public async Task<IActionResult> Delete(string maKhuyenMai)
        {
            await _promotionService.DeleteAsync(maKhuyenMai);
            return NoContent();
        }
    }
}
