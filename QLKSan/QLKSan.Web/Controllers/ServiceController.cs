using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Infrastructure;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [Authorize(Roles = WebRoles.FrontDesk)]
    public class ServiceController : Controller
    {
        private readonly IServiceApiClient _serviceApi;
        private readonly IBookingApiClient _bookingApi;

        public ServiceController(IServiceApiClient serviceApi, IBookingApiClient bookingApi)
        {
            _serviceApi = serviceApi;
            _bookingApi = bookingApi;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? maPhong = null, string? roomNumber = null)
        {
            ViewBag.Services = await _serviceApi.GetAllAsync();
            var activeStays = await _bookingApi.GetActiveAsync();
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
            var items = services?.Where(item => !string.IsNullOrEmpty(item.MaDichVu) && item.SoLuong > 0).ToList();
            if (string.IsNullOrEmpty(maPhong) || items == null || items.Count == 0)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng chọn ít nhất 1 dịch vụ.";
                return RedirectToAction("Index");
            }

            // Phiếu đang lưu trú của phòng (danh sách đã sắp xếp theo ngày check-in mới nhất)
            var activeStays = await _bookingApi.GetActiveAsync();
            var maDatPhong = activeStays
                .FirstOrDefault(stay => stay.ChiTietDatPhongs.Any(c => c.MaPhong == maPhong))?.MaDatPhong;
            if (string.IsNullOrEmpty(maDatPhong))
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin lưu trú hợp lệ cho phòng này.";
                return RedirectToAction("Index");
            }

            try
            {
                await _bookingApi.OrderServicesAsync(maDatPhong, new OrderServiceRequest { Services = items });
                TempData["SuccessMessage"] = "Đã thêm các dịch vụ thành công!";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thêm dịch vụ. " + ex.UserMessage;
            }

            return RedirectToAction("Index");
        }

        // Huỷ dịch vụ gọi nhầm (bớt số lượng hoặc huỷ hẳn), bắt buộc lý do
        [HttpPost]
        public async Task<IActionResult> VoidService(string maDatPhong, string maSDDV, int soLuong, string lyDo, string maPhong)
        {
            try
            {
                await _bookingApi.VoidServiceAsync(maDatPhong, maSDDV, new VoidServiceRequest { SoLuong = soLuong, LyDo = lyDo });
                TempData["SuccessMessage"] = "Đã huỷ dịch vụ gọi nhầm.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không thể huỷ dịch vụ. " + ex.UserMessage;
            }
            return RedirectToAction("Index", new { maPhong });
        }
    }
}
