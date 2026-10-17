using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;

namespace QLKSan.Controllers
{
    public class ServiceOrderItem
    {
        public string MaDichVu { get; set; }
        public int SoLuong { get; set; }
    }

    public class ServiceController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public ServiceController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? maPhong = null, string? roomNumber = null)
        {
            ViewBag.Services = await _serviceManager.GetAllServicesAsync();
            var activeStays = await _serviceManager.GetActiveStaysAsync();
            ViewBag.ActiveStays = activeStays;

            if (string.IsNullOrEmpty(maPhong) && !string.IsNullOrEmpty(roomNumber))
            {
                var matchingStay = activeStays.FirstOrDefault(stay => 
                    stay.ChiTietDatPhongs?.FirstOrDefault()?.Phong?.SoPhong == roomNumber);
                if (matchingStay != null)
                {
                    maPhong = matchingStay.ChiTietDatPhongs.FirstOrDefault()?.Phong?.MaPhong;
                }
            }

            ViewBag.SelectedRoom = maPhong;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> OrderService(string maPhong, List<ServiceOrderItem> services)
        {
            if (string.IsNullOrEmpty(maPhong) || services == null || services.Count == 0)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng chọn ít nhất 1 dịch vụ.";
                return RedirectToAction("Index");
            }

            string maDatPhong = await _serviceManager.GetActiveBookingIdByRoomAsync(maPhong);
            if (string.IsNullOrEmpty(maDatPhong))
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin lưu trú hợp lệ cho phòng này.";
                return RedirectToAction("Index");
            }

            bool anySuccess = false;
            foreach (var item in services)
            {
                if (!string.IsNullOrEmpty(item.MaDichVu) && item.SoLuong > 0)
                {
                    bool ok = await _serviceManager.OrderServiceAsync(maDatPhong, item.MaDichVu, item.SoLuong);
                    if (ok) anySuccess = true;
                }
            }

            if (anySuccess)
            {
                TempData["SuccessMessage"] = "Đã thêm các dịch vụ thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thêm dịch vụ.";
            }

            return RedirectToAction("Index");
        }
    }
}
