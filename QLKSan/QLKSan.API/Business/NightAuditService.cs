using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Messaging;
using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;
using QLKSan.Models.Events;

namespace QLKSan.Business
{
    // Chốt ngày (night audit): chốt lần lượt từng ngày đã qua, ghi nhận tiền phòng theo đêm và cọc giữ lại,
    // xử lý khách không đến, lưu ảnh chụp số liệu ngày. Quản lý mở lại được ngày chốt gần nhất.
    public interface INightAuditService
    {
        Task<NightAuditStatusDto> GetStatusAsync();

        // ngay = null: ngày kế tiếp phải chốt (hoặc hôm qua nếu đã chốt hết)
        Task<NightAuditPreviewDto> PreviewAsync(DateTime? ngay);
        Task<DayCloseDto> CloseAsync(DateTime ngay, IEnumerable<string> giuLai, string ghiChu, string maNhanVien);
        Task<List<DayCloseDto>> ListAsync(DateTime tuNgay, DateTime denNgay);
        Task<DayCloseDto> GetAsync(DateTime ngay);
        Task ReopenAsync(DateTime ngay, string lyDo, string nguoiMo);
    }

    public class NightAuditService : INightAuditService
    {
        public const string LoaiTienPhong = "Tiền phòng";
        public const string LoaiCocGiuLai = "Cọc giữ lại";

        private readonly ApplicationDbContext _context;
        private readonly NightAuditPolicy _policy;
        private readonly IEventOutbox _events;

        public NightAuditService(ApplicationDbContext context, IOptions<NightAuditPolicy> policy, IEventOutbox events)
        {
            _context = context;
            _policy = policy.Value;
            _events = events;
        }

        // Ngày đã chốt gần nhất (dùng để khoá sửa số liệu của các ngày đã chốt)
        public static async Task<DateTime?> NgayChotGanNhatAsync(ApplicationDbContext context)
            => await context.ChotNgays.MaxAsync(c => (DateTime?)c.Ngay);

        public async Task<NightAuditStatusDto> GetStatusAsync()
        {
            var ganNhat = await NgayChotGanNhatAsync(_context);
            var batDau = _policy.NgayBatDau.Date;
            var canChot = ganNhat == null ? batDau : (ganNhat.Value.AddDays(1) > batDau ? ganNhat.Value.AddDays(1) : batDau);
            var today = DateTime.Today;
            return new NightAuditStatusDto
            {
                NgayBatDau = batDau,
                NgayChotGanNhat = ganNhat,
                NgayCanChot = canChot < today ? canChot : null,
                SoNgayChuaChot = canChot < today ? (today - canChot).Days : 0
            };
        }

        public async Task<NightAuditPreviewDto> PreviewAsync(DateTime? ngay)
        {
            var status = await GetStatusAsync();
            var d = (ngay ?? status.NgayCanChot ?? DateTime.Today.AddDays(-1)).Date;
            var dto = new NightAuditPreviewDto { Ngay = d, TinhTrang = status };

            if (await _context.ChotNgays.AnyAsync(c => c.Ngay == d))
                dto.LyDoKhongChot = $"Ngày {d:dd/MM/yyyy} đã chốt.";
            else
                dto.LyDoKhongChot = KiemTraThuTu(d, status);
            dto.ChoPhepChot = dto.LyDoKhongChot == null;

            dto.SoLieu = await ComputeAsync(d, new List<PhieuDatPhong>());
            dto.KhachKhongDen = (await LoadNoShowsAsync(d)).Select(p => new NoShowCandidateDto
            {
                MaDatPhong = p.MaDatPhong,
                HoTenKhachHang = TenKhach(p),
                SoPhong = p.ChiTietDatPhongs.FirstOrDefault()?.Phong?.SoPhong,
                NgayNhan = p.NgayCheckInDuKien.Date,
                TienCoc = p.TienCoc,
                GioDenDuKien = p.GioDenDuKien
            }).ToList();

            // Cảnh báo: khách quá hạn trả, hoá đơn chưa thanh toán, nhân viên có thu tiền trong ngày mà chưa chốt ca
            var next = d.AddDays(1);
            dto.QuaHanTra = (await _context.PhieuDatPhongs
                    .Include(p => p.KhachHang)
                    .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong)
                    .Where(p => p.TrangThai == BookingStatus.CheckedIn && p.NgayCheckOutDuKien < next)
                    .ToListAsync())
                .OrderBy(p => p.NgayCheckOutDuKien)
                .Select(p => $"{p.MaDatPhong} · P{p.ChiTietDatPhongs.FirstOrDefault()?.Phong?.SoPhong} · {TenKhach(p)} (trả {p.NgayCheckOutDuKien:dd/MM})")
                .ToList();
            dto.HoaDonChuaThanhToan = await _context.HoaDons.CountAsync(h => h.TrangThai == InvoiceStatus.Unpaid && h.NgayLapHoaDon < next);

