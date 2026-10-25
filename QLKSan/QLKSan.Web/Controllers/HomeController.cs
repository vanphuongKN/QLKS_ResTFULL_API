using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Enums;
using QLKSan.Infrastructure;
using QLKSan.Models;
using QLKSan.Models.DTOs;
using System.Diagnostics;

namespace QLKSan.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRoomTypeApiClient _roomTypeApi;
        private readonly IServiceApiClient _serviceApi;
        private readonly IReviewApiClient _reviewApi;
        private readonly HotelInfoProvider _hotelInfo;

        public HomeController(IRoomTypeApiClient roomTypeApi, IServiceApiClient serviceApi, IReviewApiClient reviewApi, HotelInfoProvider hotelInfo)
        {
            _roomTypeApi = roomTypeApi;
            _serviceApi = serviceApi;
            _reviewApi = reviewApi;
            _hotelInfo = hotelInfo;
        }

        // Trang giới thiệu: hạng phòng và dịch vụ lấy từ dữ liệu thật (API không chạy thì vẫn hiển thị trang, chỉ ẩn danh sách)
        public async Task<IActionResult> Index()
        {
            ViewBag.Hotel = await _hotelInfo.GetAsync();
            try
            {
                ViewBag.RoomTypes = (await _roomTypeApi.GetAllAsync()).OrderByDescending(l => l.GiaThueNgay).ToList();
                // Mục "Trải nghiệm": dịch vụ được quản trị đánh dấu giới thiệu và đang phục vụ
                ViewBag.Experiences = (await _serviceApi.GetAllAsync()).Where(s => s.LaTraiNghiem && s.TrangThai == ServiceStatus.Available).Take(3).ToList();
                ViewBag.Reviews = await _reviewApi.GetFeaturedAsync(3);
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException)
            {
                ViewBag.RoomTypes = new List<RoomTypeDto>();
                ViewBag.Experiences = new List<ServiceDto>();
                ViewBag.Reviews = new List<ReviewDto>();
            }
            return View();
        }

        // Chi tiết hạng phòng: bộ ảnh, thông số, tiện nghi, đánh giá; đặt phòng chuyển sang Customer/BookRoom với hạng đã chọn
        [HttpGet]
        public async Task<IActionResult> RoomType(string id, DateTime? checkIn = null, DateTime? checkOut = null)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction(nameof(Index));
            var detail = await _roomTypeApi.GetDetailAsync(id);
            if (detail == null) return NotFound();

            ViewBag.Hotel = await _hotelInfo.GetAsync();
            var ci = checkIn == null || checkIn.Value.Date < DateTime.Today ? DateTime.Today.AddDays(1) : checkIn.Value.Date;
            ViewBag.CheckIn = ci;
            ViewBag.CheckOut = checkOut != null && checkOut.Value.Date > ci ? checkOut.Value.Date : ci.AddDays(2);
            return View(detail);
        }

        // ===== Trang giới thiệu (nội dung tĩnh + thông tin khách sạn từ API) =====
        [HttpGet]
        public async Task<IActionResult> About()
        {
            ViewBag.Hotel = await _hotelInfo.GetAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Dining()
        {
            ViewBag.Hotel = await _hotelInfo.GetAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Spa()
        {
            ViewBag.Hotel = await _hotelInfo.GetAsync();
            return View();
        }

        // Thư viện ảnh: bộ ảnh của từng hạng phòng (quản trị tải lên) + ảnh không gian chung
        [HttpGet]
        public async Task<IActionResult> Gallery()
        {
            var albums = new List<RoomTypeDetailDto>();
            try
            {
                foreach (var lp in (await _roomTypeApi.GetAllAsync()).OrderByDescending(l => l.GiaThueNgay))
                {
                    var detail = await _roomTypeApi.GetDetailAsync(lp.MaLoai);
                    if (detail != null) albums.Add(detail);
                }
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException) { }
            ViewBag.Albums = albums;
            return View();
        }

        // Đặc quyền thành viên: hạng, quyền lợi, cách tích / đổi điểm (theo chương trình cấu hình ở API)
        [HttpGet]
        public async Task<IActionResult> Membership()
        {
            ViewBag.Hotel = await _hotelInfo.GetAsync();
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
