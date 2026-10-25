using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    // Tạo/đóng công việc dọn phòng — dùng chung cho trả phòng (CheckoutService), đổi trạng thái phòng (RoomService)
    // và màn hình buồng phòng (HousekeepingService). Không tự SaveChanges: gộp vào giao dịch của nơi gọi.
    public static class HousekeepingTasks
    {
        public const string DonSauTraPhong = "Dọn sau trả phòng";
        public const string DonTheoYeuCau = "Dọn theo yêu cầu";
        public const string DonSauDoiPhong = "Dọn sau đổi phòng";
        public const string YeuCauKhach = "Yêu cầu của khách";

        // Tầng suy ra từ số phòng: bỏ 2 chữ số cuối (101 → 1, 1203 → 12)
        public static int TangCua(string soPhong)
            => soPhong != null && soPhong.Length > 2 && int.TryParse(soPhong[..^2], out var t) ? t : 0;

        // Việc xử lý yêu cầu khách xong → yêu cầu tự chuyển Hoàn thành
        public static async Task CompleteLinkedRequestAsync(ApplicationDbContext context, PhanCongDonPhong task, string ketQua)
        {
            if (task.MaYeuCau == null) return;
            var y = await context.YeuCauKhachs.FindAsync(task.MaYeuCau.Value);
            if (y == null || y.TrangThai is GuestRequestStatus.Done or GuestRequestStatus.Cancelled) return;
            y.TrangThai = GuestRequestStatus.Done;
            y.ThoiGianXuLy = DateTime.Now;
            y.MaNhanVienXuLy = task.MaNhanVien ?? y.MaNhanVienXuLy;
            y.KetQua = ketQua;
        }

        // Công việc đang mở: chưa hoàn thành và chưa báo sự cố
        public static bool IsOpen(HousekeepingTaskStatus status)
            => status == HousekeepingTaskStatus.Unassigned
            || status == HousekeepingTaskStatus.Assigned
            || status == HousekeepingTaskStatus.InProgress;

        // Mỗi phòng chỉ có tối đa 1 công việc đang mở
        public static async Task<PhanCongDonPhong> EnsureOpenTaskAsync(ApplicationDbContext context, string maPhong, string maDatPhong, string loaiCongViec)
        {
            var existing = await context.PhanCongDonPhongs
                .FirstOrDefaultAsync(t => t.MaPhong == maPhong
                    && (t.TrangThai == HousekeepingTaskStatus.Unassigned
                     || t.TrangThai == HousekeepingTaskStatus.Assigned
                     || t.TrangThai == HousekeepingTaskStatus.InProgress));
            if (existing != null) return existing;

            var task = new PhanCongDonPhong
            {
                MaPhanCong = await IdGenerator.NextAsync(context.PhanCongDonPhongs.Select(t => t.MaPhanCong), "PC", 4),
                MaPhong = maPhong,
                MaDatPhong = maDatPhong,
                LoaiCongViec = loaiCongViec,
                TrangThai = HousekeepingTaskStatus.Unassigned,
                ThoiGianTao = DateTime.Now
            };
            context.PhanCongDonPhongs.Add(task);
            return task;
        }

        // Đóng các công việc đang mở của phòng (vd: lễ tân xác nhận đã dọn, đổi trạng thái thủ công)
        public static async Task CloseOpenTasksAsync(ApplicationDbContext context, string maPhong, string ghiChu)
        {
            var tasks = await context.PhanCongDonPhongs
                .Where(t => t.MaPhong == maPhong
                    && (t.TrangThai == HousekeepingTaskStatus.Unassigned
                     || t.TrangThai == HousekeepingTaskStatus.Assigned
                     || t.TrangThai == HousekeepingTaskStatus.InProgress))
                .ToListAsync();
            foreach (var task in tasks)
            {
                task.TrangThai = HousekeepingTaskStatus.Completed;
                task.ThoiGianHoanThanh = DateTime.Now;
                task.GhiChu = string.IsNullOrEmpty(task.GhiChu) ? ghiChu : $"{task.GhiChu} | {ghiChu}";
                await CompleteLinkedRequestAsync(context, task, ghiChu);
            }
        }

        // Phòng hết bảo trì: các công việc báo sự cố của phòng coi như đã xử lý
        public static async Task ResolveIssuesAsync(ApplicationDbContext context, string maPhong)
        {
            var issues = await context.PhanCongDonPhongs
                .Where(t => t.MaPhong == maPhong && t.TrangThai == HousekeepingTaskStatus.Issue)
                .ToListAsync();
            foreach (var task in issues)
            {
                task.TrangThai = HousekeepingTaskStatus.Completed;
                task.ThoiGianHoanThanh = DateTime.Now;
                task.GhiChu = $"{task.GhiChu} | Đã xử lý sự cố {DateTime.Now:dd/MM/yyyy HH:mm}";
            }
        }
    }
}
