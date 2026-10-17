using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;

namespace QLKSan.Controllers
{
    public class RoomController : Controller
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        public async Task<IActionResult> Status()
        {
            var rooms = await _roomService.GetAllRoomsAsync();
            return View(rooms);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string maPhong, QLKSan.Enums.RoomStatus status)
        {
            if (string.IsNullOrEmpty(maPhong))
            {
                return NotFound();
            }

            try
            {
                await _roomService.UpdateRoomStatusAsync(maPhong, status);
                TempData["SuccessMessage"] = "Cập nhật trạng thái phòng thành công!";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = maPhong });
        }
    }
}
