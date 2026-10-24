using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Enums;
using QLKSan.Infrastructure;
using QLKSan.Models;
using System.Diagnostics;

namespace QLKSan.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRoomTypeApiClient _roomTypeApi;
        private readonly IServiceApiClient _serviceApi;

        public HomeController(IRoomTypeApiClient roomTypeApi, IServiceApiClient serviceApi)
        {
            _roomTypeApi = roomTypeApi;
            _serviceApi = serviceApi;
        }

        // Trang giới thiệu: hạng phòng và dịch vụ lấy từ dữ liệu thật (API không chạy thì vẫn hiển thị trang, chỉ ẩn danh sách)
        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.RoomTypes = (await _roomTypeApi.GetAllAsync()).OrderByDescending(l => l.GiaThueNgay).ToList();
                ViewBag.Services = (await _serviceApi.GetAllAsync()).Where(s => s.TrangThai == ServiceStatus.Available).ToList();
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException)
            {
                ViewBag.RoomTypes = new List<QLKSan.Models.DTOs.RoomTypeDto>();
                ViewBag.Services = new List<QLKSan.Models.DTOs.ServiceDto>();
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
