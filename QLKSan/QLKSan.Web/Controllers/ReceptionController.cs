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
        public async Task<IActionResult> Customers(string search)
        {
            ViewBag.Search = search;
            var customers = await _customerApi.GetAllAsync(search);
            return View(customers);
        }

        [HttpGet]
        public async Task<IActionResult> CustomerDetails(string id)
        {
            var customer = await _customerApi.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound("Không tìm thấy khách hàng.");
            }
            return PartialView("_CustomerDetailsPartial", customer);
        }
    }
}
