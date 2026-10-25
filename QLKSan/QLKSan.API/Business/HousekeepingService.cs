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

namespace QLKSan.Business
{
    public class HousekeepingService : IHousekeepingService
    {
        private readonly ApplicationDbContext _context;

        public HousekeepingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Phong>> GetRoomsNeedingCleaningAsync()
        {
            return await _context.Phongs
                .Include(p => p.LoaiPhong)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.PhieuDatPhong).ThenInclude(p => p.KhachHang)
                .Where(p => p.TrangThai == RoomStatus.Cleaning)
                .ToListAsync();
        }

        public async Task MarkRoomAsCleanAsync(string maPhong)
        {
            var room = await _context.Phongs.FindAsync(maPhong)
                ?? throw new NotFoundException($"Không tìm thấy phòng '{maPhong}'.");
            if (room.TrangThai != RoomStatus.Cleaning)
                throw new ConflictException("Phòng không ở trạng thái đang dọn.");

            room.TrangThai = RoomStatus.Available;
            await HousekeepingTasks.CloseOpenTasksAsync(_context, maPhong, $"Lễ tân xác nhận đã dọn {DateTime.Now:dd/MM HH:mm}");
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetCountByStatusAsync(RoomStatus status)
        {
            return await _context.Phongs.CountAsync(p => p.TrangThai == status);
        }

        public async Task<IEnumerable<NhanVien>> GetHousekeepingStaffAsync()
        {
            var staff = await _context.NhanViens
                .Include(n => n.TaiKhoan)
                .Where(n => n.TrangThai == EmployeeStatus.Working)
                .ToListAsync();
            return staff
                .Where(s => s.TaiKhoan?.VaiTro == Role.Housekeeping
                         || (s.ChucVu != null && (s.ChucVu.Contains("Buồng") || s.ChucVu.Contains("buồng") || s.ChucVu.Contains("Dọn") || s.ChucVu.Contains("Vệ sinh"))))
                .OrderBy(s => s.TenNhanVien)
                .ToList();
        }

        // ===== Công việc dọn phòng =====

        private IQueryable<PhanCongDonPhong> TaskQuery()
            => _context.PhanCongDonPhongs
                .Include(t => t.Phong).ThenInclude(p => p.LoaiPhong)
                .Include(t => t.NhanVien);

        public async Task<IEnumerable<PhanCongDonPhong>> GetOpenTasksAsync()
        {
            return await TaskQuery()
                .Where(t => t.TrangThai != HousekeepingTaskStatus.Completed)
                .OrderBy(t => t.ThoiGianTao)
                .ToListAsync();
        }

        public async Task<IEnumerable<PhanCongDonPhong>> GetTasksOfStaffAsync(string maNhanVien)
        {
            // Việc đang làm + việc đã xong trong hôm nay
            var today = DateTime.Today;
            return await TaskQuery()
                .Where(t => t.MaNhanVien == maNhanVien
                    && (t.TrangThai != HousekeepingTaskStatus.Completed || t.ThoiGianHoanThanh >= today))
                .OrderBy(t => t.TrangThai == HousekeepingTaskStatus.Completed)
                .ThenBy(t => t.ThoiGianGiao)
                .ToListAsync();
        }

        private async Task<PhanCongDonPhong> FindTaskAsync(string maPhanCong)
            => await TaskQuery().FirstOrDefaultAsync(t => t.MaPhanCong == maPhanCong)
               ?? throw new NotFoundException($"Không tìm thấy công việc '{maPhanCong}'.");

        private static void EnsureCanAct(PhanCongDonPhong task, string maNhanVienThucHien, bool laQuanLy)
        {
            if (!laQuanLy && task.MaNhanVien != maNhanVienThucHien)
                throw new BusinessException("Công việc này không được giao cho bạn.");
        }

        public async Task<PhanCongDonPhong> AssignTaskAsync(string maPhanCong, string maNhanVien)
        {
            var task = await FindTaskAsync(maPhanCong);
            if (task.TrangThai != HousekeepingTaskStatus.Unassigned && task.TrangThai != HousekeepingTaskStatus.Assigned)
                throw new ConflictException("Chỉ giao (hoặc giao lại) được công việc chưa bắt đầu.");

            var staff = (await GetHousekeepingStaffAsync()).FirstOrDefault(s => s.MaNhanVien == maNhanVien)
                ?? throw new BusinessException("Chỉ giao việc cho nhân viên buồng phòng đang làm việc.");

            task.MaNhanVien = staff.MaNhanVien;
            task.NhanVien = staff;
            task.TrangThai = HousekeepingTaskStatus.Assigned;
            task.ThoiGianGiao = DateTime.Now;
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<int> AssignManyAsync(IEnumerable<string> maPhanCongs, string maNhanVien)
        {
            var ids = maPhanCongs?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() ?? new List<string>();
            if (ids.Count == 0) throw new BusinessException("Vui lòng chọn ít nhất một việc.");
            var staff = (await GetHousekeepingStaffAsync()).FirstOrDefault(s => s.MaNhanVien == maNhanVien)
                ?? throw new BusinessException("Chỉ giao việc cho nhân viên buồng phòng đang làm việc.");
            var tasks = await _context.PhanCongDonPhongs.Where(t => ids.Contains(t.MaPhanCong)).ToListAsync();
            if (tasks.Count != ids.Count) throw new NotFoundException("Có công việc không tồn tại.");
            if (tasks.Any(t => t.TrangThai is not (HousekeepingTaskStatus.Unassigned or HousekeepingTaskStatus.Assigned)))
                throw new ConflictException("Chỉ giao (hoặc giao lại) được công việc chưa bắt đầu.");
            foreach (var t in tasks)
            {
                t.MaNhanVien = staff.MaNhanVien;
                t.TrangThai = HousekeepingTaskStatus.Assigned;
                t.ThoiGianGiao = DateTime.Now;
            }
            await _context.SaveChangesAsync();
            return tasks.Count;
        }

        private record KhachDen(string HoTen, bool LaVIP, string GioDen);

        // Phiếu nhận phòng hôm nay chưa nhận: phòng → khách sắp đến (VIP, giờ đến)
        private async Task<Dictionary<string, KhachDen>> KhachDenHomNayAsync()
        {
            var today = DateTime.Today;
            var phieu = await _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Include(p => p.ChiTietDatPhongs)
                .Where(p => (p.TrangThai == BookingStatus.Pending || p.TrangThai == BookingStatus.Approved)
                         && p.NgayCheckInDuKien >= today && p.NgayCheckInDuKien < today.AddDays(1))
                .ToListAsync();
            return phieu
                .Where(p => p.ChiTietDatPhongs.Any())
                .GroupBy(p => p.ChiTietDatPhongs.First().MaPhong)
                .ToDictionary(g => g.Key, g =>
                {
                    var p = g.First();
                    return new KhachDen($"{p.KhachHang?.Ho} {p.KhachHang?.Ten}".Trim(), p.KhachHang?.LaVIP ?? false, p.GioDenDuKien);
                });
        }

        private static void SetPriority(HousekeepingTaskDto dto, KhachDen den)
        {
            if (dto.TrangThai == HousekeepingTaskStatus.Issue)
            {
                dto.UuTien = 4;
                dto.LyDoUuTien = "Chờ kỹ thuật xử lý sự cố";
            }
            else if (den != null)
            {
                dto.UuTien = den.LaVIP ? 1 : 2;
                dto.LyDoUuTien = (den.LaVIP ? "Khách VIP" : "Khách") + " đến hôm nay" + (string.IsNullOrEmpty(den.GioDen) ? "" : " · " + den.GioDen);
            }
            else if (dto.MaYeuCau != null)
            {
                dto.UuTien = 2;
                dto.LyDoUuTien = "Khách đang chờ (yêu cầu tại phòng)";
            }
        }

        public async Task<List<HousekeepingTaskDto>> PrioritizeAsync(IEnumerable<PhanCongDonPhong> tasks)
        {
            var den = await KhachDenHomNayAsync();
            return tasks.Select(t =>
                {
                    var dto = t.ToDto();
                    SetPriority(dto, den.GetValueOrDefault(t.MaPhong));
                    return dto;
                })
                .OrderBy(t => t.TrangThai == HousekeepingTaskStatus.Completed)
                .ThenBy(t => t.UuTien)
                .ThenBy(t => t.ThoiGianTao)
                .ToList();
        }

        public async Task<List<HousekeepingRoomDto>> GetBoardAsync()
        {
            var today = DateTime.Today;
            var rooms = await _context.Phongs.Include(p => p.LoaiPhong).ToListAsync();
            var viec = (await PrioritizeAsync(await GetOpenTasksAsync())).GroupBy(t => t.MaPhong).ToDictionary(g => g.Key, g => g.First());
            var den = await KhachDenHomNayAsync();
            var phongDi = (await _context.ChiTietDatPhongs
                    .Where(c => c.PhieuDatPhong.TrangThai == BookingStatus.CheckedIn && c.PhieuDatPhong.NgayCheckOutDuKien < today.AddDays(1))
                    .Select(c => c.MaPhong).ToListAsync())
                .ToHashSet();

            return rooms.Select(p =>
                {
                    var k = den.GetValueOrDefault(p.MaPhong);
                    return new HousekeepingRoomDto
                    {
                        MaPhong = p.MaPhong,
                        SoPhong = p.SoPhong,
                        Tang = HousekeepingTasks.TangCua(p.SoPhong),
                        TenLoaiPhong = p.LoaiPhong?.TenLoai,
                        TrangThai = p.TrangThai,
                        KhachDen = k?.HoTen,
                        GioDen = k?.GioDen,
                        KhachDenVIP = k?.LaVIP ?? false,
                        KhachDiHomNay = phongDi.Contains(p.MaPhong),
                        ViecDangMo = viec.GetValueOrDefault(p.MaPhong)
                    };
                })
                .OrderBy(r => r.Tang).ThenBy(r => r.SoPhong)
                .ToList();
        }

        public async Task<PhanCongDonPhong> StartTaskAsync(string maPhanCong, string maNhanVienThucHien, bool laQuanLy)
        {
            var task = await FindTaskAsync(maPhanCong);
            EnsureCanAct(task, maNhanVienThucHien, laQuanLy);
            if (task.TrangThai != HousekeepingTaskStatus.Assigned)
                throw new ConflictException("Chỉ bắt đầu được công việc đã được giao.");

            task.TrangThai = HousekeepingTaskStatus.InProgress;
            task.ThoiGianBatDau = DateTime.Now;
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<PhanCongDonPhong> CompleteTaskAsync(string maPhanCong, string maNhanVienThucHien, bool laQuanLy)
        {
            var task = await FindTaskAsync(maPhanCong);
            EnsureCanAct(task, maNhanVienThucHien, laQuanLy);
            if (!HousekeepingTasks.IsOpen(task.TrangThai) || (task.TrangThai == HousekeepingTaskStatus.Unassigned && !laQuanLy))
                throw new ConflictException("Công việc không ở trạng thái có thể hoàn thành.");

            task.TrangThai = HousekeepingTaskStatus.Completed;
            task.ThoiGianBatDau ??= DateTime.Now;
            task.ThoiGianHoanThanh = DateTime.Now;

            // Dọn xong: phòng sẵn sàng đón khách; yêu cầu khách liên kết (nếu có) hoàn thành
            if (task.Phong.TrangThai == RoomStatus.Cleaning)
                task.Phong.TrangThai = RoomStatus.Available;
            await HousekeepingTasks.CompleteLinkedRequestAsync(_context, task, $"Buồng phòng đã xử lý ({task.NhanVien?.TenNhanVien ?? "quản lý"})");

            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<PhanCongDonPhong> ReportIssueAsync(string maPhanCong, string ghiChu, string maNhanVienThucHien, bool laQuanLy)
        {
            var task = await FindTaskAsync(maPhanCong);
            EnsureCanAct(task, maNhanVienThucHien, laQuanLy);
            if (!HousekeepingTasks.IsOpen(task.TrangThai))
                throw new ConflictException("Công việc đã kết thúc, không thể báo sự cố.");

            task.TrangThai = HousekeepingTaskStatus.Issue;
            task.GhiChu = string.IsNullOrEmpty(task.GhiChu) ? $"Sự cố: {ghiChu}" : $"{task.GhiChu} | Sự cố: {ghiChu}";

            // Phòng có sự cố chuyển sang bảo trì (không áp dụng nếu khách vẫn đang ở)
            if (task.Phong.TrangThai != RoomStatus.Occupied)
                task.Phong.TrangThai = RoomStatus.Maintenance;

            await _context.SaveChangesAsync();
            return task;
        }
    }
}
