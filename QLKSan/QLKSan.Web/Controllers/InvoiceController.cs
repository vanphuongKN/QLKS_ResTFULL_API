using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Infrastructure;

namespace QLKSan.Controllers
{
    // Bản in hoá đơn (trình duyệt "In" / "Lưu thành PDF").
    // Thu ngân, kế toán in mọi hoá đơn; khách hàng chỉ mở được hoá đơn của mình (API kiểm tra quyền sở hữu)
    [Authorize(Roles = WebRoles.Cashier + "," + WebRoles.Customer)]
    public class InvoiceController : Controller
    {
        private readonly IInvoiceApiClient _invoiceApi;

        public InvoiceController(IInvoiceApiClient invoiceApi)
        {
            _invoiceApi = invoiceApi;
        }

        [HttpGet]
        public async Task<IActionResult> Print(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            try
            {
                var detail = await _invoiceApi.GetByIdAsync(id);
                if (detail == null) return NotFound();
                return View(detail);
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                return Forbid();
            }
        }
    }
}
