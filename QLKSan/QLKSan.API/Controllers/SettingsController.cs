using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/settings")]
    [Authorize(Roles = AppRoles.Admin)]
    public class SettingsController : ControllerBase
    {
        private readonly IHotelSettingsService _settings;

        public SettingsController(IHotelSettingsService settings)
        {
            _settings = settings;
        }

        /// <summary>Cấu hình nghiệp vụ hiện hành: thông tin khách sạn, chính sách lưu trú, chương trình thành viên.</summary>
        [HttpGet]
        public ActionResult<HotelSettingsDto> Get() => Current();

        /// <summary>Lưu thông tin liên hệ của khách sạn (footer, trang khách).</summary>
        [HttpPut("hotel-info")]
        [Audit("Sửa thông tin khách sạn", "Cấu hình")]
        public ActionResult<HotelSettingsDto> SaveHotelInfo(HotelInfoSettingsDto request)
        {
            _settings.SaveHotelInfo(request.ToEntity(), User.GetMaNhanVien());
            return Current();
        }

        /// <summary>Lưu chính sách lưu trú: giờ nhận/trả phòng, phụ thu trả muộn, tự xác nhận phiếu, cọc vãng lai, tự huỷ khách không đến.</summary>
        [HttpPut("policy")]
        [Audit("Sửa chính sách lưu trú", "Cấu hình")]
        public ActionResult<HotelSettingsDto> SavePolicy(HotelPolicySettingsDto request)
        {
            _settings.SavePolicy(request.ToEntity(), User.GetMaNhanVien());
            return Current();
        }

        /// <summary>Lưu chương trình thành viên: cách tích / đổi điểm và các hạng.</summary>
        [HttpPut("loyalty")]
        [Audit("Sửa chương trình thành viên", "Cấu hình")]
        public ActionResult<HotelSettingsDto> SaveLoyalty(LoyaltySettingsDto request)
        {
            _settings.SaveLoyalty(request.ToEntity(), User.GetMaNhanVien());
            return Current();
        }

        private HotelSettingsDto Current()
        {
            var st = _settings.GetStatus();
            return new HotelSettingsDto
            {
                ThongTin = _settings.GetHotelInfo().ToDto(),
                ChinhSach = _settings.GetPolicy().ToDto(),
                ThanhVien = _settings.GetLoyalty().ToDto(),
                ThongTinDaTuyChinh = st.Info,
                ChinhSachDaTuyChinh = st.Policy,
                ThanhVienDaTuyChinh = st.Loyalty,
                CapNhatLuc = st.CapNhatLuc
            };
        }
    }
}
