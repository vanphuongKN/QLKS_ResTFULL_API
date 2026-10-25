using System;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    // Cấu hình nghiệp vụ do quản lý sửa trên web (bảng CauHinh, mỗi mục một JSON).
    // Mục chưa lưu trong CSDL thì dùng giá trị trong appsettings.json. Program.cs cấp IOptions<HotelInfo / HotelPolicy / LoyaltyPolicy>
    // từ service này nên mọi nơi đang nhận IOptions đều dùng cấu hình mới mà không phải sửa.
    public interface IHotelSettingsService
    {
        HotelInfo GetHotelInfo();
        HotelPolicy GetPolicy();
        LoyaltyPolicy GetLoyalty();

        // Mục nào đang lấy từ CSDL (đã được quản lý chỉnh) và lần cập nhật gần nhất
        (bool Info, bool Policy, bool Loyalty, DateTime? CapNhatLuc) GetStatus();

        void SaveHotelInfo(HotelInfo value, string maNhanVien);
        void SavePolicy(HotelPolicy value, string maNhanVien);
        void SaveLoyalty(LoyaltyPolicy value, string maNhanVien);
    }

    public class HotelSettingsService : IHotelSettingsService
    {
        public const string KhoaThongTin = "HotelInfo";
        public const string KhoaChinhSach = "HotelPolicy";
        public const string KhoaThanhVien = "Loyalty";

        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IOptionsMonitor<HotelInfo> _infoDefault;
        private readonly IOptionsMonitor<HotelPolicy> _policyDefault;
        private readonly IOptionsMonitor<LoyaltyPolicy> _loyaltyDefault;

        public HotelSettingsService(ApplicationDbContext context, IMemoryCache cache, IOptionsMonitor<HotelInfo> infoDefault,
            IOptionsMonitor<HotelPolicy> policyDefault, IOptionsMonitor<LoyaltyPolicy> loyaltyDefault)
        {
            _context = context;
            _cache = cache;
            _infoDefault = infoDefault;
            _policyDefault = policyDefault;
            _loyaltyDefault = loyaltyDefault;
        }

        public HotelInfo GetHotelInfo() => Get(KhoaThongTin, () => _infoDefault.CurrentValue);
        public HotelPolicy GetPolicy() => Get(KhoaChinhSach, () => _policyDefault.CurrentValue);
        public LoyaltyPolicy GetLoyalty() => Get(KhoaThanhVien, () => _loyaltyDefault.CurrentValue);

        public (bool Info, bool Policy, bool Loyalty, DateTime? CapNhatLuc) GetStatus()
        {
            var rows = _context.CauHinhs.AsNoTracking().ToList();
            return (rows.Any(r => r.Khoa == KhoaThongTin), rows.Any(r => r.Khoa == KhoaChinhSach), rows.Any(r => r.Khoa == KhoaThanhVien),
                rows.Count == 0 ? null : rows.Max(r => r.NgayCapNhat));
        }

        public void SaveHotelInfo(HotelInfo v, string maNhanVien)
        {
            if (string.IsNullOrWhiteSpace(v.Ten)) throw new BusinessException("Vui lòng nhập tên khách sạn.");
            Save(KhoaThongTin, v, maNhanVien);
        }

        public void SavePolicy(HotelPolicy v, string maNhanVien)
        {
            if (v.GioNhanPhong is < 0 or > 23 || v.GioTraPhong is < 0 or > 23)
                throw new BusinessException("Giờ nhận / trả phòng phải từ 0 đến 23.");
            v.PhuThuTraMuon = (v.PhuThuTraMuon ?? new()).OrderBy(m => m.DenGio).ToList();
            var gioTruoc = v.GioTraPhong;
            var phanTramTruoc = 0;
            foreach (var m in v.PhuThuTraMuon)
            {
                if (m.DenGio <= gioTruoc || m.DenGio > 23)
                    throw new BusinessException($"Mốc phụ thu trả muộn phải sau giờ trả phòng ({v.GioTraPhong}h), tăng dần và không quá 23h.");
                if (m.PhanTram <= phanTramTruoc || m.PhanTram > 100)
                    throw new BusinessException("Phần trăm phụ thu phải tăng dần theo mốc giờ và không quá 100%.");
                gioTruoc = m.DenGio;
                phanTramTruoc = m.PhanTram;
            }
            if (v.PhanTramTienCocVangLai is < 0 or > 100) throw new BusinessException("Tiền cọc khách vãng lai phải từ 0 đến 100%.");
            Save(KhoaChinhSach, v, maNhanVien);
        }

        public void SaveLoyalty(LoyaltyPolicy v, string maNhanVien)
        {
            if (v.SoTienMotDiem < 1000) throw new BusinessException("Số tiền cho 1 điểm phải từ 1.000đ.");
            if (v.GiaTriMotDiem < 1) throw new BusinessException("Giá trị 1 điểm phải từ 1đ.");
            if (v.DiemDoiToiThieu < 1) throw new BusinessException("Số điểm đổi tối thiểu phải từ 1.");
            v.Hangs = (v.Hangs ?? new()).Where(h => !string.IsNullOrWhiteSpace(h.Ten)).OrderBy(h => h.DiemToiThieu).ToList();
            if (v.Hangs.Count == 0) throw new BusinessException("Cần ít nhất một hạng thành viên.");
            if (v.Hangs[0].DiemToiThieu != 0) throw new BusinessException("Hạng thấp nhất phải bắt đầu từ 0 điểm.");
            for (var i = 0; i < v.Hangs.Count; i++)
            {
                var h = v.Hangs[i];
                h.Ten = h.Ten.Trim();
                h.QuyenLoi = (h.QuyenLoi ?? new()).Select(q => q?.Trim()).Where(q => !string.IsNullOrEmpty(q)).ToList();
                if (h.PhanTramGiamPhong is < 0 or > 50) throw new BusinessException($"Ưu đãi hạng {h.Ten} phải từ 0 đến 50%.");
                if (i > 0 && h.DiemToiThieu == v.Hangs[i - 1].DiemToiThieu) throw new BusinessException("Mỗi hạng phải có mốc điểm khác nhau.");
            }
            if (v.Hangs.Select(h => h.Ten).Distinct(StringComparer.OrdinalIgnoreCase).Count() != v.Hangs.Count)
                throw new BusinessException("Tên các hạng không được trùng nhau.");
            Save(KhoaThanhVien, v, maNhanVien);
        }

        private T Get<T>(string khoa, Func<T> macDinh)
            => _cache.GetOrCreate("cau-hinh:" + khoa, _ =>
            {
                var row = _context.CauHinhs.AsNoTracking().FirstOrDefault(c => c.Khoa == khoa);
                return row == null ? macDinh() : JsonSerializer.Deserialize<T>(row.GiaTri);
            });

        private void Save<T>(string khoa, T value, string maNhanVien)
        {
            var row = _context.CauHinhs.FirstOrDefault(c => c.Khoa == khoa);
            if (row == null)
            {
                row = new CauHinh { Khoa = khoa };
                _context.CauHinhs.Add(row);
            }
            row.GiaTri = JsonSerializer.Serialize(value);
            row.NgayCapNhat = DateTime.Now;
            row.MaNhanVien = maNhanVien;
            _context.SaveChanges();
            _cache.Remove("cau-hinh:" + khoa);
        }
    }
}
