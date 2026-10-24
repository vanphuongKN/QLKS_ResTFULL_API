using Microsoft.EntityFrameworkCore;
using QLKSan.Models.Entities;

namespace QLKSan.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaiKhoan> TaiKhoans { get; set; }
        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<LienHeDiaChi> LienHeDiaChis { get; set; }
        public DbSet<LoaiPhong> LoaiPhongs { get; set; }
        public DbSet<Phong> Phongs { get; set; }
        public DbSet<TienIch> TienIchs { get; set; }
        public DbSet<PhongTienIch> PhongTienIchs { get; set; }
        public DbSet<HinhAnh> HinhAnhs { get; set; }
        public DbSet<DichVu> DichVus { get; set; }
        public DbSet<KhuyenMai> KhuyenMais { get; set; }
        public DbSet<PhieuDatPhong> PhieuDatPhongs { get; set; }
        public DbSet<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
        public DbSet<SuDungDichVu> SuDungDichVus { get; set; }
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<ThanhToan> ThanhToans { get; set; }
        public DbSet<LichSuGiaoDich> LichSuGiaoDichs { get; set; }
        public DbSet<DanhGia> DanhGias { get; set; }
        public DbSet<PhanCongDonPhong> PhanCongDonPhongs { get; set; }
        public DbSet<QuyTacGia> QuyTacGias { get; set; }
        public DbSet<HinhAnhLoaiPhong> HinhAnhLoaiPhongs { get; set; }
        public DbSet<LichSuDiem> LichSuDiems { get; set; }
        public DbSet<NhatKyThaoTac> NhatKyThaoTacs { get; set; }
        public DbSet<CauHinh> CauHinhs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Xóa bỏ số nhiều (pluralize) để map đúng với tên bảng số ít trong CSDL
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                modelBuilder.Entity(entityType.ClrType).ToTable(entityType.ClrType.Name);
            }

            // Explicit table mappings for tables with underscores in DB
            modelBuilder.Entity<LienHeDiaChi>().ToTable("LienHe_DiaChi");
            modelBuilder.Entity<PhongTienIch>().ToTable("Phong_TienIch");

            // Cấu hình Trigger trên bảng PhieuDatPhong (SQL Server có trigger trg_UpdateRoomStatus_CheckIn)
            modelBuilder.Entity<PhieuDatPhong>()
                .ToTable("PhieuDatPhong", tb => tb.HasTrigger("trg_UpdateRoomStatus_CheckIn"));

            // Configure Many-to-Many relationship for PhongTienIch
            modelBuilder.Entity<PhongTienIch>()
                .HasKey(pti => new { pti.MaPhong, pti.MaTienIch });

            // Value Converter cho Role (VaiTro) ánh xạ tới mã vai trò trong DB mới (VT01 = Admin, VT02 = Lễ tân, VT03 = Khách hàng)
            var roleConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<QLKSan.Enums.Role, string>(
                v => v == QLKSan.Enums.Role.Admin ? "Quản lý"
                   : v == QLKSan.Enums.Role.Receptionist ? "Lễ tân"
                   : v == QLKSan.Enums.Role.Housekeeping ? "Buồng phòng"
                   : v == QLKSan.Enums.Role.Accountant ? "Kế toán"
                   : "Khách hàng",
                v => v == "Quản lý" ? QLKSan.Enums.Role.Admin
                   : v == "Lễ tân" ? QLKSan.Enums.Role.Receptionist
                   : v == "Buồng phòng" ? QLKSan.Enums.Role.Housekeeping
                   : v == "Kế toán" ? QLKSan.Enums.Role.Accountant
                   : QLKSan.Enums.Role.Customer
            );

            modelBuilder.Entity<TaiKhoan>()
                .Property(t => t.VaiTro)
                .HasColumnName("VaiTro") // Ánh xạ thuộc tính VaiTro trong C# tới cột VaiTro trong DB
                .HasConversion(roleConverter);

            // Add converters for BookingStatus, RoomStatus, RentalType, InvoiceStatus
            var bookingStatusConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<QLKSan.Enums.BookingStatus, string>(
                v => v == QLKSan.Enums.BookingStatus.Pending ? "Chờ duyệt" : 
                     v == QLKSan.Enums.BookingStatus.Approved ? "Đã duyệt" :
                     v == QLKSan.Enums.BookingStatus.CheckedIn ? "Đã nhận phòng" : 
                     v == QLKSan.Enums.BookingStatus.CheckedOut ? "Đã trả phòng" : 
                     v == QLKSan.Enums.BookingStatus.Completed ? "Hoàn thành" :
                     v == QLKSan.Enums.BookingStatus.Cancelled ? "Đã hủy" : "Chờ duyệt",
                v => v == "Chờ duyệt" ? QLKSan.Enums.BookingStatus.Pending : 
                     v == "Đã duyệt" ? QLKSan.Enums.BookingStatus.Approved :
                     v == "Đã nhận phòng" ? QLKSan.Enums.BookingStatus.CheckedIn : 
                     v == "Đã trả phòng" ? QLKSan.Enums.BookingStatus.CheckedOut : 
                     v == "Hoàn thành" ? QLKSan.Enums.BookingStatus.Completed :
                     QLKSan.Enums.BookingStatus.Cancelled
            );

            modelBuilder.Entity<PhieuDatPhong>()
                .Property(p => p.TrangThai)
                .HasConversion(bookingStatusConverter);

            var roomStatusConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<QLKSan.Enums.RoomStatus, string>(
                v => v == QLKSan.Enums.RoomStatus.Available ? "Trống" : 
                     v == QLKSan.Enums.RoomStatus.Occupied ? "Đang ở" : 
                     v == QLKSan.Enums.RoomStatus.Cleaning ? "Chờ dọn dẹp" : 
                     v == QLKSan.Enums.RoomStatus.Maintenance ? "Bảo trì" : "Đã đặt",
                v => v == "Trống" ? QLKSan.Enums.RoomStatus.Available : 
                     v == "Đang ở" ? QLKSan.Enums.RoomStatus.Occupied : 
                     v == "Chờ dọn dẹp" ? QLKSan.Enums.RoomStatus.Cleaning : 
                     v == "Bảo trì" ? QLKSan.Enums.RoomStatus.Maintenance : 
                     QLKSan.Enums.RoomStatus.Booked
            );

            modelBuilder.Entity<Phong>()
                .Property(p => p.TrangThai)
                .HasConversion(roomStatusConverter);

            var rentalTypeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<QLKSan.Enums.RentalType, string>(
                v => v == QLKSan.Enums.RentalType.Daily ? "Theo ngày" : "Theo giờ",
                v => v == "Theo ngày" ? QLKSan.Enums.RentalType.Daily : QLKSan.Enums.RentalType.Hourly
            );

            modelBuilder.Entity<ChiTietDatPhong>()
                .Property(c => c.HinhThucThue)
                .HasConversion(rentalTypeConverter);

            var invoiceStatusConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<QLKSan.Enums.InvoiceStatus, string>(
                v => v == QLKSan.Enums.InvoiceStatus.Unpaid ? "Chưa thanh toán" : 
                     v == QLKSan.Enums.InvoiceStatus.Paid ? "Đã thanh toán" : "Đã hủy",
                v => v == "Chưa thanh toán" ? QLKSan.Enums.InvoiceStatus.Unpaid : 
                     v == "Đã thanh toán" ? QLKSan.Enums.InvoiceStatus.Paid : 
                     QLKSan.Enums.InvoiceStatus.Cancelled
            );

            modelBuilder.Entity<HoaDon>()
                .Property(h => h.TrangThai)
                .HasConversion(invoiceStatusConverter);

            // EmployeeStatus converter
            var employeeStatusConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<QLKSan.Enums.EmployeeStatus, string>(
                v => v == QLKSan.Enums.EmployeeStatus.Working ? "Đang làm việc" : "Nghỉ việc",
                v => v == "Đang làm việc" ? QLKSan.Enums.EmployeeStatus.Working : QLKSan.Enums.EmployeeStatus.Resigned
            );

            modelBuilder.Entity<NhanVien>()
                .Property(n => n.TrangThai)
                .HasConversion(employeeStatusConverter);

            // PromotionStatus converter (computed column trả về int: 1=Active, 0=Expired)
            var promotionStatusConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<QLKSan.Enums.PromotionStatus, int>(
                v => v == QLKSan.Enums.PromotionStatus.Active ? 1 : 0,
                v => v == 1 ? QLKSan.Enums.PromotionStatus.Active : QLKSan.Enums.PromotionStatus.Expired
            );

            modelBuilder.Entity<KhuyenMai>()
                .Property(k => k.TrangThai)
                .HasConversion(promotionStatusConverter)
                .ValueGeneratedOnAddOrUpdate();

            // TransactionStatus converter
            var transactionStatusConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<QLKSan.Enums.TransactionStatus, string>(
                v => v == QLKSan.Enums.TransactionStatus.Success ? "Thành công" : "Thất bại",
                v => v == "Thành công" ? QLKSan.Enums.TransactionStatus.Success : QLKSan.Enums.TransactionStatus.Failed
            );

            modelBuilder.Entity<LichSuGiaoDich>()
                .Property(l => l.TrangThaiGiaoDich)
                .HasConversion(transactionStatusConverter);

            // ServiceStatus converter
            var serviceStatusConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<QLKSan.Enums.ServiceStatus, string>(
                v => v == QLKSan.Enums.ServiceStatus.Available ? "Sẵn sàng" : "Đã hết",
                v => !string.IsNullOrEmpty(v) && v.Trim() == "Sẵn sàng" ? QLKSan.Enums.ServiceStatus.Available : QLKSan.Enums.ServiceStatus.Unavailable
            );

            modelBuilder.Entity<DichVu>()
                .Property(d => d.TrangThai)
                .HasConversion(serviceStatusConverter);

            // PaymentType converter (ThanhToan.LoaiGiaoDich)
            modelBuilder.Entity<ThanhToan>()
                .Property(t => t.LoaiGiaoDich)
                .HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<QLKSan.Enums.PaymentType, string>(
                    v => v == QLKSan.Enums.PaymentType.Refund ? "Hoàn" : "Thu",
                    v => v == "Hoàn" ? QLKSan.Enums.PaymentType.Refund : QLKSan.Enums.PaymentType.Payment));

            // HousekeepingTaskStatus converter (PhanCongDonPhong.TrangThai)
            modelBuilder.Entity<PhanCongDonPhong>()
                .Property(t => t.TrangThai)
                .HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<QLKSan.Enums.HousekeepingTaskStatus, string>(
                    v => v == QLKSan.Enums.HousekeepingTaskStatus.Assigned ? "Đã giao"
                       : v == QLKSan.Enums.HousekeepingTaskStatus.InProgress ? "Đang dọn"
                       : v == QLKSan.Enums.HousekeepingTaskStatus.Completed ? "Hoàn thành"
                       : v == QLKSan.Enums.HousekeepingTaskStatus.Issue ? "Sự cố"
                       : "Chờ giao",
                    v => v == "Đã giao" ? QLKSan.Enums.HousekeepingTaskStatus.Assigned
                       : v == "Đang dọn" ? QLKSan.Enums.HousekeepingTaskStatus.InProgress
                       : v == "Hoàn thành" ? QLKSan.Enums.HousekeepingTaskStatus.Completed
                       : v == "Sự cố" ? QLKSan.Enums.HousekeepingTaskStatus.Issue
                       : QLKSan.Enums.HousekeepingTaskStatus.Unassigned));

            // Configure Unique Constraints based on Database Schema
            modelBuilder.Entity<TaiKhoan>()
                .HasIndex(t => t.TenDangNhap)
                .IsUnique();

            modelBuilder.Entity<NhanVien>()
                .HasIndex(n => n.MaDangNhap)
                .IsUnique()
                .HasFilter("[MaDangNhap] IS NOT NULL");

            modelBuilder.Entity<KhachHang>()
                .HasIndex(k => k.MaDangNhap)
                .IsUnique()
                .HasFilter("[MaDangNhap] IS NOT NULL");

            modelBuilder.Entity<KhachHang>()
                .HasIndex(k => k.CCCD_Passport)
                .IsUnique()
                .HasFilter("[CCCD_Passport] IS NOT NULL");

            modelBuilder.Entity<Phong>()
                .ToTable("Phong", tb => tb.HasTrigger("trg_GhiLichSuGiaPhong"))
                .HasIndex(p => p.SoPhong)
                .IsUnique();

            modelBuilder.Entity<HoaDon>()
                .HasIndex(h => h.MaDatPhong)
                .IsUnique();

            // ThueVAT và TongTienCanThanhToan là computed columns (AS formula) trong SQL
            // EF không được phép INSERT/UPDATE các cột này
            modelBuilder.Entity<HoaDon>()
                .Property(h => h.ThueVAT)
                .ValueGeneratedOnAddOrUpdate();

            modelBuilder.Entity<HoaDon>()
                .Property(h => h.TongTienCanThanhToan)
                .ValueGeneratedOnAddOrUpdate();

            modelBuilder.Entity<HoaDon>()
                .Property(h => h.SoTienConLai)
                .HasColumnType("decimal(18,2)")
                .ValueGeneratedOnAddOrUpdate();

            modelBuilder.Entity<SuDungDichVu>()
                .Property(s => s.ThanhTien)
                .ValueGeneratedOnAddOrUpdate();

            // Prevent Cascade Delete for multiple paths
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // For decimal columns, specify precision
            modelBuilder.Entity<LoaiPhong>()
                .Property(p => p.GiaThueNgay)
                .HasColumnType("decimal(18,0)");

            modelBuilder.Entity<LoaiPhong>()
                .Property(p => p.GiaThueGio)
                .HasColumnType("decimal(18,0)");
        }
    }
}
