using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Infrastructure;

namespace QLKSan.Controllers
{
    [Authorize(Roles = WebRoles.Cashier)]
    public class RoomController : Controller
    {
        private readonly IRoomApiClient _roomApi;
        private readonly IBookingApiClient _bookingApi;
        private readonly IReportApiClient _reportApi;

        public RoomController(IRoomApiClient roomApi, IBookingApiClient bookingApi, IReportApiClient reportApi)
        {
            _roomApi = roomApi;
            _bookingApi = bookingApi;
            _reportApi = reportApi;
        }

        // Lịch phòng: mỗi hàng 1 phòng, mỗi cột 1 đêm — mặc định 14 đêm từ hôm nay
        [HttpGet]
        public async Task<IActionResult> Timeline(DateTime? tuNgay, int soNgay = 14)
        {
            var from = (tuNgay ?? DateTime.Today).Date;
            soNgay = Math.Clamp(soNgay, 7, 31);
            ViewBag.TuNgay = from;
            ViewBag.SoNgay = soNgay;
            ViewBag.Bookings = await _bookingApi.GetTimelineAsync(from, from.AddDays(soNgay - 1));
            var rooms = await _roomApi.GetAllAsync();
            return View(rooms.OrderBy(r => r.SoPhong).ToList());
        }

        // Sổ khách lưu trú (khai báo tạm trú): khách có thời gian ở giao với khoảng ngày
        [HttpGet]
        public async Task<IActionResult> Guests(DateTime? tuNgay, DateTime? denNgay)
        {
            var (from, to) = GuestRange(tuNgay, denNgay);
            ViewBag.TuNgay = from;
            ViewBag.DenNgay = to;
            return View(await _reportApi.GetGuestsAsync(from, to));
        }

        [HttpGet]
        public async Task<IActionResult> ExportGuests(DateTime? tuNgay, DateTime? denNgay)
        {
            var (from, to) = GuestRange(tuNgay, denNgay);
            var (content, contentType, fileName) = await _reportApi.ExportGuestsAsync(from, to);
            return File(content, contentType, fileName ?? $"SoKhachLuuTru_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
        }

        // Mặc định: hôm nay
        private static (DateTime From, DateTime To) GuestRange(DateTime? tuNgay, DateTime? denNgay)
        {
            var from = (tuNgay ?? DateTime.Today).Date;
            var to = (denNgay ?? from).Date;
            return to < from ? (to, from) : (from, to);
        }

        public async Task<IActionResult> Status()
        {
            var rooms = await _roomApi.GetAllAsync();
            return View(rooms);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var room = await _roomApi.GetByIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        [HttpPost]
        [Authorize(Roles = WebRoles.FrontDesk)]
        public async Task<IActionResult> UpdateStatus(string maPhong, QLKSan.Enums.RoomStatus status)
        {
            if (string.IsNullOrEmpty(maPhong))
            {
                return NotFound();
            }

            try
            {
                await _roomApi.UpdateStatusAsync(maPhong, status);
                TempData["SuccessMessage"] = "Cập nhật trạng thái phòng thành công!";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.UserMessage;
            }

            return RedirectToAction(nameof(Details), new { id = maPhong });
        }
    }
}
