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
    [Route("api/promotions")]
    [Authorize]
    public class PromotionsController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ICustomerService _customerService;

        public PromotionsController(IAdminService adminService, ICustomerService customerService)
        {
            _adminService = adminService;
            _customerService = customerService;
        }

        /// <summary>Danh sách tất cả khuyến mãi.</summary>
        [HttpGet]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<IEnumerable<PromotionDto>>> GetAll()
        {
            var promotions = await _adminService.GetAllPromotionsAsync();
            return Ok(promotions.Select(k => k.ToDto()));
        }

        /// <summary>Kiểm tra mã khuyến mãi: 200 nếu đang có hiệu lực, 404 nếu không tồn tại, 400 nếu hết hạn.</summary>
        [HttpGet("{maKhuyenMai}")]
        public async Task<ActionResult<PromotionDto>> GetActive(string maKhuyenMai)
        {
            var promotion = await _customerService.GetActivePromotionAsync(maKhuyenMai);
            return promotion.ToDto();
        }

        /// <summary>Thêm khuyến mãi.</summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<PromotionDto>> Create(PromotionRequest request)
        {
            var promotion = request.ToEntity();
            await _adminService.AddPromotionAsync(promotion);
            return Created($"/api/promotions/{promotion.MaKhuyenMai}", promotion.ToDto());
        }
    }
}
