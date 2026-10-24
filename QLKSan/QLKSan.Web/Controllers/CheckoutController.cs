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

        public CheckoutController(IRoomApiClient roomApi, IInvoiceApiClient invoiceApi)
        {
            _roomApi = roomApi;
            _invoiceApi = invoiceApi;
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

        [HttpPost]
        public async Task<IActionResult> Pay(string maHoaDon, string phuongThuc)
        {
            try
            {
                await _invoiceApi.PayAsync(maHoaDon, new PaymentRequest { PhuongThuc = phuongThuc });
                TempData["SuccessMessage"] = "Thanh toán và trả phòng thành công!";
                TempData["PrintInvoiceId"] = maHoaDon;
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Thanh toán thất bại. " + ex.UserMessage;
            }

            return RedirectToAction("Index", "Reception");
        }
    }
}
