using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Infrastructure;

namespace QLKSan.ViewComponents
{
    // Cảnh báo trên trang lễ tân / kế toán / quản lý khi còn ngày đã qua chưa chốt
    public class NightAuditBannerViewComponent : ViewComponent
    {
        private readonly INightAuditApiClient _nightAuditApi;

        public NightAuditBannerViewComponent(INightAuditApiClient nightAuditApi)
        {
            _nightAuditApi = nightAuditApi;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!WebRoles.Cashier.Split(',').Any(UserClaimsPrincipal.IsInRole)) return Content("");
            try
            {
                var status = await _nightAuditApi.GetStatusAsync();
                return status.SoNgayChuaChot > 0 ? View(status) : Content("");
            }
            catch (ApiException)
            {
                // Không lấy được tình trạng thì bỏ qua cảnh báo, không làm hỏng trang
                return Content("");
            }
        }
    }
}