            var lanThuCuoi = (await _context.ThanhToans.Where(t => t.NgayThanhToan >= d && t.NgayThanhToan < next && t.MaNhanVien != null)
                    .Select(t => new { t.MaNhanVien, ThoiGian = t.NgayThanhToan }).ToListAsync())
                .Concat(await _context.GiaoDichCocs.Where(g => g.ThoiGian >= d && g.ThoiGian < next && g.MaNhanVien != null)
                    .Select(g => new { g.MaNhanVien, g.ThoiGian }).ToListAsync())
                .GroupBy(x => x.MaNhanVien)
                .ToDictionary(g => g.Key, g => g.Max(x => x.ThoiGian));
            if (lanThuCuoi.Count > 0)
            {
                var maNV = lanThuCuoi.Keys.ToList();
                var caCuoi = (await _context.GiaoCas.Where(g => maNV.Contains(g.MaNhanVien)).GroupBy(g => g.MaNhanVien)
                    .Select(g => new { g.Key, KetThuc = g.Max(x => x.KetThuc) }).ToListAsync()).ToDictionary(x => x.Key, x => x.KetThuc);
                var chuaChot = lanThuCuoi.Where(x => !caCuoi.TryGetValue(x.Key, out var kt) || kt < x.Value).Select(x => x.Key).ToList();
                dto.CaChuaChot = await _context.NhanViens.Where(n => chuaChot.Contains(n.MaNhanVien))
                    .OrderBy(n => n.TenNhanVien).Select(n => n.TenNhanVien).ToListAsync();
            }
            return dto;
        }

