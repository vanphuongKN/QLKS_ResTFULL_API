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
    // Phiếu kỹ thuật: sự cố trong phòng (từ buồng phòng báo, yêu cầu khách loại Kỹ thuật hoặc lễ tân ghi), theo dõi tới khi sửa xong.
    // Phiếu "khoá phòng" chuyển phòng sang Bảo trì; sửa xong phiếu cuối cùng thì phòng chuyển sang Đang dọn và tự tạo việc dọn.
    public interface IMaintenanceService
    {
        // loc: "mo" (mới / đang sửa), "xong" (xong / huỷ trong 7 ngày), còn lại: 30 ngày + đang mở
        Task<List<MaintenanceTicketDto>> ListAsync(string loc);
        Task<MaintenanceTicketDto> CreateAsync(MaintenanceCreateRequest request, string maNhanVien);
        Task<MaintenanceTicketDto> UpdateStatusAsync(int maPhieu, GuestRequestStatus trangThai, string ketQua, string maNhanVien);
    }

    public class MaintenanceService : IMaintenanceService
    {
        public const string MucDoThuong = "Thường";
        public const string MucDoKhan = "Khẩn";

        private readonly ApplicationDbContext _context;

        public MaintenanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<PhieuKyThuat> Query() => _context.PhieuKyThuats
            .Include(p => p.Phong)
            .Include(p => p.NhanVienBao)
            .Include(p => p.NhanVienXuLy);

        public async Task<List<MaintenanceTicketDto>> ListAsync(string loc)
        {
            var q = Query();
            var moc7 = DateTime.Today.AddDays(-7);
            var moc30 = DateTime.Today.AddDays(-30);
            q = loc switch
            {
                "mo" => q.Where(p => p.TrangThai == GuestRequestStatus.New || p.TrangThai == GuestRequestStatus.InProgress),
                "xong" => q.Where(p => (p.TrangThai == GuestRequestStatus.Done || p.TrangThai == GuestRequestStatus.Cancelled) && p.ThoiGianBao >= moc7),
                _ => q.Where(p => p.TrangThai == GuestRequestStatus.New || p.TrangThai == GuestRequestStatus.InProgress || p.ThoiGianBao >= moc30)
            };
            return (await q.ToListAsync())
                .OrderBy(p => GuestRequestService.DangMo(p.TrangThai) ? 0 : 1)
                .ThenBy(p => p.MucDo == MucDoKhan ? 0 : 1)
                .ThenByDescending(p => p.ThoiGianBao)
                .Select(p => p.ToDto())
                .ToList();
        }

        public async Task<MaintenanceTicketDto> CreateAsync(MaintenanceCreateRequest r, string maNhanVien)
        {
            if (r.MucDo != MucDoThuong && r.MucDo != MucDoKhan)
                throw new BusinessException("Mức độ phải là Thường hoặc Khẩn.");
            var phong = await _context.Phongs.FindAsync(r.MaPhong)
                ?? throw new NotFoundException($"Không tìm thấy phòng '{r.MaPhong}'.");
            var phieu = await OpenAsync(_context, phong, r.MoTa.Trim(), r.MucDo, r.KhoaPhong, maNhanVien, null, null);
            await _context.SaveChangesAsync();
            return (await Query().FirstAsync(p => p.MaPhieu == phieu.MaPhieu)).ToDto();
        }

        // Mở phiếu (dùng chung cho lễ tân ghi, buồng phòng báo sự cố, yêu cầu khách loại Kỹ thuật). Không tự SaveChanges.
        // Khoá phòng: phòng không có khách chuyển sang Bảo trì
        internal static async Task<PhieuKyThuat> OpenAsync(ApplicationDbContext context, Phong phong, string moTa, string mucDo, bool khoaPhong,
            string maNhanVien, string maPhanCong, int? maYeuCau)
        {
            var coKhach = phong.TrangThai == RoomStatus.Occupied
                || await context.ChiTietDatPhongs.AnyAsync(c => c.MaPhong == phong.MaPhong && c.PhieuDatPhong.TrangThai == BookingStatus.CheckedIn);
            var phieu = new PhieuKyThuat
            {
                MaPhong = phong.MaPhong,
                MoTa = moTa,
                MucDo = mucDo,
                KhoaPhong = khoaPhong && !coKhach,
                TrangThai = GuestRequestStatus.New,
                MaNhanVienBao = maNhanVien,
                ThoiGianBao = DateTime.Now,
                MaPhanCong = maPhanCong,
                MaYeuCau = maYeuCau
            };
            context.PhieuKyThuats.Add(phieu);
            if (phieu.KhoaPhong) phong.TrangThai = RoomStatus.Maintenance;
            return phieu;
        }

        public async Task<MaintenanceTicketDto> UpdateStatusAsync(int maPhieu, GuestRequestStatus trangThai, string ketQua, string maNhanVien)
        {
            var phieu = await _context.PhieuKyThuats.Include(p => p.Phong).FirstOrDefaultAsync(p => p.MaPhieu == maPhieu)
                ?? throw new NotFoundException($"Không tìm thấy phiếu kỹ thuật #{maPhieu}.");
            if (!GuestRequestService.DangMo(phieu.TrangThai))
                throw new ConflictException("Phiếu đã xong hoặc đã huỷ.");
            if (trangThai == GuestRequestStatus.New)
                throw new BusinessException("Trạng thái mới không hợp lệ.");
            ketQua = string.IsNullOrWhiteSpace(ketQua) ? null : ketQua.Trim();
            if (trangThai == GuestRequestStatus.Cancelled && ketQua == null)
                throw new BusinessException("Vui lòng nhập lý do huỷ phiếu.");

            phieu.TrangThai = trangThai;
            phieu.MaNhanVienXuLy = maNhanVien;
            if (ketQua != null) phieu.KetQua = ketQua;
            if (!GuestRequestService.DangMo(trangThai))
            {
                phieu.ThoiGianXong = DateTime.Now;
                await CloseEffectsAsync(_context, phieu, ketQua ?? "Kỹ thuật đã xử lý");
            }
            await _context.SaveChangesAsync();
            return (await Query().FirstAsync(p => p.MaPhieu == maPhieu)).ToDto();
        }

        // Phiếu kết thúc: yêu cầu khách liên kết hoàn thành; phiếu khoá phòng cuối cùng → phòng Đang dọn + việc dọn, đóng việc "Sự cố"
        private static async Task CloseEffectsAsync(ApplicationDbContext context, PhieuKyThuat phieu, string ketQua)
        {
            if (phieu.MaYeuCau != null)
            {
                var y = await context.YeuCauKhachs.FindAsync(phieu.MaYeuCau.Value);
                if (y != null && GuestRequestService.DangMo(y.TrangThai))
                {
                    y.TrangThai = phieu.TrangThai == GuestRequestStatus.Done ? GuestRequestStatus.Done : GuestRequestStatus.Cancelled;
                    y.ThoiGianXuLy = DateTime.Now;
                    y.MaNhanVienXuLy = phieu.MaNhanVienXuLy;
                    y.KetQua = $"Phiếu kỹ thuật #{phieu.MaPhieu}: {ketQua}";
                }
            }

            if (!phieu.KhoaPhong || phieu.Phong.TrangThai != RoomStatus.Maintenance) return;
            var conPhieuKhoa = await context.PhieuKyThuats.AnyAsync(p => p.MaPhong == phieu.MaPhong && p.MaPhieu != phieu.MaPhieu && p.KhoaPhong
                && (p.TrangThai == GuestRequestStatus.New || p.TrangThai == GuestRequestStatus.InProgress));
            if (conPhieuKhoa) return;

            phieu.Phong.TrangThai = RoomStatus.Cleaning;
            await HousekeepingTasks.ResolveIssuesAsync(context, phieu.MaPhong, closeTickets: false);
            await HousekeepingTasks.EnsureOpenTaskAsync(context, phieu.MaPhong, null, HousekeepingTasks.DonSauSuaChua);
        }
    }
}
