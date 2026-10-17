using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Models.Entities;

namespace QLKSan.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService) { _adminService = adminService; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Revenue = await _adminService.GetTotalRevenueAsync();
            ViewBag.WeeklyRevenue = await _adminService.GetWeeklyRevenueAsync();
            ViewBag.Bookings = await _adminService.GetTotalBookingsAsync();
            ViewBag.Customers = await _adminService.GetTotalCustomersAsync();
            ViewBag.Promotions = await _adminService.GetAllPromotionsAsync();
            ViewBag.YearlyRevenue = await _adminService.GetYearlyRevenueHistoryAsync(DateTime.Now.Year);
            return View();
        }

        // ===== ROOM TYPES =====
        [HttpGet]
        public async Task<IActionResult> RoomTypes()
        {
            ViewBag.RoomTypes = await _adminService.GetAllRoomTypesAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddRoomType(string tenLoai, int soNguoiToiDa, decimal giaThueNgay, decimal giaThueGio, string moTa)
        {
            var maLoai = await _adminService.GenerateNextRoomTypeIdAsync();
            var rt = new LoaiPhong { MaLoai = maLoai, TenLoai = tenLoai, SoNguoiToiDa = soNguoiToiDa, GiaThueNgay = giaThueNgay, GiaThueGio = giaThueGio, MoTa = moTa };
            bool ok = await _adminService.AddRoomTypeAsync(rt);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Thêm loại phòng thành công!" : "Lỗi thêm loại phòng.";
            return RedirectToAction("RoomTypes");
        }

        [HttpPost]
        public async Task<IActionResult> EditRoomType(string maLoai, string tenLoai, int soNguoiToiDa, decimal giaThueNgay, decimal giaThueGio, string moTa)
        {
            var rt = new LoaiPhong { MaLoai = maLoai, TenLoai = tenLoai, SoNguoiToiDa = soNguoiToiDa, GiaThueNgay = giaThueNgay, GiaThueGio = giaThueGio, MoTa = moTa };
            bool ok = await _adminService.UpdateRoomTypeAsync(rt);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Cập nhật loại phòng thành công!" : "Lỗi cập nhật.";
            return RedirectToAction("RoomTypes");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRoomType(string maLoai)
        {
            bool ok = await _adminService.DeleteRoomTypeAsync(maLoai);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Xóa loại phòng thành công!" : "Không thể xóa (có phòng đang dùng loại này).";
            return RedirectToAction("RoomTypes");
        }

        // ===== ROOMS =====
        [HttpGet]
        public async Task<IActionResult> Rooms()
        {
            ViewBag.Rooms = await _adminService.GetAllRoomsAsync();
            ViewBag.RoomTypes = await _adminService.GetAllRoomTypesAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddRoom(string soPhong, string maLoai)
        {
            var maPhong = "P" + soPhong.Trim();
            var room = new Phong { MaPhong = maPhong, SoPhong = soPhong, MaLoai = maLoai };
            bool ok = await _adminService.AddRoomAsync(room);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Thêm phòng thành công!" : "Lỗi thêm phòng.";
            return RedirectToAction("Rooms");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRoom(string maPhong)
        {
            bool ok = await _adminService.DeleteRoomAsync(maPhong);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Xóa phòng thành công!" : "Không thể xóa (phòng có liên kết).";
            return RedirectToAction("Rooms");
        }

        [HttpPost]
        public async Task<IActionResult> EditRoom(string maPhong, string soPhong, string maLoai)
        {
            var room = new Phong { MaPhong = maPhong, SoPhong = soPhong, MaLoai = maLoai };
            bool ok = await _adminService.UpdateRoomAsync(room);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Cập nhật phòng thành công!" : "Lỗi cập nhật.";
            return RedirectToAction("Rooms");
        }

        // ===== SERVICES =====
        [HttpGet]
        public async Task<IActionResult> Services()
        {
            ViewBag.Services = await _adminService.GetAllServicesAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddService(string tenDichVu, decimal giaDichVu, string donViTinh)
        {
            var maDichVu = await _adminService.GenerateNextServiceIdAsync();
            var s = new DichVu { MaDichVu = maDichVu, TenDichVu = tenDichVu, GiaDichVu = giaDichVu, DonViTinh = donViTinh };
            bool ok = await _adminService.AddServiceAsync(s);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Thêm dịch vụ thành công!" : "Lỗi thêm dịch vụ.";
            return RedirectToAction("Services");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteService(string maDichVu)
        {
            bool ok = await _adminService.DeleteServiceAsync(maDichVu);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Xóa dịch vụ thành công!" : "Không thể xóa.";
            return RedirectToAction("Services");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleServiceStatus(string maDichVu)
        {
            bool ok = await _adminService.ToggleServiceStatusAsync(maDichVu);
            if (ok) TempData["SuccessMessage"] = "Đã thay đổi trạng thái dịch vụ!";
            else TempData["ErrorMessage"] = "Lỗi khi đổi trạng thái dịch vụ.";
            return RedirectToAction("Services");
        }

        [HttpPost]
        public async Task<IActionResult> EditService(string maDichVu, string tenDichVu, decimal giaDichVu, string donViTinh)
        {
            var s = new DichVu { MaDichVu = maDichVu, TenDichVu = tenDichVu, GiaDichVu = giaDichVu, DonViTinh = donViTinh };
            bool ok = await _adminService.UpdateServiceAsync(s);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Cập nhật dịch vụ thành công!" : "Lỗi cập nhật.";
            return RedirectToAction("Services");
        }

        // ===== STAFF =====
        [HttpGet]
        public async Task<IActionResult> Staff()
        {
            ViewBag.Staff = await _adminService.GetAllStaffAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddStaff(string tenNhanVien, string chucVu, string sdt, string email, string tenDangNhap, string matKhau)
        {
            var phoneRegex = new System.Text.RegularExpressions.Regex(@"^(0[3|5|7|8|9])+([0-9]{8})\b");
            if (!phoneRegex.IsMatch(sdt?.Trim() ?? ""))
            {
                TempData["ErrorMessage"] = "Số điện thoại nhân viên không hợp lệ.";
                return RedirectToAction("Staff");
            }
            if ((matKhau?.Length ?? 0) < 6)
            {
                TempData["ErrorMessage"] = "Mật khẩu nhân viên phải từ 6 ký tự trở lên.";
                return RedirectToAction("Staff");
            }

            var maNhanVien = await _adminService.GenerateNextStaffIdAsync();
            var nv = new NhanVien { MaNhanVien = maNhanVien, TenNhanVien = tenNhanVien, ChucVu = chucVu, SDT = sdt, Email = email, NgayVaoLam = DateTime.Now };
            bool ok = await _adminService.AddStaffAsync(nv, tenDangNhap, matKhau);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Thêm nhân viên thành công!" : "Lỗi.";
            return RedirectToAction("Staff");
        }

        [HttpPost]
        public async Task<IActionResult> EditStaff(string maNhanVien, string tenNhanVien, string chucVu, string sdt, string email)
        {
            var phoneRegex = new System.Text.RegularExpressions.Regex(@"^(0[3|5|7|8|9])+([0-9]{8})\b");
            if (!phoneRegex.IsMatch(sdt?.Trim() ?? ""))
            {
                TempData["ErrorMessage"] = "Số điện thoại nhân viên không hợp lệ.";
                return RedirectToAction("Staff");
            }

            var nv = new NhanVien { MaNhanVien = maNhanVien, TenNhanVien = tenNhanVien, ChucVu = chucVu, SDT = sdt, Email = email };
            bool ok = await _adminService.UpdateStaffAsync(nv);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Cập nhật nhân viên thành công!" : "Lỗi cập nhật.";
            return RedirectToAction("Staff");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStaff(string maNhanVien)
        {
            bool ok = await _adminService.DeleteStaffAsync(maNhanVien);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Xóa nhân viên thành công!" : "Không thể xóa.";
            return RedirectToAction("Staff");
        }

        // ===== PROMOTIONS =====
        [HttpGet]
        public async Task<IActionResult> Promotions()
        {
            ViewBag.Promotions = await _adminService.GetAllPromotionsAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddPromotion(KhuyenMai p)
        {
            if (!ModelState.IsValid)
            {
                // Note: The UI for AddPromotion is likely a modal or part of Promotions view. 
                // We should redirect back with error if not valid, or return view. 
                // Returning view for a single page might require repopulating ViewBag.
                TempData["ErrorMessage"] = "Dữ liệu khuyến mãi không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Promotions");
            }
            
            bool ok = await _adminService.AddPromotionAsync(p);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Thêm khuyến mãi thành công!" : "Lỗi thêm khuyến mãi (có thể trùng mã).";
            return RedirectToAction("Promotions");
        }

        // ===== REVIEWS =====
        [HttpGet]
        public async Task<IActionResult> Reviews()
        {
            ViewBag.Reviews = await _adminService.GetAllReviewsAsync();
            return View();
        }
    }
}
