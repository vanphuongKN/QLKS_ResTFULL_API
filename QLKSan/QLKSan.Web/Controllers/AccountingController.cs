using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Enums;
using QLKSan.Infrastructure;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    // Kế toán: tra cứu hoá đơn, hoàn tiền, báo cáo doanh thu / công suất phòng
    [Authorize(Roles = WebRoles.Finance)]
    public class AccountingController : Controller
    {
        private readonly IInvoiceApiClient _invoiceApi;
        private readonly IReportApiClient _reportApi;

        public AccountingController(IInvoiceApiClient invoiceApi, IReportApiClient reportApi)
        {
            _invoiceApi = invoiceApi;
            _reportApi = reportApi;
        }

        // Mặc định: từ đầu tháng đến hôm nay
        private static (DateTime From, DateTime To) Range(DateTime? tuNgay, DateTime? denNgay)
        {
            var from = (tuNgay ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
            var to = (denNgay ?? DateTime.Today).Date;
            return to < from ? (to, from) : (from, to);
        }

        [HttpGet]
        public async Task<IActionResult> Invoices(DateTime? tuNgay, DateTime? denNgay, InvoiceStatus? trangThai, string search)
        {
            ViewBag.TuNgay = tuNgay;
            ViewBag.DenNgay = denNgay;
            ViewBag.TrangThai = trangThai;
            ViewBag.Search = search;
            var invoices = await _invoiceApi.GetAllAsync(tuNgay, denNgay, trangThai, search);
            return View(invoices);
        }

        [HttpGet]
        public async Task<IActionResult> Invoice(string id)
        {
            var detail = await _invoiceApi.GetByIdAsync(id);
            if (detail == null) return NotFound();
            return View(detail);
        }

        [HttpPost]
        public async Task<IActionResult> Refund(string id, decimal soTien, string phuongThuc, string lyDo)
        {
            try
            {
                await _invoiceApi.RefundAsync(id, new RefundRequest { SoTien = soTien, PhuongThuc = phuongThuc, LyDo = lyDo });
                TempData["SuccessMessage"] = $"Đã hoàn {soTien:N0}đ cho hoá đơn {id}.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không thể hoàn tiền. " + ex.UserMessage;
            }
            return RedirectToAction(nameof(Invoice), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Reports(DateTime? tuNgay, DateTime? denNgay)
        {
            var (from, to) = Range(tuNgay, denNgay);
            ViewBag.TuNgay = from;
            ViewBag.DenNgay = to;
            ViewBag.Occupancy = await _reportApi.GetOccupancyAsync(from, to);
            var revenue = await _reportApi.GetRevenueAsync(from, to);
            return View(revenue);
        }

        [HttpGet]
        public async Task<IActionResult> Export(DateTime? tuNgay, DateTime? denNgay)
        {
            var (from, to) = Range(tuNgay, denNgay);
            var (content, contentType, fileName) = await _reportApi.ExportRevenueAsync(from, to);
            return File(content, contentType, fileName ?? $"BaoCaoDoanhThu_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
        }
    }
}
