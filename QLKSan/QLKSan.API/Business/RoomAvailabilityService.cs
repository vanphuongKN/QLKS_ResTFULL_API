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

        // Ném ConflictException nếu phòng đã có phiếu trùng lịch (boQuaMaDatPhong: bỏ qua chính phiếu đang gia hạn / đổi ngày)
        Task EnsureAvailableAsync(string maPhong, DateTime checkIn, DateTime checkOut, string boQuaMaDatPhong = null);

        // Khoá dòng Phong trong transaction hiện tại để 2 người không đặt cùng phòng cùng lúc
        Task LockRoomAsync(string maPhong);

        // Giữ một phòng đích danh / phòng trống đầu tiên của hạng cho khoảng ngày (gọi trong transaction; đã khoá phòng)
        Task<Phong> ReserveRoomAsync(string maPhong, DateTime checkIn, DateTime checkOut, int soNguoi);
        Task<Phong> ReserveRoomOfTypeAsync(string maLoai, DateTime checkIn, DateTime checkOut, int soNguoi);
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

        public async Task EnsureAvailableAsync(string maPhong, DateTime checkIn, DateTime checkOut, string boQuaMaDatPhong = null)
        {
            var today = DateTime.Today;
            var conflict = (await LoadActiveBookingsAsync(maPhong))
                .Where(b => b.MaDatPhong != boQuaMaDatPhong)
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

        public async Task<Phong> ReserveRoomAsync(string maPhong, DateTime checkIn, DateTime checkOut, int soNguoi)
        {
            // Khoá phòng để hai khách không đặt trùng lịch cùng lúc
            await LockRoomAsync(maPhong);

            var room = await _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.MaPhong == maPhong)
                ?? throw new BusinessException("Phòng không tồn tại.");
            if (room.TrangThai == RoomStatus.Maintenance)
                throw new ConflictException("Phòng đang bảo trì, vui lòng chọn phòng khác.");
            if (room.LoaiPhong != null && soNguoi > room.LoaiPhong.SoNguoiToiDa)
                throw new BusinessException($"Số lượng khách ({soNguoi}) vượt quá sức chứa tối đa của {room.LoaiPhong.TenLoai} ({room.LoaiPhong.SoNguoiToiDa} người lớn).");

            await EnsureAvailableAsync(maPhong, checkIn, checkOut);
            return room;
        }

        // Xếp phòng trống đầu tiên của hạng (theo số phòng); lễ tân có thể đổi phòng trước khi nhận khách
        public async Task<Phong> ReserveRoomOfTypeAsync(string maLoai, DateTime checkIn, DateTime checkOut, int soNguoi)
        {
            var loai = await _context.LoaiPhongs.FindAsync(maLoai)
                ?? throw new BusinessException("Hạng phòng không tồn tại.");
            if (soNguoi > loai.SoNguoiToiDa)
                throw new BusinessException($"Số lượng khách ({soNguoi}) vượt quá sức chứa tối đa của {loai.TenLoai} ({loai.SoNguoiToiDa} người lớn).");

            var candidates = await _context.Phongs.Include(p => p.LoaiPhong)
                .Where(p => p.MaLoai == maLoai && p.TrangThai != RoomStatus.Maintenance)
                .OrderBy(p => p.SoPhong)
                .ToListAsync();
            foreach (var room in candidates)
            {
                // Khoá từng phòng trước khi kiểm tra để hai yêu cầu đồng thời không cùng nhận một phòng
                await LockRoomAsync(room.MaPhong);
                try
                {
                    await EnsureAvailableAsync(room.MaPhong, checkIn, checkOut);
                    return room;
                }
                catch (ConflictException)
                {
                    // Phòng này đã có khách trong khoảng ngày: thử phòng tiếp theo cùng hạng
                }
            }
            throw new ConflictException($"{loai.TenLoai} đã hết phòng trong khoảng ngày đã chọn. Vui lòng chọn ngày hoặc hạng phòng khác.");
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
