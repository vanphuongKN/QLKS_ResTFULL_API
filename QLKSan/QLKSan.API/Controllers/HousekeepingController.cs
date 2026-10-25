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
            var board = await _housekeepingService.GetBoardAsync();

            return new HousekeepingDto
            {
                PhongCanDon = cleaningRooms.Select(p => p.ToDto()).ToList(),
                SoPhongDangDon = await _housekeepingService.GetCountByStatusAsync(RoomStatus.Cleaning),
                SoPhongTrong = await _housekeepingService.GetCountByStatusAsync(RoomStatus.Available),
                SoPhongDangO = await _housekeepingService.GetCountByStatusAsync(RoomStatus.Occupied),
                NhanVienBuongPhong = staff.Select(n => n.ToDto()).ToList(),
                CongViec = await _housekeepingService.PrioritizeAsync(await _housekeepingService.GetOpenTasksAsync()),
                Phong = board,
                KhachDenPhongChuaSan = board.Count(r => r.KhachDen != null && r.TrangThai != RoomStatus.Available)
            };
        }

        /// <summary>Công việc dọn phòng chưa hoàn thành (chờ giao, đã giao, đang dọn, sự cố).</summary>
        [HttpGet("tasks")]
        public async Task<ActionResult<IEnumerable<HousekeepingTaskDto>>> GetOpenTasks()
        {
            var tasks = await _housekeepingService.GetOpenTasksAsync();
            return Ok(await _housekeepingService.PrioritizeAsync(tasks));
        }

        /// <summary>Việc của nhân viên buồng phòng đang đăng nhập (việc đang làm + đã xong hôm nay).</summary>
        [HttpGet("tasks/mine")]
        [Authorize(Roles = AppRoles.HousekeepingWorkers)]
        public async Task<ActionResult<IEnumerable<HousekeepingTaskDto>>> GetMyTasks()
        {
            var tasks = await _housekeepingService.GetTasksOfStaffAsync(User.GetMaNhanVien());
            return Ok(await _housekeepingService.PrioritizeAsync(tasks));
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
        [Audit("Giao việc buồng phòng", "Buồng phòng")]
        public async Task<ActionResult<HousekeepingTaskDto>> Assign(string maPhanCong, AssignTaskRequest request)
        {
            var task = await _housekeepingService.AssignTaskAsync(maPhanCong, request.MaNhanVien);
            return task.ToDto();
        }

        /// <summary>Giao nhiều việc chưa bắt đầu cho một nhân viên (vd: giao cả tầng); trả về số việc đã giao.</summary>
        [HttpPost("tasks/assign-bulk")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Giao việc buồng phòng theo nhóm", "Buồng phòng")]
        public async Task<ActionResult<int>> AssignBulk(BulkAssignTaskRequest request)
        {
            return await _housekeepingService.AssignManyAsync(request.MaPhanCongs, request.MaNhanVien);
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
        [Audit("Dọn phòng xong", "Buồng phòng")]
        public async Task<ActionResult<HousekeepingTaskDto>> Complete(string maPhanCong)
        {
            var task = await _housekeepingService.CompleteTaskAsync(maPhanCong, User.GetMaNhanVien(), LaQuanLy, User.IsInAnyRole(AppRoles.RoomInspectors));
            return task.ToDto();
        }

        /// <summary>
        /// Kiểm tra phòng sau khi dọn (theo danh mục của hạng phòng). Đạt: hoàn thành, phòng sẵn sàng đón khách.
        /// Chưa đạt: trả lại nhân viên dọn lại, ghi các mục chưa đạt.
        /// </summary>
        [HttpPost("tasks/{maPhanCong}/inspect")]
        [Authorize(Roles = AppRoles.RoomInspectors)]
        [Audit("Kiểm tra phòng", "Buồng phòng")]
        public async Task<ActionResult<HousekeepingTaskDto>> Inspect(string maPhanCong, InspectTaskRequest request)
        {
            var task = await _housekeepingService.InspectAsync(maPhanCong, request.Dat, request.MucChuaDat, request.GhiChu, User.GetMaNhanVien());
            return task.ToDto();
        }

        /// <summary>Tạo ngay việc dọn hằng ngày / chỉnh trang buổi tối còn thiếu trong ngày (tác vụ nền cũng tự chạy định kỳ).</summary>
        [HttpPost("daily-tasks")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        public async Task<ActionResult<DailyTasksResultDto>> GenerateDaily()
        {
            var (don, chinhTrang) = await _housekeepingService.GenerateDailyTasksAsync(DateTime.Now);
            return new DailyTasksResultDto { DonHangNgay = don, ChinhTrangToi = chinhTrang };
        }

        /// <summary>
        /// Dặn dò buồng phòng cho khách đang ở: không làm phiền hôm nay, giờ dọn mong muốn, từ chối dọn hằng ngày (cả kỳ nghỉ).
        /// Việc theo ngày chưa bắt đầu được bỏ qua tương ứng.
        /// </summary>
        [HttpPut("stays/{maDatPhong}/preferences")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Dặn dò buồng phòng", "Phiếu đặt phòng")]
        public async Task<ActionResult<HousekeepingPreferencesDto>> SetPreferences(string maDatPhong, HousekeepingPreferencesRequest request)
        {
            return await _housekeepingService.SetPreferencesAsync(maDatPhong, request.KhongLamPhienHomNay, request.GioDonMongMuon, request.TuChoiDonHangNgay,
                $"Nhân viên {User.Identity?.Name}");
        }

        /// <summary>Nhân viên gặp biển "Không làm phiền": bỏ qua việc dọn hằng ngày / chỉnh trang, ghi DND cho phiếu hôm nay.</summary>
        [HttpPost("tasks/{maPhanCong}/dnd")]
        [Audit("Khách không làm phiền", "Buồng phòng")]
        public async Task<ActionResult<HousekeepingTaskDto>> SkipDnd(string maPhanCong)
        {
            var task = await _housekeepingService.SkipForDndAsync(maPhanCong, User.GetMaNhanVien(), LaQuanLy);
            return task.ToDto();
        }

        /// <summary>Ghi đồ minibar khách đã dùng vào bảng kê của khách đang ở tại phòng; trả về tổng tiền vừa ghi.</summary>
        [HttpPost("rooms/{maPhong}/minibar")]
        [Audit("Ghi minibar", "Phòng")]
        public async Task<ActionResult<decimal>> PostMinibar(string maPhong, MinibarRequest request)
        {
            return await _housekeepingService.PostMinibarAsync(maPhong, request.Items, $"Nhân viên {User.Identity?.Name}");
        }

        /// <summary>Báo sự cố khi dọn phòng: phòng chuyển sang Bảo trì.</summary>
        [HttpPost("tasks/{maPhanCong}/issue")]
        [Audit("Báo sự cố phòng", "Buồng phòng")]
        public async Task<ActionResult<HousekeepingTaskDto>> ReportIssue(string maPhanCong, ReportIssueRequest request)
        {
            var task = await _housekeepingService.ReportIssueAsync(maPhanCong, request.GhiChu, User.GetMaNhanVien(), LaQuanLy);
            return task.ToDto();
        }

        /// <summary>Lễ tân xác nhận phòng đã dọn xong (Đang dọn → Trống).</summary>
        [HttpPost("rooms/{maPhong}/clean")]
        [Authorize(Roles = AppRoles.FrontDesk)]
        [Audit("Xác nhận phòng sạch", "Phòng")]
        public async Task<IActionResult> MarkClean(string maPhong)
        {
            await _housekeepingService.MarkRoomAsCleanAsync(maPhong);
            return NoContent();
        }
    }
}