        public async Task<DayCloseDto> CloseAsync(DateTime ngay, IEnumerable<string> giuLai, string ghiChu, string maNhanVien)
        {
            var d = ngay.Date;
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                if (await _context.ChotNgays.AnyAsync(c => c.Ngay == d))
                    throw new ConflictException($"Ngày {d:dd/MM/yyyy} đã được chốt.");
                var loi = KiemTraThuTu(d, await GetStatusAsync());
                if (loi != null) throw new BusinessException(loi);

                // Khách không đến: huỷ và giữ cọc, trừ phiếu lễ tân bỏ đánh dấu (khách báo đến muộn)
                var giu = new HashSet<string>(giuLai ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
                var nguoiChot = (await _context.NhanViens.FindAsync(maNhanVien))?.TenNhanVien ?? maNhanVien;
                var khongDen = (await LoadNoShowsAsync(d)).Where(p => !giu.Contains(p.MaDatPhong)).ToList();
                foreach (var p in khongDen)
                {
                    p.TrangThai = BookingStatus.Cancelled;
                    p.NgayHuy = DateTime.Now;
                    p.GhiChu = BookingNotes.Append(p.GhiChu, $"[Không đến - chốt ngày {d:dd/MM/yyyy} - {nguoiChot}]"
                        + (p.TienCoc > 0 ? $" [Giữ cọc {p.TienCoc:N0}đ]" : ""));
                    await _events.AddBookingEventAsync(BookingEventTypes.HuyPhieu, p.MaDatPhong, $"Khách không đến ngày {p.NgayCheckInDuKien:dd/MM/yyyy}");
                }

                var soLieu = await ComputeAsync(d, khongDen);
                soLieu.KhachKhongDen = khongDen.Count;

                // Ghi doanh thu theo đêm và đánh dấu cọc đã ghi nhận
                foreach (var dem in soLieu.CacDem)
                    _context.DoanhThuDems.Add(new DoanhThuDem { Ngay = d, MaDatPhong = dem.MaDatPhong, MaPhong = dem.MaPhong, Loai = LoaiTienPhong, SoTien = dem.Gia, GhiChu = dem.QuyTac });
                var maCoc = soLieu.CocGiuLai.Select(c => c.MaDatPhong).ToList();
                foreach (var p in await _context.PhieuDatPhongs.Where(p => maCoc.Contains(p.MaDatPhong)).ToListAsync())
                {
                    p.NgayGhiNhanCocGiuLai = d;
                    _context.DoanhThuDems.Add(new DoanhThuDem { Ngay = d, MaDatPhong = p.MaDatPhong, Loai = LoaiCocGiuLai, SoTien = p.TienCoc });
                }

                var chot = new ChotNgay
                {
                    Ngay = d,
                    ThoiGianChot = DateTime.Now,
                    MaNhanVien = maNhanVien,
                    TongSoPhong = soLieu.TongSoPhong,
                    DemPhongBan = soLieu.DemPhongBan,
                    CongSuat = soLieu.CongSuat,
                    TienPhong = soLieu.TienPhong,
                    ADR = soLieu.ADR,
                    RevPAR = soLieu.RevPAR,
                    DoanhThuKhac = soLieu.DoanhThuKhac,
                    TienDichVu = soLieu.TienDichVu,
                    SoHoaDonThanhToan = soLieu.SoHoaDonThanhToan,
                    TongHoaDonThanhToan = soLieu.TongHoaDonThanhToan,
                    KhachDen = soLieu.KhachDen,
                    KhachDi = soLieu.KhachDi,
                    KhachKhongDen = soLieu.KhachKhongDen,
                    GhiChu = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim(),
                    ChiTiet = JsonSerializer.Serialize(new ChiTietChot { TheoPhuongThuc = soLieu.TheoPhuongThuc, CacDem = soLieu.CacDem, CocGiuLai = soLieu.CocGiuLai })
                };
                _context.ChotNgays.Add(chot);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                await _context.Entry(chot).Reference(c => c.NhanVien).LoadAsync();
                return ToDto(chot);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<DayCloseDto>> ListAsync(DateTime tuNgay, DateTime denNgay)
        {
            var from = tuNgay.Date;
            var to = denNgay.Date;
            return (await _context.ChotNgays.Include(c => c.NhanVien).Where(c => c.Ngay >= from && c.Ngay <= to)
                    .OrderByDescending(c => c.Ngay).ToListAsync())
                .Select(ToDto).ToList();
        }

        public async Task<DayCloseDto> GetAsync(DateTime ngay)
        {
            var d = ngay.Date;
            var chot = await _context.ChotNgays.Include(c => c.NhanVien).FirstOrDefaultAsync(c => c.Ngay == d)
                ?? throw new NotFoundException($"Ngày {d:dd/MM/yyyy} chưa chốt.");
            return ToDto(chot);
        }

        public async Task ReopenAsync(DateTime ngay, string lyDo, string nguoiMo)
        {
            var d = ngay.Date;
            if (string.IsNullOrWhiteSpace(lyDo))
                throw new BusinessException("Vui lòng nhập lý do mở lại.");
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var chot = await _context.ChotNgays.FindAsync(d)
                    ?? throw new NotFoundException($"Ngày {d:dd/MM/yyyy} chưa chốt.");
                if (await _context.ChotNgays.AnyAsync(c => c.Ngay > d))
                    throw new ConflictException("Chỉ mở lại được ngày chốt gần nhất.");

                // Gỡ doanh thu đã ghi và trả cọc giữ lại về trạng thái chưa ghi nhận; khách không đến đã huỷ giữ nguyên
                _context.DoanhThuDems.RemoveRange(await _context.DoanhThuDems.Where(x => x.Ngay == d).ToListAsync());
                foreach (var p in await _context.PhieuDatPhongs.Where(p => p.NgayGhiNhanCocGiuLai == d).ToListAsync())
                {
                    p.NgayGhiNhanCocGiuLai = null;
                    p.GhiChu = BookingNotes.Append(p.GhiChu, $"[Mở lại chốt ngày {d:dd/MM/yyyy} - {nguoiMo}] {lyDo.Trim()}");
                }
                _context.ChotNgays.Remove(chot);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Chỉ chốt ngày đã qua, từ ngày bắt đầu áp dụng, lần lượt từng ngày
        private static string KiemTraThuTu(DateTime d, NightAuditStatusDto status)
        {
            if (d >= DateTime.Today) return "Chỉ chốt được ngày đã qua (từ hôm qua trở về trước).";
            if (d < status.NgayBatDau) return $"Chốt ngày áp dụng từ {status.NgayBatDau:dd/MM/yyyy}; ngày trước mốc này không cần chốt.";
            if (status.NgayCanChot != null && d != status.NgayCanChot.Value)
                return $"Phải chốt lần lượt: ngày cần chốt tiếp theo là {status.NgayCanChot:dd/MM/yyyy}.";
            return null;
        }

        // Phiếu có ngày nhận phòng tới ngày chốt mà vẫn chưa nhận phòng
        private Task<List<PhieuDatPhong>> LoadNoShowsAsync(DateTime d)
        {
            var next = d.AddDays(1);
            return _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong)
                .Where(p => (p.TrangThai == BookingStatus.Pending || p.TrangThai == BookingStatus.Approved) && p.NgayCheckInDuKien < next)
                .OrderBy(p => p.NgayCheckInDuKien).ThenBy(p => p.MaDatPhong)
                .ToListAsync();
        }

        // Số liệu một ngày. khongDen: phiếu sẽ huỷ ở lần chốt này (cọc của chúng được ghi nhận luôn)
        private async Task<NightAuditFiguresDto> ComputeAsync(DateTime d, List<PhieuDatPhong> khongDen)
        {
            var next = d.AddDays(1);
            var dto = new NightAuditFiguresDto { Ngay = d, TongSoPhong = await _context.Phongs.CountAsync() };

            // Đêm phòng bán: khách đã nhận phòng trước hết ngày d và (đang ở, hoặc đã trả sau đêm d theo số đêm tính tiền)
            var luuTru = await _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong)
                .Where(p => p.NgayCheckInThucTe != null && p.NgayCheckInThucTe < next
                         && (p.TrangThai == BookingStatus.CheckedIn || (p.TrangThai == BookingStatus.CheckedOut && p.NgayCheckOutThucTe >= d)))
                .ToListAsync();
            var rules = await _context.QuyTacGias.Where(q => q.KichHoat).ToListAsync();
            foreach (var p in luuTru)
            {
                var ngayNhan = p.NgayCheckInThucTe.Value;
                if (p.TrangThai == BookingStatus.CheckedOut)
                {
                    var soDem = BillingCalculator.TinhSoDem(ngayNhan, p.NgayCheckOutThucTe.Value);
                    if (d >= ngayNhan.Date.AddDays(soDem)) continue;
                }
                var ct = p.ChiTietDatPhongs.FirstOrDefault();
                if (ct == null) continue;
                var gia = PricingCalculator.PriceNights(ct.GiaThueThucTe, ct.Phong?.MaLoai, d, 1, rules).First();
                dto.CacDem.Add(new NightRoomRevenueDto
                {
                    MaDatPhong = p.MaDatPhong,
                    MaPhong = ct.MaPhong,
                    SoPhong = ct.Phong?.SoPhong,
                    HoTenKhachHang = TenKhach(p),
                    Gia = gia.Gia,
                    QuyTac = gia.QuyTac
                });
            }
            dto.CacDem = dto.CacDem.OrderBy(x => x.SoPhong).ToList();
            dto.DemPhongBan = dto.CacDem.Count;
            dto.TienPhong = dto.CacDem.Sum(x => x.Gia);
            dto.CongSuat = dto.TongSoPhong == 0 ? 0 : Math.Round(100m * dto.DemPhongBan / dto.TongSoPhong, 1);
            dto.ADR = dto.DemPhongBan == 0 ? 0 : Math.Round(dto.TienPhong / dto.DemPhongBan, 0);
            dto.RevPAR = dto.TongSoPhong == 0 ? 0 : Math.Round(dto.TienPhong / dto.TongSoPhong, 0);

            // Cọc giữ lại chưa ghi nhận: phiếu đã huỷ (huỷ hoặc ngày nhận dự kiến không sau ngày d) còn tiền cọc
            var coc = await _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Where(p => p.TrangThai == BookingStatus.Cancelled && p.TienCoc > 0 && p.NgayGhiNhanCocGiuLai == null && p.NgayHuy != null
                         && (p.NgayHuy < next || p.NgayCheckInDuKien < next))
                .ToListAsync();
            var maKhongDen = khongDen.Select(p => p.MaDatPhong).ToHashSet();
            dto.CocGiuLai = coc.Where(p => !maKhongDen.Contains(p.MaDatPhong)).Concat(khongDen.Where(p => p.TienCoc > 0))
                .OrderBy(p => p.MaDatPhong)
                .Select(p => new DepositHoldingDto
                {
                    MaDatPhong = p.MaDatPhong,
                    HoTenKhachHang = TenKhach(p),
                    NgayNhan = p.NgayCheckInDuKien.Date,
                    TrangThai = BookingStatus.Cancelled,
                    TienCoc = p.TienCoc
                }).ToList();
            dto.DoanhThuKhac = dto.CocGiuLai.Sum(x => x.TienCoc);

            dto.TienDichVu = await _context.SuDungDichVus.Where(s => s.NgaySuDung >= d && s.NgaySuDung < next).SumAsync(s => (decimal?)s.ThanhTien) ?? 0;
            dto.KhachDen = await _context.PhieuDatPhongs.CountAsync(p => p.NgayCheckInThucTe >= d && p.NgayCheckInThucTe < next);
            dto.KhachDi = await _context.PhieuDatPhongs.CountAsync(p => p.TrangThai == BookingStatus.CheckedOut && p.NgayCheckOutThucTe >= d && p.NgayCheckOutThucTe < next);

            // Hoá đơn thanh toán trong ngày: lần thu đầu tiên rơi vào ngày d
            var hoaDon = await _context.HoaDons
                .Where(h => h.TrangThai == InvoiceStatus.Paid)
                .Select(h => new { h.TongTienCanThanhToan, LanThuDau = h.ThanhToans.Where(t => t.LoaiGiaoDich == PaymentType.Payment).Min(t => (DateTime?)t.NgayThanhToan) ?? h.NgayLapHoaDon })
                .Where(h => h.LanThuDau >= d && h.LanThuDau < next)
                .ToListAsync();
            dto.SoHoaDonThanhToan = hoaDon.Count;
            dto.TongHoaDonThanhToan = Math.Round(hoaDon.Sum(h => h.TongTienCanThanhToan), 0);

            // Tiền thực thu / chi trong ngày theo phương thức (thanh toán + cọc; không tính điểm thưởng)
            var dong = (await _context.ThanhToans
                    .Where(t => t.NgayThanhToan >= d && t.NgayThanhToan < next && t.PhuongThuc != LoyaltyPolicy.PhuongThucDiem)
                    .Select(t => new { t.PhuongThuc, SoTien = t.LoaiGiaoDich == PaymentType.Refund ? -t.SoTienDaTra : t.SoTienDaTra })
                    .ToListAsync())
                .Concat(await _context.GiaoDichCocs
                    .Where(g => g.ThoiGian >= d && g.ThoiGian < next)
                    .Select(g => new { g.PhuongThuc, SoTien = g.LoaiGiaoDich == PaymentType.Refund ? -g.SoTien : g.SoTien })
                    .ToListAsync())
                .Where(x => x.SoTien != 0);
            dto.TheoPhuongThuc = dong.GroupBy(x => x.PhuongThuc ?? "Không rõ")
                .Select(g => new ShiftMethodDto
                {
                    PhuongThuc = g.Key,
                    SoGiaoDich = g.Count(),
                    Thu = g.Where(x => x.SoTien > 0).Sum(x => x.SoTien),
                    Chi = -g.Where(x => x.SoTien < 0).Sum(x => x.SoTien),
                    Rong = g.Sum(x => x.SoTien)
                })
                .OrderBy(m => m.PhuongThuc != PaymentMethods.TienMat).ThenByDescending(m => m.Rong)
                .ToList();
            return dto;
        }

        private static string TenKhach(PhieuDatPhong p) => p.KhachHang == null ? "" : $"{p.KhachHang.Ho} {p.KhachHang.Ten}".Trim();

        private static DayCloseDto ToDto(ChotNgay c)
        {
            var chiTiet = string.IsNullOrEmpty(c.ChiTiet) ? new ChiTietChot() : JsonSerializer.Deserialize<ChiTietChot>(c.ChiTiet) ?? new ChiTietChot();
            return new DayCloseDto
            {
                Ngay = c.Ngay,
                ThoiGianChot = c.ThoiGianChot,
                TenNhanVien = c.NhanVien?.TenNhanVien,
                GhiChu = c.GhiChu,
                SoLieu = new NightAuditFiguresDto
                {
                    Ngay = c.Ngay,
                    TongSoPhong = c.TongSoPhong,
                    DemPhongBan = c.DemPhongBan,
                    CongSuat = c.CongSuat,
                    TienPhong = c.TienPhong,
                    ADR = c.ADR,
                    RevPAR = c.RevPAR,
                    DoanhThuKhac = c.DoanhThuKhac,
                    TienDichVu = c.TienDichVu,
                    SoHoaDonThanhToan = c.SoHoaDonThanhToan,
                    TongHoaDonThanhToan = c.TongHoaDonThanhToan,
                    KhachDen = c.KhachDen,
                    KhachDi = c.KhachDi,
                    KhachKhongDen = c.KhachKhongDen,
                    TheoPhuongThuc = chiTiet.TheoPhuongThuc,
                    CacDem = chiTiet.CacDem,
                    CocGiuLai = chiTiet.CocGiuLai
                }
            };
        }

        // Nội dung cột ChotNgay.ChiTiet
        private class ChiTietChot
        {
            public List<ShiftMethodDto> TheoPhuongThuc { get; set; } = new List<ShiftMethodDto>();
            public List<NightRoomRevenueDto> CacDem { get; set; } = new List<NightRoomRevenueDto>();
            public List<DepositHoldingDto> CocGiuLai { get; set; } = new List<DepositHoldingDto>();
        }
    }
}
