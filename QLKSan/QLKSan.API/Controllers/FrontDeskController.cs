using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Models.DTOs;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/front-desk")]
    [Authorize(Roles = AppRoles.FrontDesk)]
    public class FrontDeskController : ControllerBase
    {
        private readonly IFrontDeskService _frontDeskService;

        public FrontDeskController(IFrontDeskService frontDeskService)
        {
            _frontDeskService = frontDeskService;
        }

        /// <summary>
        /// Màn hình đầu ca: khách đến / khách đi (kể cả quá hạn trả) / đang lưu trú hôm nay, phiếu sắp đến,
        /// tình trạng phòng và việc buồng phòng chưa xong. Mỗi phiếu kèm VIP, sở thích, hạn chế, yêu cầu riêng.
        /// </summary>
        [HttpGet("today")]
        public async Task<ActionResult<FrontDeskTodayDto>> Today()
        {
            return await _frontDeskService.GetTodayAsync();
        }
    }
}
