using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;
using QLKSan.Models.Requests;

namespace QLKSan.Business
{
    // Sổ yêu cầu khách tại quầy: báo thức, đặt xe, nhắn gửi, đồ thất lạc… theo dõi từ lúc ghi nhận tới khi xử lý xong
    public interface IGuestRequestService
    {
        // loc: "mo" (mới / đang xử lý), "xong" (hoàn thành / huỷ trong 7 ngày), còn lại: tất cả trong 30 ngày + đang mở
        Task<List<GuestRequestDto>> ListAsync(string loc, string maDatPhong);
        Task<GuestRequestDto> CreateAsync(GuestRequestCreateRequest request, string maNhanVien);
        Task<GuestRequestDto> UpdateStatusAsync(int maYeuCau, GuestRequestStatus trangThai, string ketQua, string maNhanVien);
    }

    public class GuestRequestService : IGuestRequestService
    {
        private readonly ApplicationDbContext _context;

        public GuestRequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<YeuCauKhach> Query() => _context.YeuCauKhachs
            .Include(y => y.Phong)
            .Include(y => y.PhieuDatPhong).ThenInclude(p => p.KhachHang)
            .Include(y => y.NhanVienTao)
            .Include(y => y.NhanVienXuLy);

        internal static bool DangMo(GuestRequestStatus t) => t is GuestRequestStatus.New or GuestRequestStatus.InProgress;

        public async Task<List<GuestRequestDto>> ListAsync(string loc, string maDatPhong)
        {
            var q = Query();
            if (!string.IsNullOrWhiteSpace(maDatPhong)) q = q.Where(y => y.MaDatPhong == maDatPhong);
            var moc7 = DateTime.Today.AddDays(-7);
            var moc30 = DateTime.Today.AddDays(-30);
            q = loc switch
            {
                "mo" => q.Where(y => y.TrangThai == GuestRequestStatus.New || y.TrangThai == GuestRequestStatus.InProgress),
                "xong" => q.Where(y => (y.TrangThai == GuestRequestStatus.Done || y.TrangThai == GuestRequestStatus.Cancelled) && y.ThoiGianTao >= moc7),
                _ => q.Where(y => y.TrangThai == GuestRequestStatus.New || y.TrangThai == GuestRequestStatus.InProgress || y.ThoiGianTao >= moc30)
            };
            var list = await q.ToListAsync();
            // Đang mở trước, giờ hẹn gần nhất trước; đã xong: mới nhất trước
            return list
                .OrderBy(y => DangMo(y.TrangThai) ? 0 : 1)
                .ThenBy(y => DangMo(y.TrangThai) ? (y.ThoiGianHen ?? DateTime.MaxValue) : DateTime.MaxValue)
                .ThenByDescending(y => y.ThoiGianTao)
                .Select(y => y.ToDto())
                .ToList();
        }

        public async Task<GuestRequestDto> CreateAsync(GuestRequestCreateRequest r, string maNhanVien)
        {
            if (!GuestRequestTypes.TatCa.Contains(r.Loai))
                throw new BusinessException("Loại yêu cầu không hợp lệ.");

            var maPhong = string.IsNullOrWhiteSpace(r.MaPhong) ? null : r.MaPhong.Trim();
            string maDatPhong = null;
            if (!string.IsNullOrWhiteSpace(r.MaDatPhong))
            {
                var booking = await _context.PhieuDatPhongs.Include(p => p.ChiTietDatPhongs).FirstOrDefaultAsync(p => p.MaDatPhong == r.MaDatPhong)
                    ?? throw new NotFoundException($"Không tìm thấy phiếu đặt phòng '{r.MaDatPhong}'.");
                if (booking.TrangThai is not (BookingStatus.Pending or BookingStatus.Approved or BookingStatus.CheckedIn))
                    throw new ConflictException("Chỉ ghi yêu cầu cho khách đang ở hoặc sắp đến.");
                maDatPhong = booking.MaDatPhong;
                maPhong ??= booking.ChiTietDatPhongs.FirstOrDefault()?.MaPhong;
            }
            else if (maPhong != null && !await _context.Phongs.AnyAsync(p => p.MaPhong == maPhong))
            {
                throw new NotFoundException($"Không tìm thấy phòng '{maPhong}'.");
            }

            var y = new YeuCauKhach
            {
                MaDatPhong = maDatPhong,
                MaPhong = maPhong,
                Loai = r.Loai,
                NoiDung = r.NoiDung.Trim(),
                ThoiGianHen = r.ThoiGianHen,
                TrangThai = GuestRequestStatus.New,
                MaNhanVienTao = maNhanVien,
                ThoiGianTao = DateTime.Now
            };
            _context.YeuCauKhachs.Add(y);
            await _context.SaveChangesAsync();
            return (await Query().FirstAsync(x => x.MaYeuCau == y.MaYeuCau)).ToDto();
        }

        public async Task<GuestRequestDto> UpdateStatusAsync(int maYeuCau, GuestRequestStatus trangThai, string ketQua, string maNhanVien)
        {
            var y = await _context.YeuCauKhachs.FindAsync(maYeuCau)
                ?? throw new NotFoundException($"Không tìm thấy yêu cầu #{maYeuCau}.");
            if (!DangMo(y.TrangThai))
                throw new ConflictException("Yêu cầu đã hoàn thành hoặc đã huỷ.");
            if (trangThai == GuestRequestStatus.New)
                throw new BusinessException("Trạng thái mới không hợp lệ.");
            ketQua = string.IsNullOrWhiteSpace(ketQua) ? null : ketQua.Trim();
            if (trangThai == GuestRequestStatus.Cancelled && ketQua == null)
                throw new BusinessException("Vui lòng nhập lý do huỷ yêu cầu.");

            y.TrangThai = trangThai;
            y.MaNhanVienXuLy = maNhanVien;
            if (ketQua != null) y.KetQua = ketQua;
            if (!DangMo(trangThai)) y.ThoiGianXuLy = DateTime.Now;
            await _context.SaveChangesAsync();
            return (await Query().FirstAsync(x => x.MaYeuCau == maYeuCau)).ToDto();
        }
    }
}
