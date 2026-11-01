using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Messaging;
using QLKSan.Models.Events;
using QLKSan.Models.Entities;
using QLKSan.Models.Requests;

namespace QLKSan.Business
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoomAvailabilityService _availability;
        private readonly HotelPolicy _policy;
        private readonly IPromotionService _promotions;
        private readonly IEventOutbox _events;

        public CustomerService(ApplicationDbContext context, IRoomAvailabilityService availability, IOptions<HotelPolicy> policy, IPromotionService promotions,
            IEventOutbox events)
        {
            _events = events;
            _context = context;
            _availability = availability;
            _policy = policy.Value;
            _promotions = promotions;
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
            // Sở thích lưu trú: chỉ cập nhật khi gửi kèm (chuỗi rỗng = xoá)
            if (request.SoThich != null)
                khach.SoThich = string.IsNullOrWhiteSpace(request.SoThich) ? null : request.SoThich.Trim();
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

            if (await _context.KhachHangs.AnyAsync(k => k.MaKhachHang == maKhachHang && k.HanChe))
                throw new BusinessException("Tài khoản tạm thời không đặt phòng trực tuyến được. Vui lòng liên hệ lễ tân để được hỗ trợ.");

            // Mã khuyến mãi phải hợp lệ cho kỳ nghỉ này lúc đặt (kiểm tra sau khi xếp phòng để biết hạng phòng);
            // được lưu vào phiếu để áp dụng khi trả phòng
            var maKhuyenMai = string.IsNullOrWhiteSpace(request.MaKhuyenMai) ? null : request.MaKhuyenMai.Trim();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var room = string.IsNullOrWhiteSpace(request.MaLoai)
                    ? await _availability.ReserveRoomAsync(request.MaPhong, checkIn, checkOut, soNguoi)
                    : await _availability.ReserveRoomOfTypeAsync(request.MaLoai, checkIn, checkOut, soNguoi);
                if (maKhuyenMai != null)
                    maKhuyenMai = (await _promotions.ValidateForStayAsync(maKhuyenMai, room.MaLoai, (checkOut - checkIn).Days)).MaKhuyenMai;

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
                    NguonDat = BookingSources.Online,
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
                await _events.AddBookingEventAsync(BookingEventTypes.DatPhongMoi, booking.MaDatPhong);
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
    }
}
