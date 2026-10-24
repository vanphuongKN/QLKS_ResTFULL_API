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
            return View(await _dashboardApi.GetAsync());
        }

        // ===== ROOM TYPES =====
        [HttpGet]
        public async Task<IActionResult> RoomTypes()
        {
            ViewBag.RoomTypes = await _roomTypeApi.GetAllAsync();
            return View();
        }

        [HttpPost]
        public Task<IActionResult> AddRoomType(RoomTypeRequest request)
            => RunAsync(() => _roomTypeApi.CreateAsync(request), "Thêm loại phòng thành công!", "RoomTypes");

        [HttpPost]
        public Task<IActionResult> EditRoomType(string maLoai, RoomTypeRequest request)
            => RunAsync(() => _roomTypeApi.UpdateAsync(maLoai, request), "Cập nhật loại phòng thành công!", "RoomTypes");

        // Bộ ảnh của hạng phòng (ảnh đầu tiên là ảnh đại diện trên trang khách)
        [HttpGet]
        public async Task<IActionResult> RoomTypeImages(string id)
        {
            var detail = string.IsNullOrEmpty(id) ? null : await _roomTypeApi.GetDetailAsync(id);
            if (detail == null) return NotFound();
            return View(detail);
        }

        [HttpPost]
        public async Task<IActionResult> UploadRoomTypeImage(string maLoai, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ảnh cần tải lên.";
            }
            else
            {
                try
                {
                    using var stream = file.OpenReadStream();
                    await _roomTypeApi.UploadImageAsync(maLoai, stream, file.FileName, file.ContentType);
                    TempData["SuccessMessage"] = "Đã tải ảnh lên.";
                }
                catch (ApiException ex)
                {
                    TempData["ErrorMessage"] = ex.UserMessage;
                }
            }
            return RedirectToAction(nameof(RoomTypeImages), new { id = maLoai });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRoomTypeImage(string maLoai, string maHinhAnh)
        {
            try
            {
                await _roomTypeApi.DeleteImageAsync(maLoai, maHinhAnh);
                TempData["SuccessMessage"] = "Đã xoá ảnh.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.UserMessage;
            }
            return RedirectToAction(nameof(RoomTypeImages), new { id = maLoai });
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
        public Task<IActionResult> AddService(ServiceRequest request)
            => RunAsync(() => _serviceApi.CreateAsync(request), "Thêm dịch vụ thành công!", "Services");

        [HttpPost]
        public Task<IActionResult> DeleteService(string maDichVu)
            => RunAsync(() => _serviceApi.DeleteAsync(maDichVu), "Xóa dịch vụ thành công!", "Services");

        [HttpPost]
        public Task<IActionResult> SetServiceStatus(string maDichVu, ServiceStatus trangThai)
            => RunAsync(() => _serviceApi.SetStatusAsync(maDichVu, trangThai),
                $"Đã chuyển dịch vụ sang \"{trangThai.GetDisplayName()}\".", "Services");

        [HttpPost]
        public Task<IActionResult> EditService(string maDichVu, ServiceRequest request)
            => RunAsync(() => _serviceApi.UpdateAsync(maDichVu, request), "Cập nhật dịch vụ thành công!", "Services");

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
            ViewBag.RoomTypes = await _roomTypeApi.GetAllAsync();
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

        [HttpPost]
        public Task<IActionResult> EditPromotion(string maKhuyenMai, PromotionRequest p)
        {
            p.MaKhuyenMai = maKhuyenMai;
            ModelState.Remove(nameof(PromotionRequest.MaKhuyenMai));
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Task.FromResult<IActionResult>(RedirectToAction("Promotions"));
            }
            return RunAsync(() => _promotionApi.UpdateAsync(maKhuyenMai, p), $"Đã cập nhật khuyến mãi {maKhuyenMai}.", "Promotions");
        }

        [HttpPost]
        public Task<IActionResult> SetPromotionStatus(string maKhuyenMai, bool kichHoat)
            => RunAsync(() => _promotionApi.SetActiveAsync(maKhuyenMai, kichHoat), $"Đã {(kichHoat ? "bật" : "tắt")} mã {maKhuyenMai}.", "Promotions");

        [HttpPost]
        public Task<IActionResult> DeletePromotion(string maKhuyenMai)
            => RunAsync(() => _promotionApi.DeleteAsync(maKhuyenMai), $"Đã xoá mã {maKhuyenMai}.", "Promotions");

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
        public async Task<IActionResult> Reviews(int? soSao = null, bool chuaPhanHoi = false, bool? an = null)
        {
            // Thống kê luôn tính trên toàn bộ đánh giá; danh sách theo bộ lọc
            var all = await _reviewApi.GetAllAsync();
            ViewBag.AllReviews = all;
            ViewBag.Reviews = soSao == null && !chuaPhanHoi && an == null ? all : await _reviewApi.GetAllAsync(soSao, chuaPhanHoi, an);
            ViewBag.SoSao = soSao;
            ViewBag.ChuaPhanHoi = chuaPhanHoi;
            ViewBag.An = an;
            return View();
        }

        // Thao tác trên một đánh giá rồi quay lại đúng bộ lọc đang xem
        private async Task<IActionResult> ReviewActionAsync(Func<Task> action, string successMessage, int? soSao, bool chuaPhanHoi, bool? an)
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
            return RedirectToAction(nameof(Reviews), new { soSao, chuaPhanHoi = chuaPhanHoi ? true : (bool?)null, an });
        }

        [HttpPost]
        public Task<IActionResult> ReplyReview(string maDanhGia, string noiDung, int? soSao, bool chuaPhanHoi, bool? an)
            => ReviewActionAsync(() => _reviewApi.ReplyAsync(maDanhGia, noiDung), "Đã gửi phản hồi — phản hồi hiển thị công khai trên trang hạng phòng.", soSao, chuaPhanHoi, an);

        [HttpPost]
        public Task<IActionResult> DeleteReviewReply(string maDanhGia, int? soSao, bool chuaPhanHoi, bool? an)
            => ReviewActionAsync(() => _reviewApi.DeleteReplyAsync(maDanhGia), "Đã gỡ phản hồi.", soSao, chuaPhanHoi, an);

        [HttpPost]
        public Task<IActionResult> SetReviewVisibility(string maDanhGia, bool hide, int? soSao, bool chuaPhanHoi, bool? an)
            => ReviewActionAsync(() => _reviewApi.SetHiddenAsync(maDanhGia, hide),
                hide ? "Đã ẩn đánh giá khỏi trang khách (vẫn tính vào điểm trung bình)." : "Đã hiện lại đánh giá.", soSao, chuaPhanHoi, an);
    }
}
