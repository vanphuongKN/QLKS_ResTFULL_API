using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Infrastructure;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [Authorize(Roles = WebRoles.FrontDesk)]
    public class ReceptionController : Controller
    {
        private readonly IRoomApiClient _roomApi;
        private readonly IBookingApiClient _bookingApi;
        private readonly ICustomerApiClient _customerApi;

        public ReceptionController(IRoomApiClient roomApi, IBookingApiClient bookingApi, ICustomerApiClient customerApi)
        {
            _roomApi = roomApi;
            _bookingApi = bookingApi;
            _customerApi = customerApi;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string search = null)
        {
            ViewBag.Search = search;
            ViewBag.Rooms = await _roomApi.GetAllAsync();
            ViewBag.OnlineBookings = await _bookingApi.GetOnlineAsync();
            ViewBag.Bookings = await _bookingApi.GetAllAsync(search);
            return View();
        }

        // Đổi phòng đã xếp cho phiếu chưa nhận phòng: liệt kê phòng trống đủ sức chứa trong đúng khoảng ngày của phiếu
        [HttpGet]
        public async Task<IActionResult> ChangeRoom(string id)
        {
            var booking = string.IsNullOrEmpty(id) ? null : await _bookingApi.GetByIdAsync(id);
            if (booking == null) return NotFound();
            ViewBag.Rooms = booking.TrangThai is Enums.BookingStatus.Pending or Enums.BookingStatus.Approved
                ? await _roomApi.GetAvailableAsync(booking.NgayCheckInDuKien, booking.NgayCheckOutDuKien, booking.SoLuongKhach)
                : new List<Models.DTOs.RoomDto>();
            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRoom(string id, string maPhong)
        {
            try
            {
                await _bookingApi.ChangeRoomAsync(id, maPhong);
                TempData["SuccessMessage"] = $"Đã đổi phòng cho phiếu {id}.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không thể đổi phòng. " + ex.UserMessage;
                return RedirectToAction(nameof(ChangeRoom), new { id });
            }
        }

        // Duyệt phòng online
        [HttpPost]
        public async Task<IActionResult> ApproveBooking(string id)
        {
            try
            {
                await _bookingApi.ApproveAsync(id);
                TempData["SuccessMessage"] = "Đã duyệt yêu cầu đặt phòng trực tuyến.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không thể duyệt yêu cầu này. " + ex.UserMessage;
            }
            return RedirectToAction("Index");
        }

        // Check-in phòng online (mã nhân viên lấy từ JWT phía API)
        [HttpPost]
        public async Task<IActionResult> CheckInOnlineBooking(string id)
        {
            try
            {
                await _bookingApi.CheckInAsync(id);
                TempData["SuccessMessage"] = "Khách đã nhận phòng thành công.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không thể check-in cho đơn này. " + ex.UserMessage;
            }
            return RedirectToAction("Index");
        }

        // Từ chối phiếu chờ duyệt / huỷ phiếu đã duyệt
        [HttpPost]
        public async Task<IActionResult> CancelBooking(string id, string lyDo)
        {
            try
            {
                await _bookingApi.CancelAsync(id, lyDo);
                TempData["SuccessMessage"] = $"Đã huỷ/từ chối phiếu đặt phòng {id}.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không thể huỷ phiếu. " + ex.UserMessage;
            }
            return RedirectToAction("Index");
        }

        // --- WALK-IN CHECK-IN (Khách vãng lai) ---

        [HttpGet]
        // maPhong: chọn sẵn phòng khi bấm "Nhận khách" từ sơ đồ phòng
        public async Task<IActionResult> WalkIn(string maPhong)
        {
            ViewBag.AvailableRooms = await _roomApi.GetAvailableAsync();
            return View(new CheckInRequest { MaPhong = maPhong });
        }

        [HttpPost]
        public async Task<IActionResult> WalkIn(CheckInRequest model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _bookingApi.WalkInAsync(model);
                    TempData["SuccessMessage"] = "Tạo mới đặt phòng và Check-in thành công!";
                    return RedirectToAction("Index");
                }
                catch (ApiException ex)
                {
                    ModelState.AddModelError("", ex.UserMessage);
                }
            }

            ViewBag.AvailableRooms = await _roomApi.GetAvailableAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> BookingHistory(string search)
        {
            ViewBag.Search = search;
            var bookings = await _bookingApi.GetAllAsync(search);
            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Customers(string search, bool vip = false)
        {
            ViewBag.Search = search;
            ViewBag.Vip = vip;
            var customers = await _customerApi.GetAllAsync(search);
            return View(vip ? customers.Where(k => k.LaVIP).ToList() : customers);
        }

        // Hồ sơ khách đầy đủ: thông tin, chăm sóc (VIP, sở thích, ghi chú nội bộ, hạn chế), lịch sử kỳ nghỉ, lịch sử điểm
        [HttpGet]
        public async Task<IActionResult> CustomerDetails(string id)
        {
            var customer = string.IsNullOrEmpty(id) ? null : await _customerApi.GetByIdAsync(id);
            if (customer == null) return NotFound();
            ViewBag.Points = await _customerApi.GetPointHistoryAsync(id);
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCustomer(string maKhachHang, UpdateCustomerProfileRequest model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(CustomerDetails), new { id = maKhachHang });
            }
            try
            {
                await _customerApi.UpdateProfileAsync(maKhachHang, model);
                TempData["SuccessMessage"] = "Đã cập nhật hồ sơ khách.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.UserMessage;
            }
            return RedirectToAction(nameof(CustomerDetails), new { id = maKhachHang });
        }

        // Chỉ quản lý (API kiểm tra quyền)
        [HttpPost]
        public async Task<IActionResult> AdjustPoints(string maKhachHang, int soDiem, string lyDo)
        {
            try
            {
                await _customerApi.AdjustPointsAsync(maKhachHang, new PointAdjustmentRequest { SoDiem = soDiem, LyDo = lyDo });
                TempData["SuccessMessage"] = $"Đã {(soDiem >= 0 ? "cộng" : "trừ")} {Math.Abs(soDiem):N0} điểm.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.UserMessage;
            }
            return RedirectToAction(nameof(CustomerDetails), new { id = maKhachHang });
        }
    }
}
