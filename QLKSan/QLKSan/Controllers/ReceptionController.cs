using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.ViewModels;

namespace QLKSan.Controllers
{
    public class ReceptionController : Controller
    {
        private readonly IReceptionService _receptionService;

        public ReceptionController(IReceptionService receptionService)
        {
            _receptionService = receptionService;
        }

        // Dashboard Lễ Tân
        [HttpGet]
        public async Task<IActionResult> Index(string search = null)
        {
            ViewBag.Search = search;
            ViewBag.Rooms = await _receptionService.GetAllRoomsAsync();
            ViewBag.OnlineBookings = await _receptionService.GetOnlineBookingsAsync();
            ViewBag.Bookings = await _receptionService.GetAllBookingsAsync(search);
            return View();
        }

        // Duyệt phòng online
        [HttpPost]
        public async Task<IActionResult> ApproveBooking(string id)
        {
            bool success = await _receptionService.ApproveOnlineBookingAsync(id);
            if (success) TempData["SuccessMessage"] = "Đã duyệt yêu cầu đặt phòng trực tuyến.";
            else TempData["ErrorMessage"] = "Không thể duyệt yêu cầu này.";
            return RedirectToAction("Index");
        }

        // Check-in phòng online
        [HttpPost]
        public async Task<IActionResult> CheckInOnlineBooking(string id)
        {
            string maNhanVien = "NV01"; // Default fallback
            var username = Request.Cookies["Username"];
            if (!string.IsNullOrEmpty(username))
            {
                var account = await _receptionService.GetAccountByUsernameAsync(username);
                if (account != null && account.NhanVien != null)
                {
                    maNhanVien = account.NhanVien.MaNhanVien;
                }
            }

            bool success = await _receptionService.CheckInOnlineBookingAsync(id, maNhanVien);
            if (success) TempData["SuccessMessage"] = "Khách đã nhận phòng thành công.";
            else TempData["ErrorMessage"] = "Không thể check-in cho đơn này. Vui lòng thử lại.";
            return RedirectToAction("Index");
        }

        // --- WALK-IN CHECK-IN (Khách vãng lai) ---

        [HttpGet]
        public async Task<IActionResult> WalkIn()
        {
            var availableRooms = await _receptionService.GetAvailableRoomsAsync();
            ViewBag.AvailableRooms = availableRooms;
            return View(new CheckInViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> WalkIn(CheckInViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AvailableRooms = await _receptionService.GetAvailableRoomsAsync();
                return View(model);
            }

            string maNhanVien = "NV01"; // Default fallback
            var username = Request.Cookies["Username"];
            if (!string.IsNullOrEmpty(username))
            {
                var account = await _receptionService.GetAccountByUsernameAsync(username);
                if (account != null && account.NhanVien != null)
                {
                    maNhanVien = account.NhanVien.MaNhanVien;
                }
            }

            string error = await _receptionService.ProcessCheckInAsync(model, maNhanVien);
            
            if (error == null)
            {
                TempData["SuccessMessage"] = "Tạo mới đặt phòng và Check-in thành công!";
                return RedirectToAction("Index");
            }
            
            ModelState.AddModelError("", error);
            ViewBag.AvailableRooms = await _receptionService.GetAvailableRoomsAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> BookingHistory(string search)
        {
            ViewBag.Search = search;
            var bookings = await _receptionService.GetAllBookingsAsync(search);
            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Customers(string search)
        {
            ViewBag.Search = search;
            var customers = await _receptionService.GetAllCustomersAsync(search);
            return View(customers);
        }

        [HttpGet]
        public async Task<IActionResult> CustomerDetails(string id)
        {
            var customer = await _receptionService.GetCustomerDetailsAsync(id);
            if (customer == null)
            {
                return NotFound("Không tìm thấy khách hàng.");
            }
            return PartialView("_CustomerDetailsPartial", customer);
        }
    }
}
