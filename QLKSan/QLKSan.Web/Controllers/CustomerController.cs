using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Infrastructure;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [Authorize(Roles = WebRoles.Customer)]
    public class CustomerController : Controller
    {
        private readonly ICustomerApiClient _customerApi;
        private readonly IRoomApiClient _roomApi;
        private readonly IBookingApiClient _bookingApi;
        private readonly IReviewApiClient _reviewApi;
        private readonly IPromotionApiClient _promotionApi;

        public CustomerController(ICustomerApiClient customerApi, IRoomApiClient roomApi, IBookingApiClient bookingApi,
            IReviewApiClient reviewApi, IPromotionApiClient promotionApi)
        {
            _customerApi = customerApi;
            _roomApi = roomApi;
            _bookingApi = bookingApi;
            _reviewApi = reviewApi;
            _promotionApi = promotionApi;
        }

        [HttpGet]
        public async Task<IActionResult> MyStays()
        {
            var customer = await _customerApi.GetMeAsync();
            if (customer == null) return RedirectToAction("Login", "Auth");

            ViewBag.Customer = customer;
            ViewBag.Bookings = await _customerApi.GetMyBookingsAsync();

            var reviews = await _customerApi.GetMyReviewsAsync();
            ViewBag.ReviewedRoomIds = reviews.Select(r => r.MaPhong).ToHashSet();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var customer = await _customerApi.GetMeAsync();
            if (customer == null) return RedirectToAction("Login", "Auth");

            ViewBag.Customer = customer;
            return View();
        }

        // Khách tự cập nhật SĐT, email, địa chỉ (họ tên và CCCD do lễ tân quản lý)
        [HttpPost]
        public async Task<IActionResult> UpdateContact(UpdateContactRequest model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                await _customerApi.UpdateMyContactAsync(model);
                TempData["SuccessMessage"] = "Đã cập nhật thông tin liên hệ.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.UserMessage;
            }
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public async Task<IActionResult> Feedback(string? roomId = null)
        {
            ViewBag.SelectedRoomId = roomId;

            // Query bookings of the logged-in customer
            var bookings = await _customerApi.GetMyBookingsAsync();

            // Filter unique rooms that the customer has actually stayed in and checked out
            var bookedRooms = bookings
                .Where(b => b.TrangThai == QLKSan.Enums.BookingStatus.CheckedOut)
                .SelectMany(b => b.ChiTietDatPhongs ?? Enumerable.Empty<BookingRoomDto>())
                .Select(c => c.Phong)
                .Where(r => r != null)
                .GroupBy(r => r.MaPhong)
                .Select(g => g.First())
                .OrderBy(r => r.SoPhong)
                .ToList();

            ViewBag.BookedRooms = bookedRooms;

            // Query all existing reviews by this customer
            var reviews = await _customerApi.GetMyReviewsAsync();
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
            try
            {
                await _reviewApi.CreateAsync(new ReviewRequest { MaPhong = maPhong, SoSao = rating, NoiDung = comment, AnDanh = isAnonymous });
                TempData["SuccessMessage"] = "Đã gửi đánh giá thành công!";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.UserMessage;
            }
            return RedirectToAction("Feedback");
        }

        // Danh sách phòng trống phụ thuộc khoảng ngày khách chọn (mặc định: hôm nay → ngày mai)
        [HttpGet]
        public async Task<IActionResult> BookRoom(DateTime? checkIn = null, DateTime? checkOut = null, int? soNguoi = null)
        {
            var model = new BookRoomRequest
            {
                CheckIn = checkIn == null || checkIn.Value.Date < DateTime.Today ? DateTime.Today : checkIn.Value.Date,
                SoNguoi = soNguoi is > 0 ? soNguoi.Value : 1
            };
            model.CheckOut = checkOut != null && checkOut.Value.Date > model.CheckIn ? checkOut.Value.Date : model.CheckIn.AddDays(1);

            ViewBag.AvailableRooms = await _roomApi.GetAvailableAsync(model.CheckIn, model.CheckOut, model.SoNguoi);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> BookRoom(BookRoomRequest model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _bookingApi.CreateAsync(model);
                    TempData["SuccessMessage"] = "Đặt phòng thành công! Vui lòng chờ duyệt.";
                    return RedirectToAction("MyStays");
                }
                catch (ApiException ex)
                {
                    TempData["ErrorMessage"] = ex.UserMessage;
                }
            }

            // Return to form with error and entered data
            ViewBag.AvailableRooms = model.CheckOut > model.CheckIn && model.CheckIn >= DateTime.Today
                ? await _roomApi.GetAvailableAsync(model.CheckIn, model.CheckOut, model.SoNguoi > 0 ? model.SoNguoi : null)
                : await _roomApi.GetAvailableAsync();
            return View(model);
        }

        // Khách tự huỷ phiếu chưa nhận phòng (trước ngày nhận phòng)
        [HttpPost]
        public async Task<IActionResult> CancelBooking(string id, string lyDo)
        {
            try
            {
                await _bookingApi.CancelAsync(id, lyDo);
                TempData["SuccessMessage"] = $"Đã huỷ phiếu đặt phòng {id}.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không thể huỷ phiếu. " + ex.UserMessage;
            }
            return RedirectToAction("MyStays");
        }

        // Báo giá theo từng đêm cho khoảng ngày đã chọn (giá cuối tuần / lễ có thể khác giá niêm yết)
        [HttpGet]
        public async Task<IActionResult> Quote(string maPhong, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                var quote = await _roomApi.QuoteAsync(maPhong, checkIn, checkOut);
                return Json(new
                {
                    success = true,
                    soDem = quote.SoDem,
                    tongTien = quote.TongTien,
                    giaTrungBinh = quote.GiaTrungBinhDem,
                    cacDem = quote.CacDem.Select(d => new { dem = d.Dem.ToString("dd/MM"), gia = d.Gia, quyTac = d.QuyTac })
                });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = ex.UserMessage });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckPromotion(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã khuyến mãi." });
            }

            try
            {
                var promo = await _promotionApi.GetActiveAsync(code);
                return Json(new {
                    success = true,
                    message = $"Áp dụng thành công: {promo.TenKhuyenMai}!",
                    loaiGiam = promo.LoaiGiam,
                    giaTriGiam = promo.GiaTriGiam
                });
            }
            catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
            {
                // API trả 404: không tồn tại, 400: hết hạn hoặc chưa kích hoạt
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
