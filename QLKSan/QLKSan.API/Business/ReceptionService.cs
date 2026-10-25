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
using QLKSan.Extensions;

namespace QLKSan.Business
{
    public class ReceptionService : IReceptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoomAvailabilityService _availability;
        private readonly HotelPolicy _policy;
        private readonly IPricingService _pricing;
        private readonly IPromotionService _promotions;

        public ReceptionService(ApplicationDbContext context, IRoomAvailabilityService availability, IOptions<HotelPolicy> policy, IPricingService pricing,
            IPromotionService promotions)
        {
            _context = context;
            _availability = availability;
            _policy = policy.Value;
            _pricing = pricing;
            _promotions = promotions;
        }

        public async Task<IEnumerable<Phong>> GetAllRoomsAsync()
        {
            return await _context.Phongs
                .Include(p => p.LoaiPhong)
                .OrderBy(p => p.SoPhong)
                .ToListAsync();
        }

        public async Task<IEnumerable<PhieuDatPhong>> GetOnlineBookingsAsync()
        {
            return await _context.PhieuDatPhongs
                .Include(p => p.KhachHang).ThenInclude(k => k.LienHeDiaChis)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong).ThenInclude(r => r.LoaiPhong)
                .Where(p => p.TrangThai == BookingStatus.Pending || p.TrangThai == BookingStatus.Approved)
                .OrderBy(p => p.NgayCheckInDuKien)
                .ToListAsync();
        }

        public async Task ChangeRoomAsync(string maDatPhong, string maPhongMoi)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.PhieuDatPhongs.Include(p => p.ChiTietDatPhongs)
                    .FirstOrDefaultAsync(p => p.MaDatPhong == maDatPhong)
                    ?? throw new NotFoundException($"Không tìm thấy phiếu đặt phòng '{maDatPhong}'.");
                if (booking.TrangThai != BookingStatus.Pending && booking.TrangThai != BookingStatus.Approved)
                    throw new ConflictException("Chỉ đổi phòng cho phiếu chưa nhận phòng (chờ duyệt hoặc đã duyệt).");

                var chiTiet = booking.ChiTietDatPhongs.FirstOrDefault()
                    ?? throw new BusinessException("Phiếu đặt không gắn với phòng nào.");
                if (chiTiet.MaPhong == maPhongMoi) return;

                await _availability.LockRoomAsync(maPhongMoi);
                var room = await _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.MaPhong == maPhongMoi)
                    ?? throw new NotFoundException($"Không tìm thấy phòng '{maPhongMoi}'.");
                if (room.TrangThai == RoomStatus.Maintenance)
                    throw new ConflictException($"Phòng {room.SoPhong} đang bảo trì.");
                if (booking.SoLuongKhach > room.LoaiPhong.SoNguoiToiDa)
                    throw new BusinessException($"Phòng {room.SoPhong} chỉ chứa tối đa {room.LoaiPhong.SoNguoiToiDa} khách.");
                await _availability.EnsureAvailableAsync(maPhongMoi, booking.NgayCheckInDuKien, booking.NgayCheckOutDuKien);

                chiTiet.MaPhong = maPhongMoi;
                chiTiet.GiaThueThucTe = room.LoaiPhong.GiaThueNgay;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ApproveOnlineBookingAsync(string maDatPhong)
        {
            var booking = await _context.PhieuDatPhongs.FindAsync(maDatPhong)
                ?? throw new NotFoundException($"Không tìm thấy phiếu đặt phòng '{maDatPhong}'.");
            if (booking.TrangThai != BookingStatus.Pending)
                throw new ConflictException("Chỉ có thể duyệt phiếu đặt phòng đang chờ duyệt.");

            booking.TrangThai = BookingStatus.Approved;
            await _context.SaveChangesAsync();
        }

        public async Task CheckInOnlineBookingAsync(string maDatPhong, string maNhanVien, CheckInBookingRequest request = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.PhieuDatPhongs
                    .Include(p => p.ChiTietDatPhongs)
                    .FirstOrDefaultAsync(p => p.MaDatPhong == maDatPhong);

                if (booking == null)
                    throw new NotFoundException($"Không tìm thấy phiếu đặt phòng '{maDatPhong}'.");
                if (booking.TrangThai != BookingStatus.Pending && booking.TrangThai != BookingStatus.Approved)
                    throw new ConflictException("Chỉ có thể check-in phiếu đặt phòng đang chờ duyệt hoặc đã duyệt.");

                // Chỉ nhận phòng trong khoảng ngày đã đặt
                var today = DateTime.Today;
                if (today < booking.NgayCheckInDuKien.Date)
                    throw new BusinessException($"Chưa đến ngày nhận phòng ({booking.NgayCheckInDuKien:dd/MM/yyyy}).");
                if (today >= booking.NgayCheckOutDuKien.Date)
                    throw new BusinessException("Phiếu đã quá ngày trả phòng dự kiến, không thể check-in.");

                // Phòng phải trống và đã dọn sạch
                var maPhong = booking.ChiTietDatPhongs.FirstOrDefault()?.MaPhong;
                var room = await _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.MaPhong == maPhong)
                    ?? throw new BusinessException("Phiếu đặt không gắn với phòng nào.");
                if (room.TrangThai != RoomStatus.Available)
                {
                    var lyDo = room.TrangThai switch
                    {
                        RoomStatus.Occupied => "đang có khách",
                        RoomStatus.Cleaning => "đang chờ dọn dẹp",
                        RoomStatus.Maintenance => "đang bảo trì",
                        _ => "chưa sẵn sàng"
                    };
                    throw new ConflictException($"Phòng {room.SoPhong} {lyDo}, chưa thể nhận phòng.");
                }

                // Số khách thực tế khi nhận phòng (có thể khác lúc đặt) và tiền cọc / đảm bảo thu tại quầy
                if (request?.SoLuongKhach is int soKhach && soKhach != booking.SoLuongKhach)
                {
                    if (room.LoaiPhong != null && soKhach > room.LoaiPhong.SoNguoiToiDa)
                        throw new BusinessException($"Phòng {room.SoPhong} chỉ chứa tối đa {room.LoaiPhong.SoNguoiToiDa} khách.");
                    booking.SoLuongKhach = soKhach;
                }
                if (request != null && request.TienCoc > 0)
                {
                    ValidateDeposit(request.TienCoc, request.PhuongThucCoc);
                    RecordDeposit(booking, PaymentType.Payment, request.TienCoc, request.PhuongThucCoc, maNhanVien, "Cọc khi nhận phòng");
                }
                // Người đi cùng khai báo tại quầy (đưa vào sổ lưu trú)
                if (request?.KhachDiCung != null)
                    await StayService.ApplyCompanionsAsync(_context, booking, request.KhachDiCung);

                booking.TrangThai = BookingStatus.CheckedIn;
                booking.NgayCheckInThucTe = DateTime.Now;
                booking.MaNhanVien = maNhanVien;
                room.TrangThai = RoomStatus.Occupied;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task CancelBookingAsync(string maDatPhong, string lyDo, string nguoiHuy, bool laKhachHang, bool hoanCoc = false, string maNhanVien = null)
        {
            var booking = await _context.PhieuDatPhongs.FindAsync(maDatPhong)
                ?? throw new NotFoundException($"Không tìm thấy phiếu đặt phòng '{maDatPhong}'.");
            if (booking.TrangThai != BookingStatus.Pending && booking.TrangThai != BookingStatus.Approved)
                throw new ConflictException("Chỉ huỷ được phiếu chưa nhận phòng (đang chờ duyệt hoặc đã duyệt).");

            // Chính sách: khách tự huỷ được đến trước ngày nhận phòng; sau đó phải liên hệ lễ tân
            if (laKhachHang && DateTime.Today >= booking.NgayCheckInDuKien.Date)
                throw new BusinessException("Đã đến ngày nhận phòng, không thể tự huỷ. Vui lòng liên hệ lễ tân.");

            var hanhDong = booking.TrangThai == BookingStatus.Pending && !laKhachHang ? "Từ chối" : "Huỷ";
            booking.TrangThai = BookingStatus.Cancelled;
            booking.NgayHuy = DateTime.Now;
            booking.GhiChu = BookingNotes.Append(booking.GhiChu,
                $"[{hanhDong} {DateTime.Now:dd/MM/yyyy HH:mm} - {nguoiHuy}] {(string.IsNullOrWhiteSpace(lyDo) ? "Không ghi lý do" : lyDo.Trim())}");

            // Tiền cọc: lễ tân chọn hoàn lại (ghi dòng hoàn vào sổ cọc, cùng phương thức lần thu gần nhất) hoặc giữ theo chính sách
            if (booking.TienCoc > 0)
            {
                if (hoanCoc && !laKhachHang)
                {
                    var phuongThuc = await _context.GiaoDichCocs
                        .Where(g => g.MaDatPhong == maDatPhong && g.LoaiGiaoDich == PaymentType.Payment)
                        .OrderByDescending(g => g.ThoiGian).Select(g => g.PhuongThuc).FirstOrDefaultAsync();
                    var soTien = booking.TienCoc;
                    RecordDeposit(booking, PaymentType.Refund, soTien, phuongThuc ?? PaymentMethods.TienMat, maNhanVien, "Hoàn cọc khi huỷ phiếu");
                    booking.GhiChu = BookingNotes.Append(booking.GhiChu, $"[Hoàn cọc {soTien:N0}đ]");
                }
                else
                {
                    booking.GhiChu = BookingNotes.Append(booking.GhiChu, $"[Giữ cọc {booking.TienCoc:N0}đ theo chính sách huỷ]");
                }
            }

            // Phòng tự được giải phóng vì phòng trống được tính theo phiếu đang hiệu lực
            await _context.SaveChangesAsync();
        }

        public async Task<KhachHang> GetCustomerByCccdAsync(string cccd)
        {
            return await _context.KhachHangs.FirstOrDefaultAsync(k => k.CCCD_Passport == cccd);
        }

        public async Task<string> ProcessCheckInAsync(CheckInRequest model, string maNhanVien)
        {
            // Khách vãng lai nhận phòng ngay hôm nay
            var today = DateTime.Today;
            var ngayTra = model.NgayCheckOut.Date;
            if (model.NgayCheckIn.Date != today)
                throw new BusinessException("Khách vãng lai phải nhận phòng trong hôm nay.");
            if (ngayTra <= today)
                throw new BusinessException("Ngày trả phòng phải sau hôm nay.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Resolve Room (khoá phòng để tránh đặt trùng cùng lúc)
                await _availability.LockRoomAsync(model.MaPhong);
                var room = await _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.MaPhong == model.MaPhong);
                if (room == null || room.TrangThai != RoomStatus.Available)
                    throw new ConflictException("Phòng không khả dụng hoặc đã được đặt.");

                if (room.LoaiPhong != null && model.SoNguoi > room.LoaiPhong.SoNguoiToiDa)
                    throw new BusinessException($"Số lượng khách ({model.SoNguoi}) vượt quá sức chứa tối đa của {room.LoaiPhong.TenLoai} ({room.LoaiPhong.SoNguoiToiDa} người lớn).");

                // Không được trùng với phiếu đã đặt trước trong khoảng hôm nay → ngày trả
                await _availability.EnsureAvailableAsync(room.MaPhong, today, ngayTra);

                // 2. Resolve Customer (CCCD đã có hồ sơ thì dùng hồ sơ đó)
                var phuongThucCoc = string.IsNullOrWhiteSpace(model.PhuongThucCoc) ? PaymentMethods.TienMat : model.PhuongThucCoc;
                ValidateDeposit(1, phuongThucCoc);
                var customer = await ResolveCustomerAsync(model.Ho, model.Ten, model.CCCD_Passport, model.SDT, null);

                // 3. Create Booking (PhieuDatPhong) — cọc theo % tiền phòng dự kiến (đã áp dụng giá mùa/cuối tuần)
                var soDem = BillingCalculator.TinhSoDem(today, ngayTra);
                var giaTungDem = await _pricing.GetNightlyPricesAsync(room.MaLoai, room.LoaiPhong.GiaThueNgay, today, soDem);
                var tienCoc = Math.Round(giaTungDem.Sum(n => n.Gia) * _policy.PhanTramTienCocVangLai / 100m, 0);

                var booking = new PhieuDatPhong
                {
                    MaDatPhong = await IdGenerator.NextAsync(_context.PhieuDatPhongs.Select(p => p.MaDatPhong), "DP", 4),
                    MaKhachHang = customer.MaKhachHang,
                    MaNhanVien = maNhanVien,
                    NgayDat = DateTime.Now,
                    NgayCheckInDuKien = today,
                    NgayCheckOutDuKien = ngayTra,
                    NgayCheckInThucTe = DateTime.Now,
                    SoLuongKhach = model.SoNguoi,
                    TrangThai = BookingStatus.CheckedIn,
                    NguonDat = BookingSources.VangLai
                };
                _context.PhieuDatPhongs.Add(booking);
                if (tienCoc > 0)
                    RecordDeposit(booking, PaymentType.Payment, tienCoc, phuongThucCoc, maNhanVien, $"Cọc {_policy.PhanTramTienCocVangLai}% khách vãng lai");

                // 4. Create Booking Detail (ChiTietDatPhong)
                _context.ChiTietDatPhongs.Add(new ChiTietDatPhong
                {
                    MaCTDP = await IdGenerator.NextAsync(_context.ChiTietDatPhongs.Select(c => c.MaCTDP), "CT", 4),
                    MaDatPhong = booking.MaDatPhong,
                    MaPhong = room.MaPhong,
                    HinhThucThue = RentalType.Daily,
                    GiaThueThucTe = room.LoaiPhong.GiaThueNgay
                });

                // 5. Update Room Status
                room.TrangThai = RoomStatus.Occupied;
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

        public async Task<IEnumerable<PhieuDatPhong>> GetAllBookingsAsync(string search = null)
        {
            var query = _context.PhieuDatPhongs
                .Include(p => p.KhachHang).ThenInclude(k => k.LienHeDiaChis)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong).ThenInclude(r => r.LoaiPhong)
                .Include(p => p.HoaDon)
                .AsQueryable();

            var result = await query.OrderByDescending(p => p.NgayDat).ToListAsync();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                result = result.Where(p => 
                    (p.MaDatPhong?.ContainsIgnoreCaseAndAccents(search) ?? false) ||
                    ((p.KhachHang.Ho + " " + p.KhachHang.Ten).ContainsIgnoreCaseAndAccents(search)) ||
                    p.ChiTietDatPhongs.Any(c => c.Phong.SoPhong.ContainsIgnoreCaseAndAccents(search)))
                .ToList();
            }

            return result;
        }

        public async Task<List<Models.DTOs.TimelineBookingDto>> GetTimelineAsync(DateTime tuNgay, DateTime denNgay)
        {
            var from = tuNgay.Date;
            var toExclusive = denNgay.Date.AddDays(1);
            var today = DateTime.Today;
            var bookings = await _context.ChiTietDatPhongs
                .Include(c => c.PhieuDatPhong).ThenInclude(p => p.KhachHang)
                .Where(c => c.PhieuDatPhong.TrangThai == BookingStatus.Pending || c.PhieuDatPhong.TrangThai == BookingStatus.Approved
                         || c.PhieuDatPhong.TrangThai == BookingStatus.CheckedIn || c.PhieuDatPhong.TrangThai == BookingStatus.CheckedOut)
                .ToListAsync();

            return bookings.Select(c =>
            {
                var p = c.PhieuDatPhong;
                // Đã trả: theo ngày thực tế; đang ở: tới ngày trả dự kiến (ít nhất là ngày mai); chưa đến: theo ngày dự kiến
                var start = (p.NgayCheckInThucTe ?? p.NgayCheckInDuKien).Date;
                var end = p.TrangThai == BookingStatus.CheckedOut && p.NgayCheckOutThucTe != null
                    ? p.NgayCheckOutThucTe.Value.Date
                    : RoomAvailabilityService.NgayTraHieuLuc(p.TrangThai, p.NgayCheckOutDuKien, today);
                if (end <= start) end = start.AddDays(1);
                return new Models.DTOs.TimelineBookingDto
                {
                    MaDatPhong = p.MaDatPhong,
                    MaPhong = c.MaPhong,
                    HoTenKhachHang = $"{p.KhachHang?.Ho} {p.KhachHang?.Ten}".Trim(),
                    TuNgay = start,
                    DenNgay = end,
                    TrangThai = p.TrangThai
                };
            })
            .Where(b => b.TuNgay < toExclusive && b.DenNgay > from)
            .OrderBy(b => b.MaPhong).ThenBy(b => b.TuNgay)
            .ToList();
        }

        public async Task<PhieuDatPhong> GetBookingByIdAsync(string maDatPhong)
        {
            return await _context.PhieuDatPhongs
                .Include(p => p.KhachHang).ThenInclude(k => k.LienHeDiaChis)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong).ThenInclude(r => r.LoaiPhong)
                .Include(p => p.SuDungDichVus).ThenInclude(s => s.DichVu)
                .Include(p => p.HoaDon).ThenInclude(h => h.ThanhToans)
                .Include(p => p.GiaoDichCocs).ThenInclude(g => g.NhanVien)
                .Include(p => p.KhachDiCungs)
                .Include(p => p.LichSuDoiPhongs).ThenInclude(l => l.PhongCu)
                .Include(p => p.LichSuDoiPhongs).ThenInclude(l => l.PhongMoi)
                .Include(p => p.LichSuDoiPhongs).ThenInclude(l => l.NhanVien)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.MaDatPhong == maDatPhong);
        }

        public async Task<string> CreateFrontDeskBookingAsync(FrontDeskBookingRequest r, string maNhanVien)
        {
            var checkIn = r.CheckIn.Date;
            var checkOut = r.CheckOut.Date;
            if (string.IsNullOrWhiteSpace(r.MaLoai) == string.IsNullOrWhiteSpace(r.MaPhong))
                throw new BusinessException("Vui lòng chọn một hạng phòng hoặc một phòng cụ thể.");
            if (checkIn < DateTime.Today)
                throw new BusinessException("Ngày nhận phòng không được ở trong quá khứ.");
            if (checkOut <= checkIn)
                throw new BusinessException("Ngày trả phòng phải sau ngày nhận phòng.");
            if (!BookingSources.QuaQuay.Contains(r.NguonDat))
                throw new BusinessException("Nguồn đặt phòng không hợp lệ.");
            ValidateDeposit(r.TienCoc, r.PhuongThucCoc);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var room = string.IsNullOrWhiteSpace(r.MaLoai)
                    ? await _availability.ReserveRoomAsync(r.MaPhong, checkIn, checkOut, r.SoNguoi)
                    : await _availability.ReserveRoomOfTypeAsync(r.MaLoai, checkIn, checkOut, r.SoNguoi);
                var maKhuyenMai = string.IsNullOrWhiteSpace(r.MaKhuyenMai) ? null
                    : (await _promotions.ValidateForStayAsync(r.MaKhuyenMai.Trim(), room.MaLoai, (checkOut - checkIn).Days)).MaKhuyenMai;
                var customer = await ResolveCustomerAsync(r.Ho, r.Ten, r.CCCD_Passport, r.SDT, r.Email);

                var booking = new PhieuDatPhong
                {
                    MaDatPhong = await IdGenerator.NextAsync(_context.PhieuDatPhongs.Select(p => p.MaDatPhong), "DP", 4),
                    MaKhachHang = customer.MaKhachHang,
                    MaNhanVien = maNhanVien,
                    NgayDat = DateTime.Now,
                    NgayCheckInDuKien = checkIn,
                    NgayCheckOutDuKien = checkOut,
                    SoLuongKhach = r.SoNguoi,
                    TrangThai = BookingStatus.Approved,
                    NguonDat = r.NguonDat,
                    MaKhuyenMai = maKhuyenMai,
                    YeuCauDacBiet = Clean(r.YeuCauDacBiet),
                    GioDenDuKien = Clean(r.GioDenDuKien)
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
                if (r.TienCoc > 0)
                    RecordDeposit(booking, PaymentType.Payment, r.TienCoc, r.PhuongThucCoc, maNhanVien, "Cọc khi đặt phòng");

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

        public async Task AddDepositAsync(string maDatPhong, DepositRequest request, string maNhanVien)
        {
            var booking = await _context.PhieuDatPhongs.FindAsync(maDatPhong)
                ?? throw new NotFoundException($"Không tìm thấy phiếu đặt phòng '{maDatPhong}'.");
            if (booking.TrangThai is not (BookingStatus.Pending or BookingStatus.Approved or BookingStatus.CheckedIn))
                throw new ConflictException("Chỉ thu cọc cho phiếu chưa trả phòng.");
            ValidateDeposit(request.SoTien, request.PhuongThuc);
            // Hoá đơn chưa thanh toán sẽ lấy lại tiền cọc mới khi lễ tân mở màn hình trả phòng
            RecordDeposit(booking, PaymentType.Payment, request.SoTien, request.PhuongThuc, maNhanVien, Clean(request.GhiChu) ?? "Thu thêm cọc");
            await _context.SaveChangesAsync();
        }

        public async Task<List<KhachHang>> FindCustomersAsync(string q)
        {
            q = q?.Trim();
            if (string.IsNullOrEmpty(q) || q.Length < 6) return new List<KhachHang>();
            return await _context.KhachHangs
                .Include(k => k.LienHeDiaChis)
                .Include(k => k.PhieuDatPhongs)
                .Where(k => k.CCCD_Passport == q || k.LienHeDiaChis.Any(l => l.SDT == q))
                .OrderBy(k => k.Ten)
                .Take(5)
                .AsSplitQuery()
                .ToListAsync();
        }

        // CCCD đã có hồ sơ thì dùng hồ sơ đó (bổ sung liên hệ nếu hồ sơ chưa có); chưa có thì tạo khách mới kèm liên hệ
        private async Task<KhachHang> ResolveCustomerAsync(string ho, string ten, string cccd, string sdt, string email)
        {
            var customer = await _context.KhachHangs.Include(k => k.LienHeDiaChis).FirstOrDefaultAsync(k => k.CCCD_Passport == cccd);
            if (customer == null)
            {
                customer = new KhachHang
                {
                    MaKhachHang = await IdGenerator.NextAsync(_context.KhachHangs.Select(k => k.MaKhachHang), "KH", 4),
                    Ho = ho.Trim(),
                    Ten = ten.Trim(),
                    CCCD_Passport = cccd,
                    NgaySinh = DateTime.Now.AddYears(-25)
                };
                _context.KhachHangs.Add(customer);
            }
            if (customer.LienHeDiaChis == null || customer.LienHeDiaChis.Count == 0)
            {
                _context.LienHeDiaChis.Add(new LienHeDiaChi
                {
                    MaLienHe = await IdGenerator.NextAsync(_context.LienHeDiaChis.Select(l => l.MaLienHe), "LH", 4),
                    MaKhachHang = customer.MaKhachHang,
                    SDT = sdt,
                    Email = Clean(email)
                });
            }
            return customer;
        }

        private static void ValidateDeposit(decimal soTien, string phuongThuc)
        {
            if (soTien > 0 && !PaymentMethods.TaiQuay.Contains(phuongThuc))
                throw new BusinessException("Vui lòng chọn phương thức thu cọc (tiền mặt, thẻ tín dụng hoặc chuyển khoản).");
        }

        // Ghi một dòng sổ cọc và cập nhật tổng cọc của phiếu (thu cộng, hoàn trừ)
        private void RecordDeposit(PhieuDatPhong booking, PaymentType loai, decimal soTien, string phuongThuc, string maNhanVien, string ghiChu)
        {
            _context.GiaoDichCocs.Add(new GiaoDichCoc
            {
                MaDatPhong = booking.MaDatPhong,
                LoaiGiaoDich = loai,
                SoTien = soTien,
                PhuongThuc = phuongThuc,
                ThoiGian = DateTime.Now,
                MaNhanVien = maNhanVien,
                GhiChu = ghiChu
            });
            booking.TienCoc += loai == PaymentType.Refund ? -soTien : soTien;
        }

        private static string Clean(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        public async Task<IEnumerable<KhachHang>> GetAllCustomersAsync(string search = null)
        {
            var query = _context.KhachHangs
                .Include(k => k.LienHeDiaChis)
                .Include(k => k.PhieuDatPhongs)
                .AsQueryable();

            var result = await query.OrderBy(k => k.Ten).ToListAsync();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                result = result.Where(k => 
                    ((k.Ho + " " + k.Ten).ContainsIgnoreCaseAndAccents(search)) ||
                    (k.CCCD_Passport?.ContainsIgnoreCaseAndAccents(search) ?? false) ||
                    k.LienHeDiaChis.Any(l => l.SDT.ContainsIgnoreCaseAndAccents(search)))
                .ToList();
            }

            return result;
        }

        public async Task<KhachHang> GetCustomerDetailsAsync(string id)
        {
            return await _context.KhachHangs
                .Include(k => k.LienHeDiaChis)
                .Include(k => k.PhieuDatPhongs)
                    .ThenInclude(p => p.ChiTietDatPhongs)
                        .ThenInclude(c => c.Phong)
                            .ThenInclude(r => r.LoaiPhong)
                .Include(k => k.PhieuDatPhongs).ThenInclude(p => p.HoaDon)
                .AsSplitQuery()
                .FirstOrDefaultAsync(k => k.MaKhachHang == id);
        }
    }
}
