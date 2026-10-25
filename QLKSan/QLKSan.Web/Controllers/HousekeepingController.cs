using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Infrastructure;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    // Lễ tân/Admin: tổng quan + giao việc. Nhân viên buồng phòng: "Việc của tôi"
    [Authorize(Roles = WebRoles.Admin + "," + WebRoles.Receptionist + "," + WebRoles.Housekeeping)]
    public class HousekeepingController : Controller
    {
        private readonly IHousekeepingApiClient _housekeepingApi;
        private readonly IMaintenanceApiClient _maintenanceApi;
        private readonly IServiceApiClient _serviceApi;

        public HousekeepingController(IHousekeepingApiClient housekeepingApi, IMaintenanceApiClient maintenanceApi, IServiceApiClient serviceApi)
        {
            _housekeepingApi = housekeepingApi;
            _maintenanceApi = maintenanceApi;
            _serviceApi = serviceApi;
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
            ViewBag.Board = summary.Phong;
            ViewBag.KhachDenPhongChuaSan = summary.KhachDenPhongChuaSan;
            return View();
        }

        // Kiểm tra phòng: đạt (mọi mục được tích) hoặc chưa đạt (mục bỏ tích + ghi chú) → trả lại nhân viên dọn lại
        [HttpPost]
        [Authorize(Roles = WebRoles.RoomInspectors)]
        public async Task<IActionResult> Inspect(string maPhanCong, List<string> danhMuc, List<string> daDat, string ghiChu)
        {
            var chuaDat = (danhMuc ?? new List<string>()).Except(daDat ?? new List<string>()).ToList();
            var dat = chuaDat.Count == 0;
            try
            {
                await _housekeepingApi.InspectAsync(maPhanCong, new InspectTaskRequest { Dat = dat, MucChuaDat = chuaDat, GhiChu = ghiChu });
                TempData["SuccessMessage"] = dat ? "Kiểm tra đạt — phòng sẵn sàng đón khách." : $"Kiểm tra chưa đạt ({chuaDat.Count} mục) — đã trả lại để dọn lại.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không lưu được kết quả kiểm tra. " + ex.UserMessage;
            }
            return RedirectToAction(nameof(Tasks));
        }

        // Tạo ngay việc dọn hằng ngày / chỉnh trang buổi tối còn thiếu
        [HttpPost]
        [Authorize(Roles = WebRoles.FrontDesk)]
        public async Task<IActionResult> GenerateDaily()
        {
            try
            {
                var r = await _housekeepingApi.GenerateDailyAsync();
                TempData["SuccessMessage"] = r.DonHangNgay + r.ChinhTrangToi == 0
                    ? "Việc trong ngày đã đủ, không cần tạo thêm."
                    : $"Đã tạo {r.DonHangNgay} việc dọn hằng ngày, {r.ChinhTrangToi} việc chỉnh trang buổi tối.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không tạo được việc. " + ex.UserMessage;
            }
            return RedirectToAction(nameof(Tasks));
        }

        // Nhân viên gặp biển "Không làm phiền"
        [HttpPost]
        [Authorize(Roles = WebRoles.HousekeepingWorkers)]
        public Task<IActionResult> SkipDnd(string id)
            => RunAsync(_housekeepingApi.SkipDndAsync(id), "Đã ghi khách không làm phiền, bỏ qua việc hôm nay.", "Không thể cập nhật. ", "MyTasks");

        // ===== Ghi minibar =====

        [HttpGet]
        [Authorize(Roles = WebRoles.HousekeepingTeam)]
        public async Task<IActionResult> Minibar(string maPhong)
        {
            var summary = await _housekeepingApi.GetAsync();
            ViewBag.Rooms = summary.Phong.Where(r => r.TrangThai == Enums.RoomStatus.Occupied).ToList();
            ViewBag.Items = (await _serviceApi.GetAllAsync()).Where(s => s.LaMinibar && s.TrangThai == Enums.ServiceStatus.Available).ToList();
            ViewBag.MaPhong = maPhong;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = WebRoles.HousekeepingTeam)]
        public async Task<IActionResult> Minibar(string maPhong, List<string> maDichVu, List<int> soLuong)
        {
            var items = new List<ServiceOrderItem>();
            for (var i = 0; i < (maDichVu?.Count ?? 0); i++)
                if (soLuong != null && i < soLuong.Count && soLuong[i] > 0)
                    items.Add(new ServiceOrderItem { MaDichVu = maDichVu[i], SoLuong = soLuong[i] });
            try
            {
                var tong = await _housekeepingApi.PostMinibarAsync(maPhong, new MinibarRequest { Items = items });
                TempData["SuccessMessage"] = $"Đã ghi minibar {tong:N0}đ vào bảng kê của khách.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không ghi được minibar. " + ex.UserMessage;
            }
            return RedirectToAction(nameof(Minibar));
        }

        // ===== Phiếu kỹ thuật =====

        [HttpGet]
        [Authorize(Roles = WebRoles.HousekeepingTeam)]
        public async Task<IActionResult> Maintenance(string loc = "mo")
        {
            ViewBag.Loc = loc;
            ViewBag.Rooms = (await _housekeepingApi.GetAsync()).Phong;
            return View(await _maintenanceApi.GetAllAsync(loc == "tatca" ? null : loc));
        }

        [HttpPost]
        [Authorize(Roles = WebRoles.HousekeepingTeam)]
        public async Task<IActionResult> CreateTicket(MaintenanceCreateRequest model)
        {
            try
            {
                var p = await _maintenanceApi.CreateAsync(model);
                TempData["SuccessMessage"] = $"Đã mở phiếu kỹ thuật #{p.MaPhieu} cho phòng {p.SoPhong}" + (p.KhoaPhong ? " (phòng chuyển Bảo trì)." : ".");
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không mở được phiếu. " + ex.UserMessage;
            }
            return RedirectToAction(nameof(Maintenance));
        }

        [HttpPost]
        [Authorize(Roles = WebRoles.FrontDesk)]
        public async Task<IActionResult> UpdateTicket(int id, Enums.GuestRequestStatus trangThai, string ketQua, string loc = "mo")
        {
            try
            {
                await _maintenanceApi.UpdateStatusAsync(id, trangThai, ketQua);
                TempData["SuccessMessage"] = $"Đã cập nhật phiếu kỹ thuật #{id}.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không cập nhật được phiếu. " + ex.UserMessage;
            }
            return RedirectToAction(nameof(Maintenance), new { loc });
        }

        // Giao nhiều việc đã chọn (vd: cả một tầng) cho một nhân viên
        [HttpPost]
        [Authorize(Roles = WebRoles.FrontDesk)]
        public async Task<IActionResult> AssignBulk(List<string> maPhanCongs, string maNhanVien)
        {
            try
            {
                var n = await _housekeepingApi.AssignBulkAsync(maPhanCongs ?? new List<string>(), maNhanVien);
                TempData["SuccessMessage"] = $"Đã giao {n} việc.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không thể giao việc. " + ex.UserMessage;
            }
            return RedirectToAction(nameof(Tasks));
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
            => RunAsync(_housekeepingApi.CompleteAsync(id), "Đã báo dọn xong, chờ kiểm tra phòng.", "Không thể hoàn thành. ", "MyTasks");

        [HttpPost]
        [Authorize(Roles = WebRoles.HousekeepingWorkers)]
        public Task<IActionResult> ReportIssue(string id, string ghiChu)
            => RunAsync(_housekeepingApi.ReportIssueAsync(id, ghiChu), "Đã báo sự cố, mở phiếu kỹ thuật và chuyển phòng sang bảo trì.", "Không thể báo sự cố. ", "MyTasks");
    }
}
