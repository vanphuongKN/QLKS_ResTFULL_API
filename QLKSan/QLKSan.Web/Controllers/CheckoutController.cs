using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Infrastructure;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [Authorize(Roles = WebRoles.Cashier)]
    public class CheckoutController : Controller
    {
        private readonly IRoomApiClient _roomApi;
        private readonly IInvoiceApiClient _invoiceApi;
        private readonly ICustomerApiClient _customerApi;
        private readonly IBookingApiClient _bookingApi;

        public CheckoutController(IRoomApiClient roomApi, IInvoiceApiClient invoiceApi, ICustomerApiClient customerApi, IBookingApiClient bookingApi)
        {
            _roomApi = roomApi;
            _invoiceApi = invoiceApi;
            _customerApi = customerApi;
            _bookingApi = bookingApi;
        }

        [HttpGet]
        public async Task<IActionResult> Process(string maPhong)
        {
            if (string.IsNullOrEmpty(maPhong))
            {
                return View();
            }

            var booking = await _roomApi.GetCurrentStayAsync(maPhong);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng hoặc phòng chưa check-in.";
                return View();
            }

            ViewBag.Booking = booking;
            ViewBag.Invoice = booking.HoaDon;
            // Bảng kê từng đêm / dịch vụ / ưu đãi / cọc để kiểm tra với khách trước khi chốt hoá đơn
            ViewBag.Folio = await _bookingApi.GetFolioAsync(booking.MaDatPhong);
            // Hạng thành viên + số điểm hiện có để đổi khi thanh toán
            if (!string.IsNullOrEmpty(booking.MaKhachHang))
                ViewBag.Customer = await _customerApi.GetByIdAsync(booking.MaKhachHang);
            return View();
        }

        // Mã nhân viên lập hoá đơn được API lấy từ JWT của người đang đăng nhập
        [HttpPost]
        public async Task<IActionResult> GenerateInvoice(string maDatPhong, string? maKhuyenMai, string maPhong)
        {
            try
            {
                await _invoiceApi.GenerateAsync(new GenerateInvoiceRequest { MaDatPhong = maDatPhong, MaKhuyenMai = maKhuyenMai });
                TempData["SuccessMessage"] = "Đã tạo hóa đơn thành công!";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tạo hóa đơn: {ex.UserMessage}";
            }
            return RedirectToAction("Process", new { maPhong = maPhong });
        }

        [HttpPost]
        public async Task<IActionResult> ApplyPromotion(string maDatPhong, string? maKhuyenMai, string maPhong)
        {
            try
            {
                await _invoiceApi.GenerateAsync(new GenerateInvoiceRequest { MaDatPhong = maDatPhong, MaKhuyenMai = maKhuyenMai });
                TempData["SuccessMessage"] = "Đã cập nhật mã khuyến mãi thành công!";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.UserMessage}";
            }
            return RedirectToAction("Process", new { maPhong = maPhong });
        }

        // tachThanhToan: các khoản ktPhuongThuc / ktSoTien / ktGhiChu (tối đa 3); ngược lại một phương thức cho toàn bộ
        [HttpPost]
        public async Task<IActionResult> Pay(string maHoaDon, string phuongThuc, int diemSuDung = 0, bool tachThanhToan = false,
            List<string> ktPhuongThuc = null, List<decimal?> ktSoTien = null, List<string> ktGhiChu = null, string maPhong = null)
        {
            List<PaymentPart> cacKhoan = null;
            if (tachThanhToan)
            {
                cacKhoan = new List<PaymentPart>();
                for (var i = 0; i < (ktPhuongThuc?.Count ?? 0); i++)
                {
                    var soTien = ktSoTien != null && i < ktSoTien.Count ? ktSoTien[i] : null;
                    if (soTien is null or <= 0) continue;
                    cacKhoan.Add(new PaymentPart { PhuongThuc = ktPhuongThuc[i], SoTien = soTien.Value, GhiChu = ktGhiChu != null && i < ktGhiChu.Count ? ktGhiChu[i] : null });
                }
            }
            try
            {
                await _invoiceApi.PayAsync(maHoaDon, new PaymentRequest { PhuongThuc = phuongThuc, DiemSuDung = diemSuDung, CacKhoan = cacKhoan });
                TempData["SuccessMessage"] = "Thanh toán và trả phòng thành công!";
                TempData["PrintInvoiceId"] = maHoaDon;
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Thanh toán thất bại. " + ex.UserMessage;
                if (!string.IsNullOrEmpty(maPhong)) return RedirectToAction("Process", new { maPhong });
            }

            return RedirectToAction("Index", "Reception");
        }

        // Thông tin xuất hoá đơn VAT công ty (mã số thuế trống = hoá đơn cá nhân)
        [HttpPost]
        public async Task<IActionResult> SetCompany(string maHoaDon, string maPhong, InvoiceCompanyRequest model)
        {
            try
            {
                var h = await _invoiceApi.SetCompanyAsync(maHoaDon, model);
                TempData["SuccessMessage"] = string.IsNullOrEmpty(h.MaSoThue) ? "Hoá đơn xuất cho cá nhân." : $"Đã lưu thông tin xuất hoá đơn cho {h.TenCongTy}.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không lưu được thông tin xuất hoá đơn. " + ex.UserMessage;
            }
            return RedirectToAction("Process", new { maPhong });
        }
    }
}
