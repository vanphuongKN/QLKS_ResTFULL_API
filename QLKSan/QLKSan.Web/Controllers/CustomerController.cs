using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Enums;
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
        private readonly IRoomTypeApiClient _roomTypeApi;
        private readonly IBookingApiClient _bookingApi;
        private readonly IReviewApiClient _reviewApi;
        private readonly IPromotionApiClient _promotionApi;
        private readonly HotelInfoProvider _hotelInfo;

        public CustomerController(ICustomerApiClient customerApi, IRoomApiClient roomApi, IRoomTypeApiClient roomTypeApi, IBookingApiClient bookingApi,
            IReviewApiClient reviewApi, IPromotionApiClient promotionApi, HotelInfoProvider hotelInfo)
        {
            _customerApi = customerApi;
            _roomApi = roomApi;
            _roomTypeApi = roomTypeApi;
            _bookingApi = bookingApi;
            _reviewApi = reviewApi;
            _promotionApi = promotionApi;
            _hotelInfo = hotelInfo;
        }

        [HttpGet]
        public async Task<IActionResult> MyStays()
        {
            var customer = await _customerApi.GetMeAsync();
            if (customer == null) return RedirectToAction("Login", "Auth");

            ViewBag.Customer = customer;
            ViewBag.Bookings = await _customerApi.GetMyBookingsAsync();

            // Mỗi lần lưu trú được đánh giá một lần
            var reviews = await _customerApi.GetMyReviewsAsync();
            ViewBag.ReviewedBookingIds = reviews.Where(r => r.MaDatPhong != null).Select(r => r.MaDatPhong).ToHashSet();

            return View();
        }

        // Chi tiết một phiếu đặt: giá từng đêm, yêu cầu riêng, thông tin trước ngày đến, hoá đơn, huỷ, đánh giá
        [HttpGet]
        public async Task<IActionResult> Booking(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            BookingDto booking;
            try
            {
                booking = await _bookingApi.GetByIdAsync(id);
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                return Forbid();
            }
            if (booking == null) return NotFound();

            var room = booking.ChiTietDatPhongs.FirstOrDefault();
            if (room != null && booking.HoaDon == null && booking.NgayCheckOutDuKien.Date > booking.NgayCheckInDuKien.Date)
            {
                try
                {
                    ViewBag.Quote = await _roomApi.QuoteAsync(room.MaPhong, booking.NgayCheckInDuKien, booking.NgayCheckOutDuKien);
                }
                catch (ApiException) { /* không có báo giá thì chỉ hiển thị giá niêm yết */ }
            }
            var reviews = await _customerApi.GetMyReviewsAsync();
            ViewBag.Review = reviews.FirstOrDefault(r => r.MaDatPhong == booking.MaDatPhong);
            ViewBag.Hotel = await _hotelInfo.GetAsync();
            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var customer = await _customerApi.GetMeAsync();
            if (customer == null) return RedirectToAction("Login", "Auth");

            ViewBag.Customer = customer;
            ViewBag.Points = await _customerApi.GetMyPointHistoryAsync();
            return View();
        }

        // Khách tự cập nhật SĐT, email, địa chỉ, sở thích lưu trú (họ tên và CCCD do lễ tân quản lý)
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

        // Đánh giá theo lần lưu trú: chỉ các kỳ nghỉ đã trả phòng và chưa đánh giá
        [HttpGet]
        public async Task<IActionResult> Feedback(string maDatPhong = null)
        {
            var bookings = await _customerApi.GetMyBookingsAsync();
            var reviews = await _customerApi.GetMyReviewsAsync();
            var daDanhGia = reviews.Where(r => r.MaDatPhong != null).Select(r => r.MaDatPhong).ToHashSet();

            ViewBag.Stays = bookings
                .Where(b => b.TrangThai == BookingStatus.CheckedOut && !daDanhGia.Contains(b.MaDatPhong))
                .OrderByDescending(b => b.NgayCheckOutThucTe ?? b.NgayCheckOutDuKien)
                .ToList();
            ViewBag.SelectedBookingId = maDatPhong;
            ViewBag.MyReviews = reviews.OrderByDescending(r => r.NgayDanhGia).ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(string maDatPhong, int rating, string comment, bool isAnonymous)
        {
            try
            {
                await _reviewApi.CreateAsync(new ReviewRequest { MaDatPhong = maDatPhong, SoSao = rating, NoiDung = comment, AnDanh = isAnonymous });
                TempData["SuccessMessage"] = "Cảm ơn bạn đã chia sẻ cảm nhận về kỳ nghỉ!";
                return RedirectToAction(nameof(Booking), new { id = maDatPhong });
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.UserMessage;
                return RedirectToAction(nameof(Feedback), new { maDatPhong });
            }
        }

        // Đặt phòng 3 bước: chọn ngày → chọn hạng phòng (giá từng đêm) → yêu cầu riêng & xác nhận.
        // Xem phòng trống không cần đăng nhập; bước xác nhận mới yêu cầu tài khoản khách hàng
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> BookRoom(DateTime? checkIn = null, DateTime? checkOut = null, int? soNguoi = null, string maLoai = null)
        {
            var model = new BookRoomRequest
            {
                CheckIn = checkIn == null || checkIn.Value.Date < DateTime.Today ? DateTime.Today : checkIn.Value.Date,
                SoNguoi = soNguoi is > 0 ? soNguoi.Value : 2,
                MaLoai = maLoai
            };
            model.CheckOut = checkOut != null && checkOut.Value.Date > model.CheckIn ? checkOut.Value.Date : model.CheckIn.AddDays(1);
            await LoadBookingStepsAsync(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> BookRoom(BookRoomRequest model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var booking = await _bookingApi.CreateAsync(model);
                    TempData["SuccessMessage"] = booking.TrangThai == BookingStatus.Approved
                        ? "Đặt phòng thành công — kỳ nghỉ của bạn đã được xác nhận."
                        : "Đã gửi yêu cầu đặt phòng, lễ tân sẽ xác nhận sớm.";
                    return RedirectToAction(nameof(Booking), new { id = booking.MaDatPhong });
                }
                catch (ApiException ex)
                {
                    TempData["ErrorMessage"] = ex.UserMessage;
                }
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }

            if (model.CheckIn < DateTime.Today) model.CheckIn = DateTime.Today;
            if (model.CheckOut <= model.CheckIn) model.CheckOut = model.CheckIn.AddDays(1);
            await LoadBookingStepsAsync(model);
            return View(model);
        }

        // Dữ liệu cho trang đặt phòng: hạng còn trống + báo giá, hạng đang chọn, hồ sơ khách, chính sách
        private async Task LoadBookingStepsAsync(BookRoomRequest model)
        {
            var types = await _roomTypeApi.GetAvailableAsync(model.CheckIn, model.CheckOut, model.SoNguoi);
            ViewBag.Types = types;
            ViewBag.Hotel = await _hotelInfo.GetAsync();
            if (!string.IsNullOrEmpty(model.MaLoai))
            {
                var selected = types.FirstOrDefault(t => t.LoaiPhong.MaLoai == model.MaLoai);
                if (selected == null)
                {
                    TempData["ErrorMessage"] ??= "Hạng phòng này đã hết phòng trong khoảng ngày đã chọn. Vui lòng chọn hạng khác.";
                    model.MaLoai = null;
                }
                ViewBag.Selected = selected;
            }
            if (User.IsInRole(WebRoles.Customer))
            {
                ViewBag.Customer = await _customerApi.GetMeAsync();
            }
        }

        // Khách tự huỷ phiếu chưa nhận phòng (trước ngày nhận phòng)
        [HttpPost]
        public async Task<IActionResult> CancelBooking(string id, string lyDo, bool veChiTiet = false)
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
            return veChiTiet ? RedirectToAction(nameof(Booking), new { id }) : RedirectToAction(nameof(MyStays));
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
