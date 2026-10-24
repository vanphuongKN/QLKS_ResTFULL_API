using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Enums;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/housekeeping")]
    [Authorize(Roles = AppRoles.HousekeepingTeam)]
    public class HousekeepingController : ControllerBase
    {
        private readonly IHousekeepingService _housekeepingService;

        public HousekeepingController(IHousekeepingService housekeepingService)
        {
            _housekeepingService = housekeepingService;
        }

        // Admin/Lễ tân được thao tác trên công việc của mọi nhân viên buồng phòng
        private bool LaQuanLy => User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Receptionist);

        /// <summary>Tổng quan buồng phòng: phòng cần dọn, số phòng theo trạng thái, công việc đang mở, nhân viên buồng phòng.</summary>
        [HttpGet]
        public async Task<ActionResult<HousekeepingDto>> Get()
        {
            var cleaningRooms = await _housekeepingService.GetRoomsNeedingCleaningAsync();
            var staff = await _housekeepingService.GetHousekeepingStaffAsync();
            var tasks = await _housekeepingService.GetOpenTasksAsync();

            return new HousekeepingDto
            {
                PhongCanDon = cleaningRooms.Select(p => p.ToDto()).ToList(),
                SoPhongDangDon = await _housekeepingService.GetCountByStatusAsync(RoomStatus.Cleaning),
                SoPhongTrong = await _housekeepingService.GetCountByStatusAsync(RoomStatus.Available),
                SoPhongDangO = await _housekeepingService.GetCountByStatusAsync(RoomStatus.Occupied),
                NhanVienBuongPhong = staff.Select(n => n.ToDto()).ToList(),
                CongViec = tasks.Select(t => t.ToDto()).ToList()
            };
        }

        /// <summary>Công việc dọn phòng chưa hoàn thành (chờ giao, đã giao, đang dọn, sự cố).</summary>
        [HttpGet("tasks")]
        public async Task<ActionResult<IEnumerable<HousekeepingTaskDto>>> GetOpenTasks()
        {
            var tasks = await _housekeepingService.GetOpenTasksAsync();
            return Ok(tasks.Select(t => t.ToDto()));
        }

        /// <summary>Việc của nhân viên buồng phòng đang đăng nhập (việc đang làm + đã xong hôm nay).</summary>
        [HttpGet("tasks/mine")]
        [Authorize(Roles = AppRoles.HousekeepingWorkers)]
        public async Task<ActionResult<IEnumerable<HousekeepingTaskDto>>> GetMyTasks()
        {
            var tasks = await _housekeepingService.GetTasksOfStaffAsync(User.GetMaNhanVien());
            return Ok(tasks.Select(t => t.ToDto()));
        }

        /// <summary>Nhân viên buồng phòng có thể giao việc.</summary>
        [HttpGet("staff")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        public async Task<ActionResult<IEnumerable<StaffDto>>> GetStaff()
        {
            var staff = await _housekeepingService.GetHousekeepingStaffAsync();
            return Ok(staff.Select(n => n.ToDto()));
        }

        /// <summary>Lễ tân giao (hoặc giao lại) công việc cho nhân viên buồng phòng.</summary>
        [HttpPost("tasks/{maPhanCong}/assign")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        public async Task<ActionResult<HousekeepingTaskDto>> Assign(string maPhanCong, AssignTaskRequest request)
        {
            var task = await _housekeepingService.AssignTaskAsync(maPhanCong, request.MaNhanVien);
            return task.ToDto();
        }

        /// <summary>Nhân viên bắt đầu dọn phòng.</summary>
        [HttpPost("tasks/{maPhanCong}/start")]
        public async Task<ActionResult<HousekeepingTaskDto>> Start(string maPhanCong)
        {
            var task = await _housekeepingService.StartTaskAsync(maPhanCong, User.GetMaNhanVien(), LaQuanLy);
            return task.ToDto();
        }

        /// <summary>Dọn xong: công việc hoàn thành, phòng chuyển sang Trống.</summary>
        [HttpPost("tasks/{maPhanCong}/complete")]
        public async Task<ActionResult<HousekeepingTaskDto>> Complete(string maPhanCong)
        {
            var task = await _housekeepingService.CompleteTaskAsync(maPhanCong, User.GetMaNhanVien(), LaQuanLy);
            return task.ToDto();
        }

        /// <summary>Báo sự cố khi dọn phòng: phòng chuyển sang Bảo trì.</summary>
        [HttpPost("tasks/{maPhanCong}/issue")]
        public async Task<ActionResult<HousekeepingTaskDto>> ReportIssue(string maPhanCong, ReportIssueRequest request)
        {
            var task = await _housekeepingService.ReportIssueAsync(maPhanCong, request.GhiChu, User.GetMaNhanVien(), LaQuanLy);
            return task.ToDto();
        }

        /// <summary>Lễ tân xác nhận phòng đã dọn xong (Đang dọn → Trống).</summary>
        [HttpPost("rooms/{maPhong}/clean")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        public async Task<IActionResult> MarkClean(string maPhong)
        {
            await _housekeepingService.MarkRoomAsCleanAsync(maPhong);
            return NoContent();
        }
    }
}
