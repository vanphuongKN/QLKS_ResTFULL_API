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
    [Route("api/pricing-rules")]
    [Authorize(Roles = AppRoles.Admin)]
    public class PricingRulesController : ControllerBase
    {
        private readonly IPricingService _pricing;

        public PricingRulesController(IPricingService pricing)
        {
            _pricing = pricing;
        }

        /// <summary>Danh sách quy tắc giá (mùa, cuối tuần, lễ).</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PricingRuleDto>>> GetAll()
        {
            var rules = await _pricing.GetRulesAsync();
            return Ok(rules.Select(r => r.ToDto()));
        }

        /// <summary>Thêm quy tắc giá: điều chỉnh % theo khoảng ngày, đêm trong tuần, loại phòng; ưu tiên cao thắng.</summary>
        [HttpPost]
        public async Task<ActionResult<PricingRuleDto>> Create(PricingRuleRequest request)
        {
            var rule = await _pricing.AddRuleAsync(request);
            return Created($"/api/pricing-rules/{rule.MaQuyTac}", rule.ToDto());
        }

        /// <summary>Bật hoặc tắt quy tắc giá: body { "KichHoat": true | false } (gọi lặp lại cho cùng kết quả).</summary>
        [HttpPatch("{maQuyTac}")]
        public async Task<ActionResult<PricingRuleDto>> UpdateStatus(string maQuyTac, UpdatePricingRuleStatusRequest request)
        {
            var rule = await _pricing.SetRuleActiveAsync(maQuyTac, request.KichHoat!.Value);
            return rule.ToDto();
        }

        /// <summary>Xoá quy tắc giá (hoá đơn đã lập không bị ảnh hưởng).</summary>
        [HttpDelete("{maQuyTac}")]
        public async Task<IActionResult> Delete(string maQuyTac)
        {
            await _pricing.DeleteRuleAsync(maQuyTac);
            return NoContent();
        }
    }
}
