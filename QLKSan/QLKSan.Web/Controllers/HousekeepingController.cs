using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Infrastructure;

namespace QLKSan.Controllers
{
    // Lễ tân/Admin: tổng quan + giao việc. Nhân viên buồng phòng: "Việc của tôi"
    [Authorize(Roles = WebRoles.Admin + "," + WebRoles.Receptionist + "," + WebRoles.Housekeeping)]
    public class HousekeepingController : Controller
    {
        private readonly IHousekeepingApiClient _housekeepingApi;

        public HousekeepingController(IHousekeepingApiClient housekeepingApi)
        {
            _housekeepingApi = housekeepingApi;
        }

        // Gọi API rồi ghi thông báo vào TempData
        private async Task<IActionResult> RunAsync(Task action, string successMessage, string errorPrefix, string redirectAction)
        {
            try
            {
                await action;
                TempData["SuccessMessage"] = successMessage;
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = errorPrefix + ex.UserMessage;
            }
            return RedirectToAction(redirectAction);
        }

        [HttpGet]
        [Authorize(Roles = WebRoles.FrontDesk)]
        public async Task<IActionResult> Tasks()
        {
            var summary = await _housekeepingApi.GetAsync();
            ViewBag.CleaningRooms = summary.PhongCanDon;
            ViewBag.CountCleaning = summary.SoPhongDangDon;
            ViewBag.CountAvailable = summary.SoPhongTrong;
            ViewBag.CountOccupied = summary.SoPhongDangO;
            ViewBag.HousekeepingStaff = summary.NhanVienBuongPhong;
            ViewBag.Tasks = summary.CongViec;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = WebRoles.FrontDesk)]
        public Task<IActionResult> MarkClean(string maPhong)
            => RunAsync(_housekeepingApi.MarkCleanAsync(maPhong), "Phòng đã được chuyển sang trạng thái Trống (Sẵn sàng)!",
                "Không thể cập nhật trạng thái phòng. ", "Tasks");

        // Lễ tân giao (hoặc giao lại) việc dọn phòng
        [HttpPost]
        [Authorize(Roles = WebRoles.FrontDesk)]
        public Task<IActionResult> Assign(string maPhanCong, string maNhanVien)
            => RunAsync(_housekeepingApi.AssignAsync(maPhanCong, maNhanVien), "Đã giao việc dọn phòng.", "Không thể giao việc. ", "Tasks");

        // ===== Nhân viên buồng phòng =====

        [HttpGet]
        [Authorize(Roles = WebRoles.HousekeepingWorkers)]
        public async Task<IActionResult> MyTasks()
        {
            var tasks = await _housekeepingApi.GetMyTasksAsync();
            return View(tasks);
        }

        [HttpPost]
        [Authorize(Roles = WebRoles.HousekeepingWorkers)]
        public Task<IActionResult> Start(string id)
            => RunAsync(_housekeepingApi.StartAsync(id), "Đã bắt đầu dọn phòng.", "Không thể bắt đầu. ", "MyTasks");

        [HttpPost]
        [Authorize(Roles = WebRoles.HousekeepingWorkers)]
        public Task<IActionResult> Complete(string id)
            => RunAsync(_housekeepingApi.CompleteAsync(id), "Đã dọn xong, phòng sẵn sàng đón khách.", "Không thể hoàn thành. ", "MyTasks");

        [HttpPost]
        [Authorize(Roles = WebRoles.HousekeepingWorkers)]
        public Task<IActionResult> ReportIssue(string id, string ghiChu)
            => RunAsync(_housekeepingApi.ReportIssueAsync(id, ghiChu), "Đã báo sự cố, phòng chuyển sang bảo trì.", "Không thể báo sự cố. ", "MyTasks");
    }
}
