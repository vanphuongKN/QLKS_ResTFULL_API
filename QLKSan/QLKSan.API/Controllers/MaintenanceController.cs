using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/maintenance")]
    [Authorize(Roles = AppRoles.HousekeepingTeam)]
    public class MaintenanceController : ControllerBase
    {
        private readonly IMaintenanceService _maintenanceService;

        public MaintenanceController(IMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }

        /// <summary>Phiếu kỹ thuật. loc = mo (mới / đang sửa, khẩn trước), xong (xong / huỷ 7 ngày), trống: 30 ngày.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MaintenanceTicketDto>>> GetAll([FromQuery] string loc)
        {
            return Ok(await _maintenanceService.ListAsync(loc));
        }

        /// <summary>Mở phiếu kỹ thuật cho một phòng; KhoaPhong = chuyển phòng sang Bảo trì tới khi sửa xong (bỏ qua nếu phòng có khách).</summary>
        [HttpPost]
        [Audit("Mở phiếu kỹ thuật", "Kỹ thuật")]
        public async Task<ActionResult<MaintenanceTicketDto>> Create(MaintenanceCreateRequest request)
        {
            var created = await _maintenanceService.CreateAsync(request, User.GetMaNhanVien());
            return Created($"/api/maintenance/{created.MaPhieu}", created);
        }

        /// <summary>
        /// Đang sửa / đã xong / huỷ (huỷ phải có lý do). Phiếu khoá phòng cuối cùng kết thúc → phòng chuyển Đang dọn và tạo việc dọn;
        /// yêu cầu khách liên kết được cập nhật theo.
        /// </summary>
        [HttpPatch("{maPhieu:int}")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Cập nhật phiếu kỹ thuật", "Kỹ thuật")]
        public async Task<ActionResult<MaintenanceTicketDto>> UpdateStatus(int maPhieu, MaintenanceUpdateRequest request)
        {
            return await _maintenanceService.UpdateStatusAsync(maPhieu, request.TrangThai!.Value, request.KetQua, User.GetMaNhanVien());
        }
    }
}
