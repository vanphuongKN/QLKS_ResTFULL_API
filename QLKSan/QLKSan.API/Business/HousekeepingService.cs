using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
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

            // Dọn xong: phòng sẵn sàng đón khách
            if (task.Phong.TrangThai == RoomStatus.Cleaning)
                task.Phong.TrangThai = RoomStatus.Available;

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
