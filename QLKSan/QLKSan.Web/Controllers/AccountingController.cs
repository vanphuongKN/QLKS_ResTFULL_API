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

        // Tổng quan kế toán: kỳ chọn so với kỳ liền trước, theo kênh đặt, việc cần đối soát
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay)
        {
            var (from, to) = Range(tuNgay, denNgay);
            ViewBag.TuNgay = from;
            ViewBag.DenNgay = to;
            return View(await _reportApi.GetOverviewAsync(from, to));
        }

        // Đối soát: giao ca của lễ tân (tiền mặt hệ thống / thực tế) và sổ cọc
        [HttpGet]
        public async Task<IActionResult> Reconciliation(DateTime? tuNgay, DateTime? denNgay)
        {
            var (from, to) = Range(tuNgay, denNgay);
            ViewBag.TuNgay = from;
            ViewBag.DenNgay = to;
            ViewBag.Shifts = await _reportApi.GetShiftsAsync(from, to);
            return View(await _reportApi.GetDepositsAsync(from, to));
        }

        // Hoá đơn VAT công ty: ghi số hoá đơn điện tử đã xuất
        [HttpGet]
        public async Task<IActionResult> VatInvoices(bool chuaXuat = false)
        {
            ViewBag.ChuaXuat = chuaXuat;
            var list = await _invoiceApi.GetAllAsync(null, null, InvoiceStatus.Paid, null, congTy: true);
            return View(chuaXuat ? list.Where(h => string.IsNullOrEmpty(h.SoHoaDonDienTu)).ToList() : list);
        }

        [HttpPost]
        public async Task<IActionResult> SetEInvoice(string id, string soHoaDonDienTu, bool chuaXuat = false)
        {
            try
            {
                await _invoiceApi.SetEInvoiceAsync(id, soHoaDonDienTu);
                TempData["SuccessMessage"] = string.IsNullOrWhiteSpace(soHoaDonDienTu) ? $"Đã bỏ số hoá đơn điện tử của {id}." : $"Đã ghi số hoá đơn điện tử {soHoaDonDienTu} cho {id}.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không ghi được số hoá đơn điện tử. " + ex.UserMessage;
            }
            return RedirectToAction(nameof(VatInvoices), new { chuaXuat });
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
