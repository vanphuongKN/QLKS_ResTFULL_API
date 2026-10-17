using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;

namespace QLKSan.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ICheckoutService _checkoutService;
        private readonly IReceptionService _receptionService;

        public CheckoutController(ICheckoutService checkoutService, IReceptionService receptionService)
        {
            _checkoutService = checkoutService;
            _receptionService = receptionService;
        }

        [HttpGet]
        public async Task<IActionResult> Process(string maPhong)
        {
            if (string.IsNullOrEmpty(maPhong))
            {
                return View();
            }

            var booking = await _checkoutService.GetBookingDetailsByRoomAsync(maPhong);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng hoặc phòng chưa check-in.";
                return View();
            }

            ViewBag.Booking = booking;
            ViewBag.Invoice = booking.HoaDon;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GenerateInvoice(string maDatPhong, string? maKhuyenMai, string maPhong)
        {
            // Get real employee ID from cookie
            string maNhanVienLap = "NV01";
            var username = Request.Cookies["Username"];
            if (!string.IsNullOrEmpty(username))
            {
                var account = await _receptionService.GetAccountByUsernameAsync(username);
                if (account != null && account.NhanVien != null)
                {
                    maNhanVienLap = account.NhanVien.MaNhanVien;
                }
            }

            try
            {
                var invoice = await _checkoutService.GenerateInvoiceAsync(maDatPhong, maNhanVienLap, maKhuyenMai);
                
                if (invoice != null)
                {
                    TempData["SuccessMessage"] = "Đã tạo hóa đơn thành công!";
                    return RedirectToAction("Process", new { maPhong = maPhong });
                }

                TempData["ErrorMessage"] = "Lỗi khi tạo hóa đơn. Không tìm thấy đơn đặt phòng.";
                return RedirectToAction("Process", new { maPhong = maPhong });
            }
            catch (System.Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                TempData["ErrorMessage"] = $"Lỗi khi tạo hóa đơn: {innerMsg}";
                return RedirectToAction("Process", new { maPhong = maPhong });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApplyPromotion(string maDatPhong, string? maKhuyenMai, string maPhong)
        {
            string maNhanVienLap = "NV01";
            var username = Request.Cookies["Username"];
            if (!string.IsNullOrEmpty(username))
            {
                var account = await _receptionService.GetAccountByUsernameAsync(username);
                if (account != null && account.NhanVien != null)
                {
                    maNhanVienLap = account.NhanVien.MaNhanVien;
                }
            }

            try
            {
                var invoice = await _checkoutService.GenerateInvoiceAsync(maDatPhong, maNhanVienLap, maKhuyenMai);
                if (invoice != null)
                {
                    TempData["SuccessMessage"] = "Đã cập nhật mã khuyến mãi thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi khi cập nhật mã khuyến mãi.";
                }
            }
            catch (System.Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                TempData["ErrorMessage"] = $"Lỗi: {innerMsg}";
            }

            return RedirectToAction("Process", new { maPhong = maPhong });
        }

        [HttpPost]
        public async Task<IActionResult> Pay(string maHoaDon, string phuongThuc)
        {
            bool success = await _checkoutService.ProcessPaymentAsync(maHoaDon, phuongThuc);
            if (success)
            {
                TempData["SuccessMessage"] = "Thanh toán và trả phòng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Thanh toán thất bại. Vui lòng thử lại.";
            }

            return RedirectToAction("Index", "Reception");
        }
    }
}
