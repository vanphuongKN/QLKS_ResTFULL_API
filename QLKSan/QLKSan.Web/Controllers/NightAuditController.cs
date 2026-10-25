using System;
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
    // Chốt ngày: kế toán hoặc lễ tân chốt lần lượt từng ngày đã qua; quản lý mở lại ngày chốt gần nhất
    [Authorize(Roles = WebRoles.Cashier)]
    public class NightAuditController : Controller
    {
        private readonly INightAuditApiClient _nightAuditApi;

        public NightAuditController(INightAuditApiClient nightAuditApi)
        {
            _nightAuditApi = nightAuditApi;
        }

        // Xem trước ngày cần chốt (hoặc ngày chọn) và danh sách ngày đã chốt — doanh thu theo đêm
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? ngay, DateTime? tuNgay, DateTime? denNgay)
        {
            var to = (denNgay ?? DateTime.Today).Date;
            var from = (tuNgay ?? to.AddDays(-30)).Date;
            if (to < from) (from, to) = (to, from);
            ViewBag.TuNgay = from;
            ViewBag.DenNgay = to;
            ViewBag.DaChot = await _nightAuditApi.GetAllAsync(from, to);
            return View(await _nightAuditApi.PreviewAsync(ngay));
        }

        // khongDen: mọi phiếu chưa nhận phòng hiển thị; huy: phiếu còn đánh dấu huỷ (bỏ đánh dấu = khách báo đến muộn)
        [HttpPost]
        public async Task<IActionResult> Close(DateTime ngay, List<string> khongDen, List<string> huy, string ghiChu)
        {
            var dsHuy = new HashSet<string>(huy ?? new List<string>());
            try
            {
                var chot = await _nightAuditApi.CloseAsync(new CloseDayRequest
                {
                    Ngay = ngay,
                    GiuLai = (khongDen ?? new List<string>()).Where(m => !dsHuy.Contains(m)).ToList(),
                    GhiChu = ghiChu
                });
                TempData["SuccessMessage"] = $"Đã chốt ngày {chot.Ngay:dd/MM/yyyy}: {chot.SoLieu.DemPhongBan} đêm phòng, tiền phòng {chot.SoLieu.TienPhong:N0} ₫"
                    + (chot.SoLieu.KhachKhongDen > 0 ? $", huỷ {chot.SoLieu.KhachKhongDen} phiếu khách không đến." : ".");
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không chốt được ngày. " + ex.UserMessage;
                return RedirectToAction(nameof(Index), new { ngay = ngay.ToString("yyyy-MM-dd") });
            }
        }

        // Bản in số liệu một ngày đã chốt
        [HttpGet]
        public async Task<IActionResult> Detail(DateTime ngay)
        {
            var chot = await _nightAuditApi.GetAsync(ngay);
            if (chot == null)
            {
                TempData["ErrorMessage"] = $"Ngày {ngay:dd/MM/yyyy} chưa chốt.";
                return RedirectToAction(nameof(Index));
            }
            return View(chot);
        }

        [HttpPost]
        [Authorize(Roles = WebRoles.Admin)]
        public async Task<IActionResult> Reopen(DateTime ngay, string lyDo)
        {
            try
            {
                await _nightAuditApi.ReopenAsync(ngay, lyDo);
                TempData["SuccessMessage"] = $"Đã mở lại ngày {ngay:dd/MM/yyyy}. Sửa xong hãy chốt lại.";
                return RedirectToAction(nameof(Index), new { ngay = ngay.ToString("yyyy-MM-dd") });
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không mở lại được ngày chốt. " + ex.UserMessage;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
