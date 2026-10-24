using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Models.Entities;
using QLKSan.Models.Requests;

namespace QLKSan.Business
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoomAvailabilityService _availability;
        private readonly HotelPolicy _policy;

        public CustomerService(ApplicationDbContext context, IRoomAvailabilityService availability, IOptions<HotelPolicy> policy)
        {
            _context = context;
            _availability = availability;
            _policy = policy.Value;
        }

        public async Task<KhachHang> GetCustomerByAccountIdAsync(string maDangNhap)
            => await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .Include(k => k.LienHeDiaChis)
                .Include(k => k.PhieuDatPhongs)
                .FirstOrDefaultAsync(k => k.MaDangNhap == maDangNhap);

        public async Task<KhachHang> UpdateContactAsync(string maKhachHang, UpdateContactRequest request)
        {
            var khach = await _context.KhachHangs.Include(k => k.LienHeDiaChis)
                .FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang)
                ?? throw new NotFoundException("Không tìm thấy hồ sơ khách hàng.");

            var lienHe = khach.LienHeDiaChis.OrderBy(l => l.MaLienHe).FirstOrDefault();
            if (lienHe == null)
            {
                lienHe = new LienHeDiaChi
                {
                    MaLienHe = await IdGenerator.NextAsync(_context.LienHeDiaChis.Select(l => l.MaLienHe), "LH", 4),
                    MaKhachHang = maKhachHang
                };
                _context.LienHeDiaChis.Add(lienHe);
            }
            lienHe.SDT = request.SDT.Trim();
            lienHe.Email = request.Email?.Trim() ?? "";
            lienHe.DiaChi = request.DiaChi.Trim();
            lienHe.QuocGia = string.IsNullOrWhiteSpace(request.QuocGia) ? "Việt Nam" : request.QuocGia.Trim();
            await _context.SaveChangesAsync();
            return khach;
        }

        public async Task<IEnumerable<PhieuDatPhong>> GetBookingsByCustomerAsync(string maKhachHang)
            => await _context.PhieuDatPhongs
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong).ThenInclude(r => r.LoaiPhong)
                .Include(p => p.HoaDon).ThenInclude(h => h.ThanhToans)
                .Where(p => p.MaKhachHang == maKhachHang)
                .OrderByDescending(p => p.NgayDat)
                .ToListAsync();

        public async Task<string> CreateOnlineBookingAsync(string maKhachHang, BookRoomRequest request)
        {
            var checkIn = request.CheckIn.Date;
            var checkOut = request.CheckOut.Date;
            var soNguoi = request.SoNguoi;
            if (string.IsNullOrWhiteSpace(request.MaLoai) == string.IsNullOrWhiteSpace(request.MaPhong))
                throw new BusinessException("Vui lòng chọn một hạng phòng (hoặc một phòng cụ thể).");
            if (checkIn < DateTime.Today)
                throw new BusinessException("Ngày nhận phòng không được ở trong quá khứ.");
            if (checkOut <= checkIn)
                throw new BusinessException("Ngày trả phòng phải sau ngày nhận phòng.");

            // Mã khuyến mãi phải còn hiệu lực lúc đặt; được lưu vào phiếu để áp dụng khi trả phòng
            var maKhuyenMai = string.IsNullOrWhiteSpace(request.MaKhuyenMai) ? null : request.MaKhuyenMai.Trim();
            if (maKhuyenMai != null)
            {
                var promo = await _context.KhuyenMais.FindAsync(maKhuyenMai);
                var today = DateTime.Today;
                if (promo == null || promo.TrangThai != PromotionStatus.Active || promo.NgayBatDau > today || promo.NgayKetThuc < today)
                    throw new BusinessException("Mã khuyến mãi không tồn tại, đã hết hạn hoặc chưa được kích hoạt.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var room = string.IsNullOrWhiteSpace(request.MaLoai)
                    ? await PickRoomAsync(request.MaPhong, checkIn, checkOut, soNguoi)
                    : await PickRoomOfTypeAsync(request.MaLoai, checkIn, checkOut, soNguoi);

                var booking = new PhieuDatPhong
                {
                    MaDatPhong = await IdGenerator.NextAsync(_context.PhieuDatPhongs.Select(p => p.MaDatPhong), "PDP", 7),
                    MaKhachHang = maKhachHang,
                    NgayDat = DateTime.Now,
                    NgayCheckInDuKien = checkIn,
                    NgayCheckOutDuKien = checkOut,
                    SoLuongKhach = soNguoi,
                    // Còn phòng thì xác nhận ngay (chính sách TuDongXacNhanDatPhong), lễ tân vẫn huỷ được khi cần
                    TrangThai = _policy.TuDongXacNhanDatPhong ? BookingStatus.Approved : BookingStatus.Pending,
                    TienCoc = 0,
                    MaKhuyenMai = maKhuyenMai,
                    YeuCauDacBiet = string.IsNullOrWhiteSpace(request.YeuCauDacBiet) ? null : request.YeuCauDacBiet.Trim(),
                    GioDenDuKien = string.IsNullOrWhiteSpace(request.GioDenDuKien) ? null : request.GioDenDuKien.Trim()
                };
                _context.PhieuDatPhongs.Add(booking);

                _context.ChiTietDatPhongs.Add(new ChiTietDatPhong
                {
                    MaCTDP = await IdGenerator.NextAsync(_context.ChiTietDatPhongs.Select(c => c.MaCTDP), "CT", 4),
                    MaDatPhong = booking.MaDatPhong,
                    MaPhong = room.MaPhong,
                    HinhThucThue = RentalType.Daily,
                    GiaThueThucTe = room.LoaiPhong.GiaThueNgay
                });

                // Không đổi Phong.TrangThai: phòng chỉ bị giữ trong khoảng ngày của phiếu
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return booking.MaDatPhong;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Đặt đích danh một phòng (giữ cho tương thích): khoá phòng rồi kiểm tra trùng lịch
        private async Task<Phong> PickRoomAsync(string maPhong, DateTime checkIn, DateTime checkOut, int soNguoi)
        {
            // Khoá phòng để hai khách không đặt trùng lịch cùng lúc
            await _availability.LockRoomAsync(maPhong);

            var room = await _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.MaPhong == maPhong)
                ?? throw new BusinessException("Phòng không tồn tại.");
            if (room.TrangThai == RoomStatus.Maintenance)
                throw new ConflictException("Phòng đang bảo trì, vui lòng chọn phòng khác.");
            if (room.LoaiPhong != null && soNguoi > room.LoaiPhong.SoNguoiToiDa)
                throw new BusinessException($"Số lượng khách ({soNguoi}) vượt quá sức chứa tối đa của {room.LoaiPhong.TenLoai} ({room.LoaiPhong.SoNguoiToiDa} người lớn).");

            await _availability.EnsureAvailableAsync(maPhong, checkIn, checkOut);
            return room;
        }

        // Đặt theo hạng phòng: xếp phòng trống đầu tiên của hạng (theo số phòng); lễ tân có thể đổi phòng trước khi nhận khách
        private async Task<Phong> PickRoomOfTypeAsync(string maLoai, DateTime checkIn, DateTime checkOut, int soNguoi)
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
                await _availability.LockRoomAsync(room.MaPhong);
                try
                {
                    await _availability.EnsureAvailableAsync(room.MaPhong, checkIn, checkOut);
                    return room;
                }
                catch (ConflictException)
                {
                    // Phòng này đã có khách trong khoảng ngày: thử phòng tiếp theo cùng hạng
                }
            }
            throw new ConflictException($"{loai.TenLoai} đã hết phòng trong khoảng ngày đã chọn. Vui lòng chọn ngày hoặc hạng phòng khác.");
        }

        public async Task<DanhGia> SubmitReviewAsync(string maKhachHang, string maDatPhong, string maPhong, int soSao, string noiDung, bool anDanh)
        {
            if (string.IsNullOrWhiteSpace(maDatPhong) && string.IsNullOrWhiteSpace(maPhong))
                throw new BusinessException("Vui lòng chọn kỳ nghỉ cần đánh giá.");

            // Các lần lưu trú đã trả phòng của khách (kèm phòng đã ở)
            var stays = _context.PhieuDatPhongs
                .Where(b => b.MaKhachHang == maKhachHang && b.TrangThai == BookingStatus.CheckedOut);
            var stay = !string.IsNullOrWhiteSpace(maDatPhong)
                ? await stays.Include(b => b.ChiTietDatPhongs).FirstOrDefaultAsync(b => b.MaDatPhong == maDatPhong)
                : await stays.Include(b => b.ChiTietDatPhongs)
                    .Where(b => b.ChiTietDatPhongs.Any(c => c.MaPhong == maPhong) && !_context.DanhGias.Any(d => d.MaDatPhong == b.MaDatPhong))
                    .OrderByDescending(b => b.NgayCheckOutThucTe)
                    .FirstOrDefaultAsync();

            if (stay == null)
            {
                // Đã ở phòng này nhưng mọi lần ở đều đã được đánh giá
                if (string.IsNullOrWhiteSpace(maDatPhong) && await stays.AnyAsync(b => b.ChiTietDatPhongs.Any(c => c.MaPhong == maPhong)))
                    throw new ConflictException("Bạn đã đánh giá các lần lưu trú ở phòng này.");
                throw new BusinessException("Bạn chỉ có thể đánh giá kỳ nghỉ của mình đã trả phòng.");
            }
            if (await _context.DanhGias.AnyAsync(d => d.MaDatPhong == stay.MaDatPhong))
                throw new ConflictException("Bạn đã đánh giá kỳ nghỉ này rồi.");

            var review = new DanhGia
            {
                MaDanhGia = await IdGenerator.NextAsync(_context.DanhGias.Select(d => d.MaDanhGia), "DG", 4),
                MaKhachHang = maKhachHang,
                MaPhong = stay.ChiTietDatPhongs.First().MaPhong,
                MaDatPhong = stay.MaDatPhong,
                SoSao = soSao,
                NoiDung = noiDung,
                NgayDanhGia = DateTime.Now,
                AnDanh = anDanh
            };
            _context.DanhGias.Add(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<IEnumerable<DanhGia>> GetReviewsByCustomerAsync(string maKhachHang)
            => await _context.DanhGias
                .Include(d => d.Phong)
                .Where(d => d.MaKhachHang == maKhachHang)
                .ToListAsync();

        public async Task<KhuyenMai> GetActivePromotionAsync(string code)
        {
            var promo = await _context.KhuyenMais.FindAsync(code?.Trim())
                ?? throw new NotFoundException("Mã khuyến mãi không tồn tại.");

            var today = DateTime.Today;
            if (promo.TrangThai != PromotionStatus.Active || promo.NgayBatDau > today || promo.NgayKetThuc < today)
                throw new BusinessException("Mã khuyến mãi đã hết hạn hoặc chưa được kích hoạt.");

            return promo;
        }
    }
}
