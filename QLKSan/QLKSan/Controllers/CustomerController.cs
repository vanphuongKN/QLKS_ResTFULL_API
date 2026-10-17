using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Models.Entities;
using QLKSan.ViewModels;

namespace QLKSan.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        private async Task<KhachHang?> GetLoggedInCustomerAsync()
        {
            var username = Request.Cookies["Username"];
            if (string.IsNullOrEmpty(username)) return null;
            return await _customerService.GetCustomerByUsernameAsync(username);
        }

        [HttpGet]
        public async Task<IActionResult> MyStays()
        {
            var customer = await GetLoggedInCustomerAsync();
            if (customer == null) return RedirectToAction("Login", "Auth");

            ViewBag.Customer = customer;
            ViewBag.Bookings = await _customerService.GetBookingsByCustomerAsync(customer.MaKhachHang);
            
            var reviews = await _customerService.GetReviewsByCustomerAsync(customer.MaKhachHang);
            ViewBag.ReviewedRoomIds = reviews.Select(r => r.MaPhong).ToHashSet();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var customer = await GetLoggedInCustomerAsync();
            if (customer == null) return RedirectToAction("Login", "Auth");

            ViewBag.Customer = customer;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Feedback(string? roomId = null)
        {
            var customer = await GetLoggedInCustomerAsync();
            if (customer == null) return RedirectToAction("Login", "Auth");

            ViewBag.SelectedRoomId = roomId;

            // Query bookings of the logged-in customer
            var bookings = await _customerService.GetBookingsByCustomerAsync(customer.MaKhachHang);
            
            // Filter unique rooms that the customer has actually stayed in and checked out
            var bookedRooms = bookings
                .Where(b => b.TrangThai == QLKSan.Enums.BookingStatus.CheckedOut)
                .SelectMany(b => b.ChiTietDatPhongs ?? Enumerable.Empty<ChiTietDatPhong>())
                .Select(c => c.Phong)
                .Where(r => r != null)
                .GroupBy(r => r.MaPhong)
                .Select(g => g.First())
                .OrderBy(r => r.SoPhong)
                .ToList();

            ViewBag.BookedRooms = bookedRooms;

            // Query all existing reviews by this customer
            var reviews = await _customerService.GetReviewsByCustomerAsync(customer.MaKhachHang);
            var reviewsProjection = reviews.Select(r => new
            {
                maPhong = r.MaPhong,
                soSao = r.SoSao,
                noiDung = r.NoiDung ?? "",
                anDanh = r.AnDanh
            }).ToList();

            ViewBag.MyReviewsJson = System.Text.Json.JsonSerializer.Serialize(reviewsProjection);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(string maPhong, int rating, string comment, bool isAnonymous)
        {
            var customer = await GetLoggedInCustomerAsync();
            if (customer == null) return RedirectToAction("Login", "Auth");

            bool ok = await _customerService.SubmitReviewAsync(customer.MaKhachHang, maPhong, rating, comment, isAnonymous);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Đã gửi đánh giá thành công!" : "Bạn đã đánh giá phòng này rồi hoặc không đủ điều kiện đánh giá.";
            return RedirectToAction("Feedback");
        }

        [HttpGet]
        public async Task<IActionResult> BookRoom()
        {
            var customer = await GetLoggedInCustomerAsync();
            if (customer == null) return RedirectToAction("Login", "Auth");

            ViewBag.AvailableRooms = await _customerService.GetAvailableRoomsForBookingAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> BookRoom(BookRoomViewModel model)
        {
            var customer = await GetLoggedInCustomerAsync();
            if (customer == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                ViewBag.AvailableRooms = await _customerService.GetAvailableRoomsForBookingAsync();
                return View(model);
            }

            string? errorMsg = await _customerService.CreateOnlineBookingAsync(customer.MaKhachHang, model.MaPhong, model.SoNguoi, model.CheckIn, model.CheckOut, model.MaKhuyenMai);
            
            if (errorMsg == null)
            {
                TempData["SuccessMessage"] = "Đặt phòng thành công! Vui lòng chờ duyệt.";
            }
            else
            {
                TempData["ErrorMessage"] = errorMsg;
                ViewBag.AvailableRooms = await _customerService.GetAvailableRoomsForBookingAsync();
                return View(model); // Return to form with error and entered data
            }
            
            return RedirectToAction("MyStays");
        }

        [HttpGet]
        public async Task<IActionResult> CheckPromotion(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã khuyến mãi." });
            }

            var promo = await _customerService.GetPromotionAsync(code);
            if (promo == null)
            {
                return Json(new { success = false, message = "Mã khuyến mãi không tồn tại." });
            }

            var today = DateTime.Today;
            if (promo.TrangThai != Enums.PromotionStatus.Active || promo.NgayBatDau > today || promo.NgayKetThuc < today)
            {
                return Json(new { success = false, message = "Mã khuyến mãi đã hết hạn hoặc chưa được kích hoạt." });
            }

            return Json(new { 
                success = true, 
                message = $"Áp dụng thành công: {promo.TenKhuyenMai}!", 
                loaiGiam = promo.LoaiGiam, 
                giaTriGiam = promo.GiaTriGiam 
            });
        }
    }
}
