using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Enums;
using QLKSan.Extensions;
using QLKSan.Infrastructure;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [Authorize(Roles = WebRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly IDashboardApiClient _dashboardApi;
        private readonly IRoomTypeApiClient _roomTypeApi;
        private readonly IRoomApiClient _roomApi;
        private readonly IServiceApiClient _serviceApi;
        private readonly IStaffApiClient _staffApi;
        private readonly IPromotionApiClient _promotionApi;
        private readonly IReviewApiClient _reviewApi;
        private readonly IPricingRuleApiClient _pricingApi;

        public AdminController(IDashboardApiClient dashboardApi, IRoomTypeApiClient roomTypeApi, IRoomApiClient roomApi,
            IServiceApiClient serviceApi, IStaffApiClient staffApi, IPromotionApiClient promotionApi, IReviewApiClient reviewApi,
            IPricingRuleApiClient pricingApi)
        {
            _dashboardApi = dashboardApi;
            _roomTypeApi = roomTypeApi;
            _roomApi = roomApi;
            _serviceApi = serviceApi;
            _staffApi = staffApi;
            _promotionApi = promotionApi;
            _reviewApi = reviewApi;
            _pricingApi = pricingApi;
        }

        // Gọi API và ghi kết quả vào TempData (thông báo lỗi lấy từ API), rồi quay về trang danh sách
        private async Task<IActionResult> RunAsync(Func<Task> action, string successMessage, string redirectAction)
        {
            try
            {
                await action();
                TempData["SuccessMessage"] = successMessage;
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.UserMessage;
            }
            return RedirectToAction(redirectAction);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dashboard = await _dashboardApi.GetAsync(DateTime.Now.Year);
            ViewBag.Revenue = dashboard.TongDoanhThu;
            ViewBag.WeeklyRevenue = dashboard.DoanhThuTuan;
            ViewBag.Bookings = dashboard.TongDatPhong;
            ViewBag.Customers = dashboard.TongKhachHang;
            ViewBag.Promotions = dashboard.KhuyenMais;
            ViewBag.YearlyRevenue = dashboard.DoanhThuTheoThang;
            return View();
        }

        // ===== ROOM TYPES =====
        [HttpGet]
        public async Task<IActionResult> RoomTypes()
        {
            ViewBag.RoomTypes = await _roomTypeApi.GetAllAsync();
            return View();
        }

        [HttpPost]
        public Task<IActionResult> AddRoomType(string tenLoai, int soNguoiToiDa, decimal giaThueNgay, decimal giaThueGio, string moTa)
        {
            var request = new RoomTypeRequest { TenLoai = tenLoai, SoNguoiToiDa = soNguoiToiDa, GiaThueNgay = giaThueNgay, GiaThueGio = giaThueGio, MoTa = moTa };
            return RunAsync(() => _roomTypeApi.CreateAsync(request), "Thêm loại phòng thành công!", "RoomTypes");
        }

        [HttpPost]
        public Task<IActionResult> EditRoomType(string maLoai, string tenLoai, int soNguoiToiDa, decimal giaThueNgay, decimal giaThueGio, string moTa)
        {
            var request = new RoomTypeRequest { TenLoai = tenLoai, SoNguoiToiDa = soNguoiToiDa, GiaThueNgay = giaThueNgay, GiaThueGio = giaThueGio, MoTa = moTa };
            return RunAsync(() => _roomTypeApi.UpdateAsync(maLoai, request), "Cập nhật loại phòng thành công!", "RoomTypes");
        }

        [HttpPost]
        public Task<IActionResult> DeleteRoomType(string maLoai)
            => RunAsync(() => _roomTypeApi.DeleteAsync(maLoai), "Xóa loại phòng thành công!", "RoomTypes");

        // ===== ROOMS =====
        [HttpGet]
        public async Task<IActionResult> Rooms()
        {
            ViewBag.Rooms = await _roomApi.GetAllAsync();
            ViewBag.RoomTypes = await _roomTypeApi.GetAllAsync();
            return View();
        }

        [HttpPost]
        public Task<IActionResult> AddRoom(string soPhong, string maLoai)
            => RunAsync(() => _roomApi.CreateAsync(new RoomRequest { SoPhong = soPhong, MaLoai = maLoai }), "Thêm phòng thành công!", "Rooms");

        [HttpPost]
        public Task<IActionResult> DeleteRoom(string maPhong)
            => RunAsync(() => _roomApi.DeleteAsync(maPhong), "Xóa phòng thành công!", "Rooms");

        [HttpPost]
        public Task<IActionResult> EditRoom(string maPhong, string soPhong, string maLoai)
            => RunAsync(() => _roomApi.UpdateAsync(maPhong, new RoomRequest { SoPhong = soPhong, MaLoai = maLoai }), "Cập nhật phòng thành công!", "Rooms");

        // ===== SERVICES =====
        [HttpGet]
        public async Task<IActionResult> Services()
        {
            ViewBag.Services = await _serviceApi.GetAllAsync();
            return View();
        }

        [HttpPost]
        public Task<IActionResult> AddService(string tenDichVu, decimal giaDichVu, string donViTinh)
            => RunAsync(() => _serviceApi.CreateAsync(new ServiceRequest { TenDichVu = tenDichVu, GiaDichVu = giaDichVu, DonViTinh = donViTinh }),
                "Thêm dịch vụ thành công!", "Services");

        [HttpPost]
        public Task<IActionResult> DeleteService(string maDichVu)
            => RunAsync(() => _serviceApi.DeleteAsync(maDichVu), "Xóa dịch vụ thành công!", "Services");

        [HttpPost]
        public Task<IActionResult> SetServiceStatus(string maDichVu, ServiceStatus trangThai)
            => RunAsync(() => _serviceApi.SetStatusAsync(maDichVu, trangThai),
                $"Đã chuyển dịch vụ sang \"{trangThai.GetDisplayName()}\".", "Services");

        [HttpPost]
        public Task<IActionResult> EditService(string maDichVu, string tenDichVu, decimal giaDichVu, string donViTinh)
            => RunAsync(() => _serviceApi.UpdateAsync(maDichVu, new ServiceRequest { TenDichVu = tenDichVu, GiaDichVu = giaDichVu, DonViTinh = donViTinh }),
                "Cập nhật dịch vụ thành công!", "Services");

        // ===== STAFF =====
        [HttpGet]
        public async Task<IActionResult> Staff()
        {
            ViewBag.Staff = await _staffApi.GetAllAsync();
            return View();
        }

        [HttpPost]
        public Task<IActionResult> AddStaff(string tenNhanVien, string chucVu, string sdt, string email, string tenDangNhap, string matKhau, Role? vaiTro)
        {
            var request = new CreateStaffRequest
            {
                TenNhanVien = tenNhanVien, ChucVu = chucVu, SDT = sdt?.Trim(), Email = email,
                TenDangNhap = tenDangNhap, MatKhau = matKhau, VaiTro = vaiTro
            };
            return RunAsync(() => _staffApi.CreateAsync(request), "Thêm nhân viên thành công!", "Staff");
        }

        [HttpPost]
        public Task<IActionResult> EditStaff(string maNhanVien, string tenNhanVien, string chucVu, string sdt, string email,
            Role? vaiTro, EmployeeStatus? trangThai)
        {
            var request = new UpdateStaffRequest
            {
                TenNhanVien = tenNhanVien, ChucVu = chucVu, SDT = sdt?.Trim(), Email = email, VaiTro = vaiTro, TrangThai = trangThai
            };
            return RunAsync(() => _staffApi.UpdateAsync(maNhanVien, request), "Cập nhật nhân viên thành công!", "Staff");
        }

        [HttpPost]
        public Task<IActionResult> ResetPassword(string maNhanVien, string matKhauMoi)
            => RunAsync(() => _staffApi.ResetPasswordAsync(maNhanVien, matKhauMoi), "Đã đặt lại mật khẩu cho nhân viên.", "Staff");

        [HttpPost]
        public Task<IActionResult> DeleteStaff(string maNhanVien)
            => RunAsync(() => _staffApi.DeleteAsync(maNhanVien), "Xóa nhân viên thành công!", "Staff");

        // ===== PROMOTIONS =====
        [HttpGet]
        public async Task<IActionResult> Promotions()
        {
            ViewBag.Promotions = await _promotionApi.GetAllAsync();
            return View();
        }

        [HttpPost]
        public Task<IActionResult> AddPromotion(PromotionRequest p)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu khuyến mãi không hợp lệ. Vui lòng kiểm tra lại.";
                return Task.FromResult<IActionResult>(RedirectToAction("Promotions"));
            }

            return RunAsync(() => _promotionApi.CreateAsync(p), "Thêm khuyến mãi thành công!", "Promotions");
        }

        // ===== PRICING RULES (giá theo mùa / cuối tuần / lễ) =====
        [HttpGet]
        public async Task<IActionResult> Pricing()
        {
            ViewBag.Rules = await _pricingApi.GetAllAsync();
            ViewBag.RoomTypes = await _roomTypeApi.GetAllAsync();
            return View();
        }

        [HttpPost]
        public Task<IActionResult> AddPricingRule(PricingRuleRequest rule)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Task.FromResult<IActionResult>(RedirectToAction("Pricing"));
            }

            return RunAsync(() => _pricingApi.CreateAsync(rule), $"Đã thêm quy tắc giá \"{rule.TenQuyTac}\".", "Pricing");
        }

        [HttpPost]
        public Task<IActionResult> SetPricingRuleStatus(string id, bool kichHoat)
            => RunAsync(() => _pricingApi.SetActiveAsync(id, kichHoat), $"Đã {(kichHoat ? "bật" : "tắt")} quy tắc {id}.", "Pricing");

        [HttpPost]
        public Task<IActionResult> DeletePricingRule(string id)
            => RunAsync(() => _pricingApi.DeleteAsync(id), $"Đã xoá quy tắc {id}.", "Pricing");

        // ===== REVIEWS =====
        [HttpGet]
        public async Task<IActionResult> Reviews()
        {
            ViewBag.Reviews = await _reviewApi.GetAllAsync();
            return View();
        }
    }
}
