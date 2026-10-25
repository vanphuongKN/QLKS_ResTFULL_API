using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QLKSan.Common;
using QLKSan.Models.DTOs;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/hotel")]
    [AllowAnonymous]
    public class HotelController : ControllerBase
    {
        private readonly HotelInfo _info;
        private readonly HotelPolicy _policy;
        private readonly LoyaltyPolicy _loyalty;

        public HotelController(IOptions<HotelInfo> info, IOptions<HotelPolicy> policy, IOptions<LoyaltyPolicy> loyalty)
        {
            _info = info.Value;
            _policy = policy.Value;
            _loyalty = loyalty.Value;
        }

        /// <summary>Thông tin liên hệ, chính sách lưu trú (giờ nhận/trả phòng, phụ thu trả muộn, huỷ phòng) và chương trình thành viên — công khai.</summary>
        [HttpGet]
        public ActionResult<HotelInfoDto> Get()
        {
            return new HotelInfoDto
            {
                Ten = _info.Ten,
                MoTaNgan = _info.MoTaNgan,
                DiaChi = _info.DiaChi,
                Hotline = _info.Hotline,
                Email = _info.Email,
                BanDoUrl = _info.BanDoUrl,
                GioNhanPhong = $"{_policy.GioNhanPhong:00}:00",
                GioTraPhong = $"{_policy.GioTraPhong:00}:00",
                PhuThuTraMuon = _policy.PhuThuTraMuon
                    .OrderBy(m => m.DenGio)
                    .Select(m => new LateCheckoutFeeDto { DenGio = $"{m.DenGio:00}:00", PhanTram = m.PhanTram })
                    .ToList(),
                // Khớp quy tắc ReceptionService.CancelBookingAsync: khách tự huỷ được trước ngày nhận phòng
                ChinhSachHuy = "Huỷ miễn phí trước ngày nhận phòng. Từ ngày nhận phòng, vui lòng liên hệ lễ tân để được hỗ trợ.",
                ThanhVien = new LoyaltyProgramDto
                {
                    SoTienMotDiem = _loyalty.SoTienMotDiem,
                    GiaTriMotDiem = _loyalty.GiaTriMotDiem,
                    DiemDoiToiThieu = _loyalty.DiemDoiToiThieu,
                    Hangs = _loyalty.Hangs.OrderBy(h => h.DiemToiThieu)
                        .Select(h => new MembershipTierDto { Ten = h.Ten, DiemToiThieu = h.DiemToiThieu, PhanTramGiamPhong = h.PhanTramGiamPhong, QuyenLoi = h.QuyenLoi.ToList() })
                        .ToList()
                }
            };
        }
    }
}
