using Microsoft.AspNetCore.Mvc;
using QLKSan.Infrastructure;

namespace QLKSan.ViewComponents
{
    // Footer trang khách: liên hệ + chính sách lưu trú lấy từ GET /api/hotel
    public class HotelFooterViewComponent : ViewComponent
    {
        private readonly HotelInfoProvider _hotelInfo;

        public HotelFooterViewComponent(HotelInfoProvider hotelInfo)
        {
            _hotelInfo = hotelInfo;
        }

        public async Task<IViewComponentResult> InvokeAsync() => View(await _hotelInfo.GetAsync());
    }
}
