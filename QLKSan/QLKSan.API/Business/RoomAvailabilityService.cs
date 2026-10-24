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
    public interface IRoomAvailabilityService
    {
        // Phòng trống trong khoảng [checkIn, checkOut) và đủ sức chứa
        Task<List<Phong>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, int? soNguoi = null);

        // Ném BusinessException nếu phòng đã có phiếu trùng lịch
        Task EnsureAvailableAsync(string maPhong, DateTime checkIn, DateTime checkOut);

        // Khoá dòng Phong trong transaction hiện tại để 2 người không đặt cùng phòng cùng lúc
        Task LockRoomAsync(string maPhong);
    }

    // Phòng trống được xác định theo KHOẢNG NGÀY từ các phiếu đặt đang hiệu lực,
    // không dựa vào trạng thái hiện tại của phòng (Phong.TrangThai chỉ còn là trạng thái vật lý).
    public class RoomAvailabilityService : IRoomAvailabilityService
    {
        private readonly ApplicationDbContext _context;

        public RoomAvailabilityService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hai khoảng ngày [aIn, aOut) và [bIn, bOut) trùng nhau
        public static bool Overlaps(DateTime aIn, DateTime aOut, DateTime bIn, DateTime bOut)
            => aIn.Date < bOut.Date && bIn.Date < aOut.Date;

        // Khách đang ở (đã check-in) chiếm phòng tới ít nhất ngày mai, kể cả khi quá ngày trả dự kiến
        public static DateTime NgayTraHieuLuc(BookingStatus trangThai, DateTime ngayTraDuKien, DateTime today)
            => trangThai == BookingStatus.CheckedIn && ngayTraDuKien.Date <= today.Date ? today.Date.AddDays(1) : ngayTraDuKien.Date;

        public async Task<List<Phong>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, int? soNguoi = null)
        {
            var today = DateTime.Today;
            var rooms = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .Include(p => p.HinhAnhs)
                .Where(p => p.TrangThai != RoomStatus.Maintenance)
                .ToListAsync();

            var busyRooms = (await LoadActiveBookingsAsync(null))
                .Where(b => Overlaps(b.CheckIn, NgayTraHieuLuc(b.TrangThai, b.CheckOut, today), checkIn, checkOut))
                .Select(b => b.MaPhong)
                .ToHashSet();

            return rooms
                .Where(p => !busyRooms.Contains(p.MaPhong))
                // Nhận phòng hôm nay thì phòng không được đang có khách
                .Where(p => !(checkIn.Date <= today && p.TrangThai == RoomStatus.Occupied))
                .Where(p => soNguoi == null || p.LoaiPhong == null || p.LoaiPhong.SoNguoiToiDa >= soNguoi)
                .OrderBy(p => p.SoPhong)
                .ToList();
        }

        public async Task EnsureAvailableAsync(string maPhong, DateTime checkIn, DateTime checkOut)
        {
            var today = DateTime.Today;
            var conflict = (await LoadActiveBookingsAsync(maPhong))
                .Where(b => Overlaps(b.CheckIn, NgayTraHieuLuc(b.TrangThai, b.CheckOut, today), checkIn, checkOut))
                .OrderBy(b => b.CheckIn)
                .FirstOrDefault();

            if (conflict != null)
            {
                var denNgay = NgayTraHieuLuc(conflict.TrangThai, conflict.CheckOut, today);
                var loai = conflict.TrangThai == BookingStatus.CheckedIn ? "đang có khách lưu trú" : "đã có khách đặt";
                throw new ConflictException(
                    $"Phòng {loai} từ {conflict.CheckIn:dd/MM/yyyy} đến {denNgay:dd/MM/yyyy}. Vui lòng chọn ngày hoặc phòng khác.");
            }
        }

        public async Task LockRoomAsync(string maPhong)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT MaPhong FROM Phong WITH (UPDLOCK, HOLDLOCK) WHERE MaPhong = {maPhong}");
        }

        private record ActiveBooking(string MaDatPhong, string MaPhong, DateTime CheckIn, DateTime CheckOut, BookingStatus TrangThai);

        private async Task<List<ActiveBooking>> LoadActiveBookingsAsync(string maPhong)
        {
            var query = _context.ChiTietDatPhongs
                .Where(c => c.PhieuDatPhong.TrangThai == BookingStatus.Pending
                         || c.PhieuDatPhong.TrangThai == BookingStatus.Approved
                         || c.PhieuDatPhong.TrangThai == BookingStatus.CheckedIn);
            if (maPhong != null) query = query.Where(c => c.MaPhong == maPhong);

            return await query
                .Select(c => new ActiveBooking(c.MaDatPhong, c.MaPhong, c.PhieuDatPhong.NgayCheckInDuKien,
                    c.PhieuDatPhong.NgayCheckOutDuKien, c.PhieuDatPhong.TrangThai))
                .ToListAsync();
        }
    }
}
