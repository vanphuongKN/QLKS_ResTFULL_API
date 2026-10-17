using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Enums;

namespace QLKSan.Controllers
{
    public class HousekeepingController : Controller
    {
        private readonly IHousekeepingService _housekeepingService;

        public HousekeepingController(IHousekeepingService housekeepingService)
        {
            _housekeepingService = housekeepingService;
        }

        [HttpGet]
        public async Task<IActionResult> Tasks()
        {
            ViewBag.CleaningRooms = await _housekeepingService.GetRoomsNeedingCleaningAsync();
            ViewBag.CountCleaning = await _housekeepingService.GetCountByStatusAsync(RoomStatus.Cleaning);
            ViewBag.CountAvailable = await _housekeepingService.GetCountByStatusAsync(RoomStatus.Available);
            ViewBag.CountOccupied = await _housekeepingService.GetCountByStatusAsync(RoomStatus.Occupied);
            ViewBag.AttentionRoom = await _housekeepingService.GetAttentionRoomAsync();
            ViewBag.VipCount = await _housekeepingService.GetVipCountAsync();
            ViewBag.HousekeepingStaff = await _housekeepingService.GetHousekeepingStaffAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> MarkClean(string maPhong)
        {
            bool success = await _housekeepingService.MarkRoomAsCleanAsync(maPhong);
            if (success)
                TempData["SuccessMessage"] = $"Phòng đã được chuyển sang trạng thái Trống (Sẵn sàng)!";
            else
                TempData["ErrorMessage"] = "Không thể cập nhật trạng thái phòng.";

            return RedirectToAction("Tasks");
        }
    }
}
