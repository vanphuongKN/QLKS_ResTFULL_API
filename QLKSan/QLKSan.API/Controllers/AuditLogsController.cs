using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Models.DTOs;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/audit-logs")]
    [Authorize(Roles = AppRoles.Admin)]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        /// <summary>Nhật ký thao tác (mới nhất trước), lọc theo khoảng ngày, đối tượng, người thực hiện, từ khoá; phân trang.</summary>
        [HttpGet]
        public async Task<ActionResult<AuditLogPageDto>> Search([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay, [FromQuery] string doiTuong,
            [FromQuery] string nguoiDung, [FromQuery] string tuKhoa, [FromQuery] int trang = 1, [FromQuery] int soDong = 50)
        {
            return await _auditLogService.SearchAsync(tuNgay, denNgay, doiTuong, nguoiDung, tuKhoa, trang, soDong);
        }
    }
}
