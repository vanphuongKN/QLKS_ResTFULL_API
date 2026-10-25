using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Enums;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/invoices")]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly ICheckoutService _checkoutService;
        private readonly IAccountingService _accountingService;

        public InvoicesController(ICheckoutService checkoutService, IAccountingService accountingService)
        {
            _checkoutService = checkoutService;
            _accountingService = accountingService;
        }

        /// <summary>Danh sách hoá đơn (kế toán), lọc theo ngày lập, trạng thái, mã HĐ/mã phiếu/tên khách.</summary>
        [HttpGet]
        [Authorize(Roles = AppRoles.Finance)]
        public async Task<ActionResult<IEnumerable<InvoiceListItemDto>>> GetAll([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay,
            [FromQuery] InvoiceStatus? trangThai, [FromQuery] string search)
        {
            return Ok(await _accountingService.GetInvoicesAsync(tuNgay, denNgay, trangThai, search));
        }

        /// <summary>Chi tiết hoá đơn (xem/in). Thu ngân, kế toán xem mọi hoá đơn; khách hàng chỉ xem hoá đơn của mình.</summary>
        [HttpGet("{maHoaDon}")]
        [Authorize(Roles = AppRoles.Cashier + "," + AppRoles.Customer)]
        public async Task<ActionResult<InvoiceDetailDto>> GetById(string maHoaDon)
        {
            var detail = await _accountingService.GetInvoiceDetailAsync(maHoaDon);
            if (User.IsInRole(AppRoles.Customer) && detail.PhieuDatPhong?.MaKhachHang != User.GetMaKhachHang())
                return Forbid();
            return detail;
        }

        /// <summary>
        /// Lập hoá đơn cho phiếu đặt phòng (kèm mã khuyến mãi nếu có).
        /// Nếu phiếu đã có hoá đơn chưa thanh toán thì tính lại và cập nhật hoá đơn đó.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.Cashier)]
        [Audit("Lập hoá đơn", "Hoá đơn")]
        public async Task<ActionResult<InvoiceDto>> Generate(GenerateInvoiceRequest request)
        {
            var invoice = await _checkoutService.GenerateInvoiceAsync(request.MaDatPhong, User.GetMaNhanVien(), request.MaKhuyenMai);
            return invoice.ToDto();
        }

        /// <summary>
        /// Thanh toán hoá đơn: trả phòng, chuyển phòng sang Đang dọn (tạo việc buồng phòng) và cộng điểm tích luỹ.
        /// DiemSuDung &gt; 0: đổi điểm của khách trừ vào số tiền còn phải trả (ghi nhận thành khoản "Điểm thưởng").
        /// CacKhoan (tối đa 3): tách thanh toán theo phương thức, tổng bằng số tiền còn phải thu. Cọc thừa được ghi hoàn vào sổ cọc.
        /// </summary>
        [HttpPost("{maHoaDon}/payments")]
        [Authorize(Roles = AppRoles.Cashier)]
        [Audit("Thanh toán, trả phòng", "Hoá đơn")]
        public async Task<ActionResult<InvoiceDto>> Pay(string maHoaDon, PaymentRequest request)
        {
            var invoice = await _checkoutService.ProcessPaymentAsync(maHoaDon, request.PhuongThuc, User.GetMaNhanVien(), request.DiemSuDung, request.CacKhoan);
            return invoice.ToDto();
        }

        /// <summary>Thông tin xuất hoá đơn VAT cho công ty (tên, mã số thuế, địa chỉ, email nhận hoá đơn); mã số thuế trống = hoá đơn cá nhân.</summary>
        [HttpPut("{maHoaDon}/company")]
        [Authorize(Roles = AppRoles.Cashier)]
        [Audit("Cập nhật thông tin xuất hoá đơn", "Hoá đơn")]
        public async Task<ActionResult<InvoiceDto>> SetCompany(string maHoaDon, InvoiceCompanyRequest request)
        {
            return (await _checkoutService.SetCompanyInfoAsync(maHoaDon, request)).ToDto();
        }

        /// <summary>Hoàn tiền cho hoá đơn đã thanh toán (không vượt quá tổng giá trị hoá đơn trừ phần đã hoàn).</summary>
        [HttpPost("{maHoaDon}/refunds")]
        [Authorize(Roles = AppRoles.Finance)]
        [Audit("Hoàn tiền", "Hoá đơn")]
        public async Task<ActionResult<InvoiceDetailDto>> Refund(string maHoaDon, RefundRequest request)
        {
            return await _accountingService.RefundAsync(maHoaDon, request.SoTien, request.PhuongThuc, request.LyDo, User.GetMaNhanVien());
        }
    }
}
