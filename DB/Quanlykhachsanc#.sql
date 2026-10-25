/* =====================================================================
   CƠ SỞ DỮ LIỆU QUẢN LÝ KHÁCH SẠN — QuanLyKhachSanc#
   - Chạy 1 lần trên SQL Server (SSMS: mở file → Execute / F5).
   - Script XOÁ database cũ (nếu có) rồi tạo lại toàn bộ bảng + dữ liệu mẫu.
   - Dữ liệu mẫu tính THEO NGÀY CHẠY SCRIPT: luôn có khách đang ở, khách đến hôm nay,
     phiếu sắp tới, lịch sử đã thanh toán... nên chạy lại lúc nào cũng demo được.
   - Mọi tài khoản mẫu có mật khẩu: 123456 (lưu dạng BCrypt, không lưu chữ thường).
     admin1/admin2 (Quản lý), letan1-3 (Lễ tân), buongphong1 (Buồng phòng), ketoan1 (Kế toán), khach1-5 (Khách hàng)
   ===================================================================== */
-- Bắt buộc cho index có lọc (SSMS bật sẵn, sqlcmd thì không)
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE master;
GO

IF DB_ID(N'QuanLyKhachSanc#') IS NOT NULL
BEGIN
    -- Ngắt các kết nối đang mở (vd: API đang chạy) để xoá được database
    ALTER DATABASE [QuanLyKhachSanc#] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [QuanLyKhachSanc#];
END
GO

CREATE DATABASE [QuanLyKhachSanc#];
GO

USE [QuanLyKhachSanc#];
GO

/* =====================================================================
   PHẦN 1: CẤU TRÚC BẢNG
   ===================================================================== */

-- 1. NHÓM TÀI KHOẢN & NHÂN SỰ
CREATE TABLE TaiKhoan (
    MaDangNhap VARCHAR(10) PRIMARY KEY,
    TenDangNhap NVARCHAR(50) NOT NULL CONSTRAINT UQ_TaiKhoan_TenDangNhap UNIQUE,
    MatKhau NVARCHAR(255) NOT NULL,                 -- chuỗi BCrypt
    VaiTro NVARCHAR(50) DEFAULT N'Khách hàng',      -- Quản lý | Lễ tân | Buồng phòng | Kế toán | Khách hàng
    SoLanSai INT NOT NULL DEFAULT 0,                -- đăng nhập sai liên tiếp
    KhoaDen DATETIME NULL                           -- khoá tạm đến thời điểm này
);

CREATE TABLE NhanVien (
    MaNhanVien VARCHAR(10) PRIMARY KEY,
    MaDangNhap VARCHAR(10) NULL,                    -- NULL: nhân viên không dùng phần mềm (vd: buồng phòng)
    TenNhanVien NVARCHAR(100) NOT NULL,
    ChucVu NVARCHAR(50),
    SDT VARCHAR(15),
    Email VARCHAR(100),
    NgayVaoLam DATE DEFAULT GETDATE(),
    TrangThai NVARCHAR(50) DEFAULT N'Đang làm việc',
    CONSTRAINT FK_NhanVien_TaiKhoan FOREIGN KEY (MaDangNhap) REFERENCES TaiKhoan(MaDangNhap)
);
-- UNIQUE cho cột cho phép NULL: dùng index có lọc để nhiều dòng NULL không bị coi là trùng
CREATE UNIQUE INDEX UX_NhanVien_MaDangNhap ON NhanVien(MaDangNhap) WHERE MaDangNhap IS NOT NULL;

-- 2. NHÓM KHÁCH HÀNG
CREATE TABLE KhachHang (
    MaKhachHang VARCHAR(10) PRIMARY KEY,
    MaDangNhap VARCHAR(10) NULL,                    -- NULL: khách vãng lai không có tài khoản
    Ho NVARCHAR(50) NOT NULL,
    Ten NVARCHAR(50) NOT NULL,
    NgaySinh DATE,
    CCCD_Passport VARCHAR(20) NOT NULL,
    DiemTichLuy INT DEFAULT 0,                      -- số điểm hiện có (đổi được)
    DiemDaDoi INT NOT NULL DEFAULT 0,               -- tổng điểm đã đổi; hạng thành viên xét theo DiemTichLuy + DiemDaDoi
    LaVIP BIT NOT NULL DEFAULT 0,
    SoThich NVARCHAR(500) NULL,                     -- sở thích lưu trú (khách tự cập nhật được)
    GhiChuNoiBo NVARCHAR(1000) NULL,                -- chỉ nhân viên thấy
    HanChe BIT NOT NULL DEFAULT 0,                  -- không cho đặt phòng trực tuyến
    LyDoHanChe NVARCHAR(255) NULL,
    CONSTRAINT FK_KhachHang_TaiKhoan FOREIGN KEY (MaDangNhap) REFERENCES TaiKhoan(MaDangNhap)
);
CREATE UNIQUE INDEX UX_KhachHang_MaDangNhap ON KhachHang(MaDangNhap) WHERE MaDangNhap IS NOT NULL;
CREATE UNIQUE INDEX UX_KhachHang_CCCD ON KhachHang(CCCD_Passport);

CREATE TABLE LienHe_DiaChi (
    MaLienHe VARCHAR(10) PRIMARY KEY,
    MaKhachHang VARCHAR(10) NOT NULL,
    SDT VARCHAR(15) NOT NULL,
    Email VARCHAR(100),
    DiaChi NVARCHAR(255),
    QuocGia NVARCHAR(50) DEFAULT N'Việt Nam',
    CONSTRAINT FK_LienHe_KhachHang FOREIGN KEY (MaKhachHang) REFERENCES KhachHang(MaKhachHang)
);

-- 3. NHÓM PHÒNG & DỊCH VỤ
CREATE TABLE LoaiPhong (
    MaLoai VARCHAR(10) PRIMARY KEY,
    TenLoai NVARCHAR(50) NOT NULL,
    SoNguoiToiDa INT CHECK (SoNguoiToiDa > 0),
    GiaThueNgay DECIMAL(18,0) CHECK (GiaThueNgay >= 0),
    GiaThueGio DECIMAL(18,0) CHECK (GiaThueGio >= 0),
    MoTa NVARCHAR(500),                             -- mô tả ngắn (thẻ hạng phòng)
    DienTich DECIMAL(6,1) NULL,                     -- m²
    LoaiGiuong NVARCHAR(100) NULL,
    HuongNhin NVARCHAR(100) NULL,
    MoTaChiTiet NVARCHAR(MAX) NULL,                 -- trang chi tiết hạng phòng
    AnhDaiDien NVARCHAR(255) NULL                   -- ảnh đầu tiên của bộ ảnh (HinhAnhLoaiPhong)
);

-- TrangThai là trạng thái VẬT LÝ của phòng: Trống | Đang ở | Chờ dọn dẹp | Bảo trì
-- ("Đã đặt" không lưu ở đây — hệ thống tự suy ra từ phiếu đặt theo khoảng ngày)
CREATE TABLE Phong (
    MaPhong VARCHAR(10) PRIMARY KEY,
    MaLoai VARCHAR(10) NOT NULL,
    SoPhong VARCHAR(10) UNIQUE NOT NULL,
    TrangThai NVARCHAR(50) DEFAULT N'Trống',
    CONSTRAINT FK_Phong_LoaiPhong FOREIGN KEY (MaLoai) REFERENCES LoaiPhong(MaLoai)
);

-- Quy tắc điều chỉnh giá theo mùa / đêm trong tuần / dịp lễ.
-- Mỗi đêm áp dụng 1 quy tắc khớp có UuTien cao nhất (vd: Lễ ưu tiên hơn Cuối tuần)
CREATE TABLE QuyTacGia (
    MaQuyTac VARCHAR(10) PRIMARY KEY,
    TenQuyTac NVARCHAR(100) NOT NULL,
    MaLoai VARCHAR(10) NULL,                        -- NULL = mọi loại phòng
    TuNgay DATE NULL,                               -- NULL = không giới hạn
    DenNgay DATE NULL,
    CacThu VARCHAR(20) NULL,                        -- đêm trong tuần, vd '5,6' = thứ Sáu, thứ Bảy (0 = Chủ nhật); NULL = mọi đêm
    PhanTramDieuChinh DECIMAL(5,2) NOT NULL,        -- +20 = tăng 20%, -15 = giảm 15%
    UuTien INT NOT NULL DEFAULT 1,
    KichHoat BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_QuyTacGia_LoaiPhong FOREIGN KEY (MaLoai) REFERENCES LoaiPhong(MaLoai),
    CONSTRAINT CK_QuyTacGia_PhanTram CHECK (PhanTramDieuChinh BETWEEN -90 AND 300)
);

CREATE TABLE TienIch (
    MaTienIch VARCHAR(10) PRIMARY KEY,
    TenTienIch NVARCHAR(100) NOT NULL,
    Icon VARCHAR(50)
);

CREATE TABLE Phong_TienIch (
    MaPhong VARCHAR(10),
    MaTienIch VARCHAR(10),
    PRIMARY KEY (MaPhong, MaTienIch),
    CONSTRAINT FK_PTI_Phong FOREIGN KEY (MaPhong) REFERENCES Phong(MaPhong),
    CONSTRAINT FK_PTI_TienIch FOREIGN KEY (MaTienIch) REFERENCES TienIch(MaTienIch)
);

CREATE TABLE HinhAnh (
    MaHinhAnh VARCHAR(10) PRIMARY KEY,
    MaPhong VARCHAR(10) NOT NULL,
    DuongDanAnh VARCHAR(255) NOT NULL,
    CONSTRAINT FK_HinhAnh_Phong FOREIGN KEY (MaPhong) REFERENCES Phong(MaPhong)
);

-- Bộ ảnh giới thiệu của hạng phòng; ảnh do quản trị tải lên có đường dẫn /uploads/... (lưu ở QLKSan.API)
CREATE TABLE HinhAnhLoaiPhong (
    MaHinhAnh VARCHAR(10) PRIMARY KEY,
    MaLoai VARCHAR(10) NOT NULL,
    DuongDan NVARCHAR(255) NOT NULL,
    ThuTu INT NOT NULL DEFAULT 0,
    CONSTRAINT FK_HinhAnhLoaiPhong_LoaiPhong FOREIGN KEY (MaLoai) REFERENCES LoaiPhong(MaLoai) ON DELETE CASCADE
);

CREATE TABLE DichVu (
    MaDichVu VARCHAR(10) PRIMARY KEY,
    TenDichVu NVARCHAR(100) NOT NULL,
    GiaDichVu DECIMAL(18,0) CHECK (GiaDichVu >= 0),
    DonViTinh NVARCHAR(20),
    TrangThai NVARCHAR(50) DEFAULT N'Sẵn sàng',     -- Sẵn sàng | Đã hết
    LaTraiNghiem BIT NOT NULL DEFAULT 0,            -- giới thiệu ở mục "Trải nghiệm" trang chủ (không ghi giá)
    LaMinibar BIT NOT NULL DEFAULT 0,               -- đồ minibar: nhân viên buồng phòng ghi vào bảng kê của khách
    MoTa NVARCHAR(500) NULL
);

-- 4. NHÓM KHUYẾN MÃI
CREATE TABLE KhuyenMai (
    MaKhuyenMai VARCHAR(10) PRIMARY KEY,
    TenKhuyenMai NVARCHAR(100) NOT NULL,
    LoaiGiam NVARCHAR(20),                          -- Phần trăm (giảm % tiền phòng) | Số tiền
    GiaTriGiam DECIMAL(18,2) CHECK (GiaTriGiam >= 0),
    NgayBatDau DATE,
    NgayKetThuc DATE,
    -- 1 = đang áp dụng. So sánh theo NGÀY để còn hiệu lực trọn ngày kết thúc
    TrangThai AS (CASE WHEN CAST(GETDATE() AS DATE) BETWEEN NgayBatDau AND NgayKetThuc THEN 1 ELSE 0 END),
    KichHoat BIT NOT NULL DEFAULT 1,                -- quản trị tắt thì không áp dụng dù còn hạn
    SoLuotToiDa INT NULL,                           -- điều kiện áp dụng (NULL = không giới hạn)
    SoDemToiThieu INT NULL,
    MaLoai VARCHAR(10) NULL,                        -- chỉ áp dụng cho hạng phòng này
    CONSTRAINT FK_KhuyenMai_LoaiPhong FOREIGN KEY (MaLoai) REFERENCES LoaiPhong(MaLoai)
);

-- 5. NHÓM GIAO DỊCH
CREATE TABLE PhieuDatPhong (
    MaDatPhong VARCHAR(10) PRIMARY KEY,             -- PDP...: đặt online, DP...: lễ tân tạo (vãng lai / đặt qua quầy)
    MaKhachHang VARCHAR(10) NOT NULL,
    MaNhanVien VARCHAR(10) NULL,                    -- nhân viên check-in
    NgayDat DATETIME DEFAULT GETDATE(),
    NgayCheckInDuKien DATETIME NOT NULL,
    NgayCheckOutDuKien DATETIME NOT NULL,
    NgayCheckInThucTe DATETIME NULL,
    NgayCheckOutThucTe DATETIME NULL,
    SoLuongKhach INT CHECK (SoLuongKhach > 0),
    TrangThai NVARCHAR(50) DEFAULT N'Chờ duyệt',    -- Chờ duyệt | Đã duyệt | Đã nhận phòng | Đã trả phòng | Đã hủy
    TienCoc DECIMAL(18,0) DEFAULT 0,
    GhiChu NVARCHAR(MAX),
    MaKhuyenMai VARCHAR(10) NULL,                   -- mã khách chọn khi đặt online, áp dụng lúc lập hoá đơn
    YeuCauDacBiet NVARCHAR(500) NULL,               -- yêu cầu riêng của khách khi đặt online
    GioDenDuKien NVARCHAR(30) NULL,                 -- khung giờ dự kiến đến
    NguonDat NVARCHAR(30) NULL,                     -- Online | Điện thoại | Email | Tại quầy | Đại lý | Vãng lai
    KhongLamPhienNgay DATE NULL,                    -- buồng phòng: khách "Không làm phiền" ngày này
    GioDonMongMuon NVARCHAR(30) NULL,               -- giờ dọn khách muốn
    TuChoiDonHangNgay BIT NOT NULL DEFAULT 0,       -- khách từ chối dọn hằng ngày cả kỳ nghỉ
    NgayHuy DATETIME NULL,                          -- thời điểm huỷ phiếu
    NgayGhiNhanCocGiuLai DATE NULL,                 -- ngày chốt đã ghi nhận tiền cọc giữ lại thành doanh thu
    CONSTRAINT FK_PhieuDat_KhachHang FOREIGN KEY (MaKhachHang) REFERENCES KhachHang(MaKhachHang),
    CONSTRAINT FK_PhieuDat_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien)
);

CREATE TABLE ChiTietDatPhong (
    MaCTDP VARCHAR(10) PRIMARY KEY,
    MaDatPhong VARCHAR(10) NOT NULL,
    MaPhong VARCHAR(10) NOT NULL,
    HinhThucThue NVARCHAR(20) CHECK (HinhThucThue IN (N'Theo ngày', N'Theo giờ')),
    GiaThueThucTe DECIMAL(18,0) NOT NULL,
    CONSTRAINT FK_CTDP_PhieuDat FOREIGN KEY (MaDatPhong) REFERENCES PhieuDatPhong(MaDatPhong),
    CONSTRAINT FK_CTDP_Phong FOREIGN KEY (MaPhong) REFERENCES Phong(MaPhong)
);

CREATE TABLE SuDungDichVu (
    MaSDDV VARCHAR(10) PRIMARY KEY,
    MaDatPhong VARCHAR(10) NOT NULL,
    MaDichVu VARCHAR(10) NOT NULL,
    SoLuong INT CHECK (SoLuong > 0),
    NgaySuDung DATETIME DEFAULT GETDATE(),
    GiaThucTe DECIMAL(18,0) NOT NULL,
    ThanhTien AS (SoLuong * GiaThucTe),
    CONSTRAINT FK_SDDV_PhieuDat FOREIGN KEY (MaDatPhong) REFERENCES PhieuDatPhong(MaDatPhong),
    CONSTRAINT FK_SDDV_DichVu FOREIGN KEY (MaDichVu) REFERENCES DichVu(MaDichVu)
);

-- 6. NHÓM THANH TOÁN & ĐÁNH GIÁ
CREATE TABLE HoaDon (
    MaHoaDon VARCHAR(10) PRIMARY KEY,
    MaDatPhong VARCHAR(10) NOT NULL UNIQUE,
    MaNhanVienLap VARCHAR(10) NOT NULL,
    NgayLapHoaDon DATETIME DEFAULT GETDATE(),
    TongTienPhong DECIMAL(18,0) DEFAULT 0,          -- tổng giá các đêm thực tế (đã áp dụng quy tắc giá)
    SoDem INT NOT NULL DEFAULT 0,                   -- số đêm tính tiền thực tế
    TongTienDichVu DECIMAL(18,0) DEFAULT 0,
    MaKhuyenMai VARCHAR(10) NULL,
    GiamGiaKhuyenMai DECIMAL(18,0) DEFAULT 0,
    TienPhat DECIMAL(18,0) DEFAULT 0,               -- phụ thu trả phòng muộn
    GiamGiaThanhVien DECIMAL(18,0) NOT NULL DEFAULT 0, -- ưu đãi hạng thành viên (% tiền phòng)
    HangThanhVien NVARCHAR(50) NULL,                -- hạng của khách lúc lập hoá đơn
    ThueVAT AS ((TongTienPhong + TongTienDichVu + TienPhat - GiamGiaKhuyenMai - GiamGiaThanhVien) * 0.08),
    TongTienCanThanhToan AS ((TongTienPhong + TongTienDichVu + TienPhat - GiamGiaKhuyenMai - GiamGiaThanhVien) * 1.08),
    TienCoc DECIMAL(18,0) NOT NULL DEFAULT 0,       -- tiền cọc của phiếu đặt
    SoTienConLai AS ((TongTienPhong + TongTienDichVu + TienPhat - GiamGiaKhuyenMai - GiamGiaThanhVien) * 1.08 - TienCoc),
    TrangThai NVARCHAR(50) DEFAULT N'Chưa thanh toán',
    TenCongTy NVARCHAR(200) NULL,                   -- xuất hoá đơn VAT cho công ty (trống = cá nhân)
    MaSoThue VARCHAR(20) NULL,
    DiaChiCongTy NVARCHAR(300) NULL,
    EmailHoaDon NVARCHAR(100) NULL,
    SoHoaDonDienTu NVARCHAR(30) NULL,               -- số hoá đơn điện tử kế toán đã xuất cho công ty
    NgayXuatHoaDonDienTu DATETIME NULL,
    CONSTRAINT FK_HoaDon_PhieuDat FOREIGN KEY (MaDatPhong) REFERENCES PhieuDatPhong(MaDatPhong),
    CONSTRAINT FK_HoaDon_KhuyenMai FOREIGN KEY (MaKhuyenMai) REFERENCES KhuyenMai(MaKhuyenMai),
    CONSTRAINT FK_HoaDon_NhanVien FOREIGN KEY (MaNhanVienLap) REFERENCES NhanVien(MaNhanVien)
);

CREATE TABLE ThanhToan (
    MaThanhToan VARCHAR(10) PRIMARY KEY,
    MaHoaDon VARCHAR(10) NOT NULL,
    PhuongThuc NVARCHAR(50),
    NgayThanhToan DATETIME DEFAULT GETDATE(),
    SoTienDaTra DECIMAL(18,0) CHECK (SoTienDaTra >= 0),   -- luôn dương, kể cả khi hoàn tiền
    GhiChu NVARCHAR(255),
    LoaiGiaoDich NVARCHAR(20) NOT NULL DEFAULT N'Thu',    -- Thu | Hoàn
    MaNhanVien VARCHAR(10) NULL,                          -- nhân viên thu/hoàn (đối soát, chốt ca)
    CONSTRAINT FK_ThanhToan_HoaDon FOREIGN KEY (MaHoaDon) REFERENCES HoaDon(MaHoaDon),
    CONSTRAINT FK_ThanhToan_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien)
);

CREATE TABLE LichSuGiaoDich (
    MaGiaoDich VARCHAR(50) PRIMARY KEY,
    MaThanhToan VARCHAR(10),
    CongThanhToan NVARCHAR(50),
    SoTien DECIMAL(18,0),
    TrangThaiGiaoDich NVARCHAR(50),
    ThoiGianGiaoDich DATETIME DEFAULT GETDATE(),
    NoiDungChuyenKhoan NVARCHAR(255),
    CONSTRAINT FK_LSGD_ThanhToan FOREIGN KEY (MaThanhToan) REFERENCES ThanhToan(MaThanhToan)
);

CREATE TABLE DanhGia (
    MaDanhGia VARCHAR(10) PRIMARY KEY,
    MaKhachHang VARCHAR(10) NOT NULL,
    MaPhong VARCHAR(10) NOT NULL,
    SoSao INT CHECK (SoSao BETWEEN 1 AND 5),
    NoiDung NVARCHAR(1000),
    NgayDanhGia DATETIME DEFAULT GETDATE(),
    AnDanh BIT DEFAULT 0,
    MaDatPhong VARCHAR(10) NULL,                    -- lần lưu trú được đánh giá (mỗi phiếu 1 đánh giá)
    PhanHoi NVARCHAR(1000) NULL,                    -- phản hồi công khai của khách sạn
    NgayPhanHoi DATETIME NULL,
    MaNhanVienPhanHoi VARCHAR(10) NULL,
    An BIT NOT NULL DEFAULT 0,                      -- ẩn khỏi trang khách (vẫn tính vào điểm trung bình)
    CONSTRAINT FK_DanhGia_NhanVien FOREIGN KEY (MaNhanVienPhanHoi) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT FK_DanhGia_KhachHang FOREIGN KEY (MaKhachHang) REFERENCES KhachHang(MaKhachHang),
    CONSTRAINT FK_DanhGia_Phong FOREIGN KEY (MaPhong) REFERENCES Phong(MaPhong),
    CONSTRAINT FK_DanhGia_PhieuDatPhong FOREIGN KEY (MaDatPhong) REFERENCES PhieuDatPhong(MaDatPhong)
);
CREATE UNIQUE INDEX UX_DanhGia_MaDatPhong ON DanhGia(MaDatPhong) WHERE MaDatPhong IS NOT NULL;

-- 7. NHÓM BUỒNG PHÒNG: công việc dọn phòng giao cho nhân viên buồng phòng
-- Cấu hình nghiệp vụ sửa trên web (HotelInfo / HotelPolicy / Loyalty, mỗi mục một JSON). Chưa có dòng thì API dùng appsettings.json
CREATE TABLE CauHinh (
    Khoa NVARCHAR(50) PRIMARY KEY,
    GiaTri NVARCHAR(MAX) NOT NULL,
    NgayCapNhat DATETIME NOT NULL DEFAULT GETDATE(),
    MaNhanVien VARCHAR(10) NULL
);

-- Nhật ký thao tác của người dùng (ghi tự động bởi API)
CREATE TABLE NhatKyThaoTac (
    MaNhatKy BIGINT IDENTITY(1,1) PRIMARY KEY,
    ThoiGian DATETIME NOT NULL DEFAULT GETDATE(),
    MaDangNhap VARCHAR(10) NULL,
    TenDangNhap NVARCHAR(50) NULL,
    VaiTro NVARCHAR(20) NULL,
    HanhDong NVARCHAR(100) NOT NULL,
    DoiTuong NVARCHAR(50) NULL,
    MaDoiTuong NVARCHAR(100) NULL,
    ChiTiet NVARCHAR(1000) NULL,
    DiaChiIP VARCHAR(50) NULL
);
CREATE INDEX IX_NhatKy_ThoiGian ON NhatKyThaoTac(ThoiGian DESC);

-- Lịch sử điểm thưởng (tổng các dòng của một khách = số điểm hiện có)
CREATE TABLE LichSuDiem (
    MaLichSu VARCHAR(12) PRIMARY KEY,
    MaKhachHang VARCHAR(10) NOT NULL,
    ThoiGian DATETIME NOT NULL DEFAULT GETDATE(),
    SoDiem INT NOT NULL,                            -- dương: cộng, âm: trừ
    Loai NVARCHAR(30) NOT NULL,                     -- Tích điểm | Đổi điểm | Hoàn tiền | Điều chỉnh | Số dư đầu kỳ
    NoiDung NVARCHAR(255) NULL,
    MaHoaDon VARCHAR(10) NULL,
    MaNhanVien VARCHAR(10) NULL,
    CONSTRAINT FK_LichSuDiem_KhachHang FOREIGN KEY (MaKhachHang) REFERENCES KhachHang(MaKhachHang),
    CONSTRAINT FK_LichSuDiem_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien)
);
CREATE INDEX IX_LichSuDiem_KhachHang ON LichSuDiem(MaKhachHang, ThoiGian);

-- Sổ tiền cọc: mỗi lần thu / hoàn cọc một dòng (PhieuDatPhong.TienCoc = tổng thu - tổng hoàn); dùng tổng hợp tiền theo ca
CREATE TABLE GiaoDichCoc (
    MaGiaoDichCoc INT IDENTITY(1,1) PRIMARY KEY,
    MaDatPhong VARCHAR(10) NOT NULL,
    LoaiGiaoDich NVARCHAR(20) NOT NULL DEFAULT N'Thu',    -- Thu | Hoàn
    SoTien DECIMAL(18,0) NOT NULL CHECK (SoTien >= 0),
    PhuongThuc NVARCHAR(50) NULL,
    ThoiGian DATETIME NOT NULL DEFAULT GETDATE(),
    MaNhanVien VARCHAR(10) NULL,
    GhiChu NVARCHAR(255) NULL,
    CONSTRAINT FK_GiaoDichCoc_PhieuDat FOREIGN KEY (MaDatPhong) REFERENCES PhieuDatPhong(MaDatPhong),
    CONSTRAINT FK_GiaoDichCoc_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien)
);
CREATE INDEX IX_GiaoDichCoc_PhieuDat ON GiaoDichCoc(MaDatPhong);
CREATE INDEX IX_GiaoDichCoc_ThoiGian ON GiaoDichCoc(ThoiGian);

-- Người đi cùng khách đứng tên phiếu (sổ khai báo lưu trú đủ người)
CREATE TABLE KhachDiCung (
    MaKhachDiCung INT IDENTITY(1,1) PRIMARY KEY,
    MaDatPhong VARCHAR(10) NOT NULL,
    HoTen NVARCHAR(100) NOT NULL,
    CCCD_Passport VARCHAR(20) NULL,
    QuocTich NVARCHAR(50) NULL,
    CONSTRAINT FK_KhachDiCung_PhieuDat FOREIGN KEY (MaDatPhong) REFERENCES PhieuDatPhong(MaDatPhong)
);
CREATE INDEX IX_KhachDiCung_PhieuDat ON KhachDiCung(MaDatPhong);

-- Lịch sử đổi phòng khi khách đang ở
CREATE TABLE LichSuDoiPhong (
    MaLichSu INT IDENTITY(1,1) PRIMARY KEY,
    MaDatPhong VARCHAR(10) NOT NULL,
    TuPhong VARCHAR(10) NOT NULL,
    DenPhong VARCHAR(10) NOT NULL,
    ThoiGian DATETIME NOT NULL DEFAULT GETDATE(),
    LyDo NVARCHAR(255) NULL,
    GiaCu DECIMAL(18,0) NOT NULL,
    GiaMoi DECIMAL(18,0) NOT NULL,
    MaNhanVien VARCHAR(10) NULL,
    CONSTRAINT FK_LichSuDoiPhong_PhieuDat FOREIGN KEY (MaDatPhong) REFERENCES PhieuDatPhong(MaDatPhong),
    CONSTRAINT FK_LichSuDoiPhong_PhongCu FOREIGN KEY (TuPhong) REFERENCES Phong(MaPhong),
    CONSTRAINT FK_LichSuDoiPhong_PhongMoi FOREIGN KEY (DenPhong) REFERENCES Phong(MaPhong),
    CONSTRAINT FK_LichSuDoiPhong_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien)
);
CREATE INDEX IX_LichSuDoiPhong_PhieuDat ON LichSuDoiPhong(MaDatPhong);

-- Sổ yêu cầu khách tại quầy (báo thức, đặt xe, nhắn gửi, đồ thất lạc…)
CREATE TABLE YeuCauKhach (
    MaYeuCau INT IDENTITY(1,1) PRIMARY KEY,
    MaDatPhong VARCHAR(10) NULL,
    MaPhong VARCHAR(10) NULL,
    Loai NVARCHAR(30) NOT NULL,
    NoiDung NVARCHAR(500) NOT NULL,
    ThoiGianHen DATETIME NULL,
    TrangThai NVARCHAR(20) NOT NULL DEFAULT N'Mới',   -- Mới | Đang xử lý | Hoàn thành | Đã huỷ
    MaNhanVienTao VARCHAR(10) NULL,
    ThoiGianTao DATETIME NOT NULL DEFAULT GETDATE(),
    MaNhanVienXuLy VARCHAR(10) NULL,
    ThoiGianXuLy DATETIME NULL,
    KetQua NVARCHAR(500) NULL,
    CONSTRAINT FK_YeuCauKhach_PhieuDat FOREIGN KEY (MaDatPhong) REFERENCES PhieuDatPhong(MaDatPhong),
    CONSTRAINT FK_YeuCauKhach_Phong FOREIGN KEY (MaPhong) REFERENCES Phong(MaPhong),
    CONSTRAINT FK_YeuCauKhach_NVTao FOREIGN KEY (MaNhanVienTao) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT FK_YeuCauKhach_NVXuLy FOREIGN KEY (MaNhanVienXuLy) REFERENCES NhanVien(MaNhanVien)
);
CREATE INDEX IX_YeuCauKhach_TrangThai ON YeuCauKhach(TrangThai, ThoiGianHen);

-- Chốt ca lễ tân: tiền thu theo phương thức, tiền mặt hệ thống so với tiền đếm thực tế, ghi chú bàn giao
CREATE TABLE GiaoCa (
    MaGiaoCa INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien VARCHAR(10) NOT NULL,
    BatDau DATETIME NOT NULL,
    KetThuc DATETIME NOT NULL,
    TongThuRong DECIMAL(18,0) NOT NULL,
    TienMatHeThong DECIMAL(18,0) NOT NULL,
    TienMatThucTe DECIMAL(18,0) NOT NULL,
    ChenhLech DECIMAL(18,0) NOT NULL,
    GhiChuBanGiao NVARCHAR(1000) NULL,
    ChiTiet NVARCHAR(MAX) NULL,                     -- tổng theo phương thức lúc chốt (JSON)
    CONSTRAINT FK_GiaoCa_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien)
);
CREATE INDEX IX_GiaoCa_KetThuc ON GiaoCa(KetThuc DESC);

-- Phiếu kỹ thuật: sự cố cần sửa trong phòng; KhoaPhong = phòng chuyển Bảo trì tới khi sửa xong
CREATE TABLE PhieuKyThuat (
    MaPhieu INT IDENTITY(1,1) PRIMARY KEY,
    MaPhong VARCHAR(10) NOT NULL,
    MoTa NVARCHAR(500) NOT NULL,
    MucDo NVARCHAR(20) NOT NULL DEFAULT N'Thường',  -- Thường | Khẩn
    KhoaPhong BIT NOT NULL DEFAULT 0,
    TrangThai NVARCHAR(20) NOT NULL DEFAULT N'Mới', -- Mới | Đang sửa | Đã xong | Đã huỷ
    MaNhanVienBao VARCHAR(10) NULL,
    ThoiGianBao DATETIME NOT NULL DEFAULT GETDATE(),
    MaNhanVienXuLy VARCHAR(10) NULL,
    ThoiGianXong DATETIME NULL,
    KetQua NVARCHAR(500) NULL,
    MaPhanCong VARCHAR(10) NULL,                    -- việc buồng phòng báo sự cố
    MaYeuCau INT NULL,                              -- yêu cầu khách loại Kỹ thuật
    CONSTRAINT FK_PhieuKyThuat_Phong FOREIGN KEY (MaPhong) REFERENCES Phong(MaPhong),
    CONSTRAINT FK_PhieuKyThuat_NVBao FOREIGN KEY (MaNhanVienBao) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT FK_PhieuKyThuat_NVXuLy FOREIGN KEY (MaNhanVienXuLy) REFERENCES NhanVien(MaNhanVien)
);
CREATE INDEX IX_PhieuKyThuat_Phong ON PhieuKyThuat(MaPhong, TrangThai);

-- Chốt ngày (night audit): ảnh chụp số liệu từng ngày đã chốt, chốt lần lượt
CREATE TABLE ChotNgay (
    Ngay DATE PRIMARY KEY,
    ThoiGianChot DATETIME NOT NULL CONSTRAINT DF_ChotNgay_ThoiGian DEFAULT GETDATE(),
    MaNhanVien VARCHAR(10) NULL,
    TongSoPhong INT NOT NULL,
    DemPhongBan INT NOT NULL,
    CongSuat DECIMAL(5,1) NOT NULL,
    TienPhong DECIMAL(18,0) NOT NULL,               -- doanh thu phòng theo đêm (giá đêm, trước giảm giá, chưa VAT)
    ADR DECIMAL(18,0) NOT NULL,
    RevPAR DECIMAL(18,0) NOT NULL,
    DoanhThuKhac DECIMAL(18,0) NOT NULL,            -- cọc giữ lại khi huỷ / không đến
    TienDichVu DECIMAL(18,0) NOT NULL,
    SoHoaDonThanhToan INT NOT NULL,
    TongHoaDonThanhToan DECIMAL(18,0) NOT NULL,
    KhachDen INT NOT NULL,
    KhachDi INT NOT NULL,
    KhachKhongDen INT NOT NULL,
    GhiChu NVARCHAR(500) NULL,
    ChiTiet NVARCHAR(MAX) NULL,                     -- theo phương thức, từng đêm, cọc giữ lại (JSON)
    CONSTRAINT FK_ChotNgay_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien)
);

-- Doanh thu ghi nhận khi chốt ngày: Tiền phòng (từng đêm) | Cọc giữ lại
CREATE TABLE DoanhThuDem (
    MaDong INT IDENTITY(1,1) PRIMARY KEY,
    Ngay DATE NOT NULL,
    MaDatPhong VARCHAR(10) NOT NULL,
    MaPhong VARCHAR(10) NULL,
    Loai NVARCHAR(30) NOT NULL,
    SoTien DECIMAL(18,0) NOT NULL,
    GhiChu NVARCHAR(100) NULL,
    CONSTRAINT FK_DoanhThuDem_PhieuDat FOREIGN KEY (MaDatPhong) REFERENCES PhieuDatPhong(MaDatPhong)
);
CREATE INDEX IX_DoanhThuDem_Ngay ON DoanhThuDem(Ngay);

CREATE TABLE PhanCongDonPhong (
    MaPhanCong VARCHAR(10) PRIMARY KEY,
    MaPhong VARCHAR(10) NOT NULL,
    MaNhanVien VARCHAR(10) NULL,                          -- NULL = chưa giao
    MaDatPhong VARCHAR(10) NULL,                          -- phiếu vừa trả phòng tạo ra công việc
    LoaiCongViec NVARCHAR(50) NOT NULL DEFAULT N'Dọn sau trả phòng',
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Chờ giao',  -- Chờ giao | Đã giao | Đang dọn | Chờ kiểm tra | Hoàn thành | Sự cố
    ThoiGianTao DATETIME NOT NULL DEFAULT GETDATE(),
    ThoiGianGiao DATETIME NULL,
    ThoiGianBatDau DATETIME NULL,
    ThoiGianHoanThanh DATETIME NULL,
    GhiChu NVARCHAR(500) NULL,
    MaYeuCau INT NULL,                                    -- yêu cầu khách (Buồng phòng) mà việc này xử lý
    ThoiGianKiemTra DATETIME NULL,                        -- kiểm tra phòng sau khi dọn
    MaNhanVienKiemTra VARCHAR(10) NULL,
    KetQuaKiemTra NVARCHAR(500) NULL,
    SoLanKhongDat INT NOT NULL DEFAULT 0,
    CONSTRAINT FK_PCDP_NVKiemTra FOREIGN KEY (MaNhanVienKiemTra) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT FK_PCDP_Phong FOREIGN KEY (MaPhong) REFERENCES Phong(MaPhong),
    CONSTRAINT FK_PCDP_YeuCauKhach FOREIGN KEY (MaYeuCau) REFERENCES YeuCauKhach(MaYeuCau),
    CONSTRAINT FK_PCDP_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT FK_PCDP_PhieuDat FOREIGN KEY (MaDatPhong) REFERENCES PhieuDatPhong(MaDatPhong)
);
GO

-- Trigger dự phòng: đổi trạng thái phòng khi phiếu chuyển sang Đã nhận phòng / Đã trả phòng
CREATE TRIGGER trg_UpdateRoomStatus_CheckIn
ON PhieuDatPhong
AFTER UPDATE
AS
BEGIN
    IF UPDATE(TrangThai)
    BEGIN
        UPDATE Phong
        SET TrangThai = N'Đang ở'
        WHERE MaPhong IN (SELECT MaPhong FROM ChiTietDatPhong WHERE MaDatPhong IN (SELECT MaDatPhong FROM inserted WHERE TrangThai = N'Đã nhận phòng'));

        UPDATE Phong
        SET TrangThai = N'Chờ dọn dẹp'
        WHERE MaPhong IN (SELECT MaPhong FROM ChiTietDatPhong WHERE MaDatPhong IN (SELECT MaDatPhong FROM inserted WHERE TrangThai = N'Đã trả phòng'));
    END
END;
GO

/* =====================================================================
   PHẦN 2: DỮ LIỆU MẪU (tính theo ngày chạy script)
   ===================================================================== */
SET NOCOUNT ON;

DECLARE @Nay DATETIME = GETDATE();
DECLARE @H DATETIME = CAST(CAST(@Nay AS DATE) AS DATETIME);        -- 0h hôm nay
DECLARE @MatKhau NVARCHAR(255) = N'$2a$11$b/UQH7Egup51cwdTOyEgS.85blTwvM0IMu8c/zVkcAwiPD3dnjO7C';  -- BCrypt của "123456"

-- ---------- 1. TÀI KHOẢN & NHÂN VIÊN ----------
INSERT INTO TaiKhoan (MaDangNhap, TenDangNhap, MatKhau, VaiTro) VALUES
('TK01', N'admin1', @MatKhau, N'Quản lý'),
('TK02', N'letan1', @MatKhau, N'Lễ tân'),
('TK03', N'khach1', @MatKhau, N'Khách hàng'),
('TK04', N'khach2', @MatKhau, N'Khách hàng'),
('TK05', N'khach3', @MatKhau, N'Khách hàng'),
('TK06', N'letan2', @MatKhau, N'Lễ tân'),
('TK07', N'letan3', @MatKhau, N'Lễ tân'),
('TK08', N'admin2', @MatKhau, N'Quản lý'),
('TK09', N'khach4', @MatKhau, N'Khách hàng'),
('TK10', N'khach5', @MatKhau, N'Khách hàng'),
('TK11', N'buongphong1', @MatKhau, N'Buồng phòng'),
('TK12', N'ketoan1', @MatKhau, N'Kế toán');

INSERT INTO NhanVien (MaNhanVien, MaDangNhap, TenNhanVien, ChucVu, SDT, Email, NgayVaoLam, TrangThai) VALUES
('NV01', 'TK01', N'Nguyễn Hoàng Đức', N'Quản lý',               '0901234567', 'admin1@ute.udn.vn',  '2023-01-15', N'Đang làm việc'),
('NV02', 'TK02', N'Trần Thị Bích',    N'Lễ tân',                '0901234568', 'letan1@ute.udn.vn',  '2024-02-01', N'Đang làm việc'),
('NV03', 'TK06', N'Lê Văn Cường',     N'Lễ tân',                '0901234569', 'letan2@ute.udn.vn',  '2024-03-10', N'Đang làm việc'),
('NV04', 'TK07', N'Phạm Thị Duyên',   N'Lễ tân',                '0901234570', 'letan3@ute.udn.vn',  '2024-04-05', N'Đang làm việc'),
('NV05', 'TK08', N'Hoàng Văn Ân',     N'Quản lý',               '0901234571', 'admin2@ute.udn.vn',  '2025-05-20', N'Đang làm việc'),
('NV06', 'TK11', N'Ngô Thị Mai',      N'Nhân viên buồng phòng', '0901234572', 'mai.ngo@ute.udn.vn', '2025-01-10', N'Đang làm việc'),
('NV07', NULL,   N'Đỗ Văn Tâm',       N'Nhân viên buồng phòng', '0901234573', 'tam.do@ute.udn.vn',  '2025-06-01', N'Đang làm việc'),
('NV08', 'TK12', N'Vũ Thị Lan',       N'Kế toán',               '0901234574', 'lan.vu@ute.udn.vn',  '2024-08-15', N'Đang làm việc');

-- ---------- 2. KHÁCH HÀNG (KH01-05 có tài khoản, KH06-07 khách vãng lai) ----------
INSERT INTO KhachHang (MaKhachHang, MaDangNhap, Ho, Ten, NgaySinh, CCCD_Passport, DiemTichLuy) VALUES
('KH01', 'TK03', N'Võ',     N'Văn Phương', '2004-10-15', '048204000123', 0),
('KH02', 'TK04', N'Lê',     N'Thị Cẩm',    '1995-05-20', '048195000456', 0),
('KH03', 'TK05', N'Nguyễn', N'Văn Hùng',   '1988-12-05', '048188000789', 0),
('KH04', 'TK09', N'Trần',   N'Đại',        '1990-08-08', '048190000111', 0),
('KH05', 'TK10', N'Phạm',   N'Hương',      '2000-01-01', '048200000222', 0),
('KH06', NULL,   N'Đặng',   N'Minh Khoa',  '1985-03-12', '079085001234', 0),
('KH07', NULL,   N'Bùi',    N'Thu Trang',  '1998-07-25', '001198005678', 0);

INSERT INTO LienHe_DiaChi (MaLienHe, MaKhachHang, SDT, Email, DiaChi, QuocGia) VALUES
('LH01', 'KH01', '0911111111', 'phuong.vo@gmail.com',   N'Hải Châu, Đà Nẵng',     N'Việt Nam'),
('LH02', 'KH02', '0922222222', 'cam.le@gmail.com',      N'Sơn Trà, Đà Nẵng',      N'Việt Nam'),
('LH03', 'KH03', '0933333333', 'hung.nguyen@gmail.com', N'Liên Chiểu, Đà Nẵng',   N'Việt Nam'),
('LH04', 'KH04', '0944444444', 'dai.tran@gmail.com',    N'Ngũ Hành Sơn, Đà Nẵng', N'Việt Nam'),
('LH05', 'KH05', '0955555555', 'huong.pham@gmail.com',  N'Cẩm Lệ, Đà Nẵng',       N'Việt Nam'),
('LH06', 'KH06', '0966666666', NULL,                    NULL,                     N'Việt Nam'),
('LH07', 'KH07', '0977777777', NULL,                    NULL,                     N'Việt Nam');

-- ---------- 3. LOẠI PHÒNG, PHÒNG, TIỆN ÍCH, HÌNH ẢNH, DỊCH VỤ ----------
INSERT INTO LoaiPhong (MaLoai, TenLoai, SoNguoiToiDa, GiaThueNgay, GiaThueGio, MoTa, DienTich, LoaiGiuong, HuongNhin, MoTaChiTiet, AnhDaiDien) VALUES
('LP01', N'Phòng Tiêu Chuẩn',        2,  500000, 100000, N'Không gian ấm cúng nhìn ra vườn nhiệt đới, đủ tiện nghi cho chuyến đi ngắn ngày.',
         28, N'1 giường đôi', N'Hướng vườn',
         N'Phòng Tiêu Chuẩn mang gam gỗ ấm và ánh sáng dịu, mở ra khoảng vườn xanh yên tĩnh. Phù hợp cho chuyến công tác hoặc kỳ nghỉ ngắn của hai người, với đầy đủ tiện nghi thiết yếu được chuẩn bị chu đáo trước giờ nhận phòng.',
         '/images/room_garden.png'),
('LP02', N'Phòng Superior Ban Công', 2,  800000, 150000, N'Ban công riêng đón gió biển, nội thất gỗ và ánh sáng tự nhiên.',
         34, N'1 giường đôi lớn', N'Ban công hướng biển một phần',
         N'Phòng Superior có ban công riêng để thư thái mỗi chiều, nội thất gỗ tự nhiên và cửa kính lớn đón trọn ánh sáng. Lựa chọn cân bằng giữa không gian và chi phí cho các cặp đôi.',
         '/images/room_ocean.png'),
('LP03', N'Phòng Deluxe Hướng Biển', 3, 1200000, 200000, N'Tầm nhìn toàn cảnh biển, không gian rộng cho ba khách.',
         42, N'1 giường đôi lớn + 1 giường đơn', N'Hướng biển',
         N'Phòng Deluxe mở ra tầm nhìn biển trọn vẹn từ giường ngủ và ban công. Không gian rộng rãi, phù hợp cho ba khách hoặc gia đình nhỏ muốn tận hưởng bình minh trên biển ngay trong phòng.',
         '/images/room_ocean.png'),
('LP04', N'Suite Hoàng Gia',         4, 2500000, 400000, N'Phòng khách riêng biệt, cửa kính lớn nhìn ra biển.',
         68, N'1 giường King', N'Toàn cảnh biển',
         N'Suite Hoàng Gia gồm phòng ngủ và phòng khách tách biệt, cửa kính kịch trần nhìn ra biển. Không gian dành cho những dịp đặc biệt, kỳ nghỉ dài ngày hoặc khách cần sự riêng tư tuyệt đối.',
         '/images/room_presidential.png'),
('LP05', N'Phòng Gia Đình',          4, 1500000, 250000, N'Hai giường đôi, không gian rộng cho cả gia đình.',
         48, N'2 giường đôi', N'Hướng vườn',
         N'Phòng Gia Đình có hai giường đôi và khu sinh hoạt chung rộng rãi, thuận tiện cho gia đình có trẻ nhỏ. Gần khu vườn và hồ bơi, dễ dàng cho những buổi chiều vui chơi cùng nhau.',
         '/images/room_garden.png');

-- Bộ ảnh mẫu theo hạng phòng (thay bằng ảnh thật trong trang Quản trị → Loại phòng)
INSERT INTO HinhAnhLoaiPhong (MaHinhAnh, MaLoai, DuongDan, ThuTu) VALUES
('HL01', 'LP01', '/images/room_garden.png', 1),
('HL02', 'LP02', '/images/room_ocean.png', 1),
('HL03', 'LP03', '/images/room_ocean.png', 1),
('HL04', 'LP03', '/images/hero.png', 2),
('HL05', 'LP04', '/images/room_presidential.png', 1),
('HL06', 'LP04', '/images/hero.png', 2),
('HL07', 'LP05', '/images/room_garden.png', 1),
('HL08', 'LP05', '/images/hero.png', 2);

-- Trạng thái phòng khớp với phiếu đặt bên dưới
INSERT INTO Phong (MaPhong, MaLoai, SoPhong, TrangThai) VALUES
('P101', 'LP01', '101', N'Trống'),        -- có phiếu chờ duyệt nhận phòng ngày mai
('P102', 'LP02', '102', N'Trống'),        -- có khách đặt online đến nhận phòng HÔM NAY
('P103', 'LP01', '103', N'Bảo trì'),
('P201', 'LP03', '201', N'Đang ở'),       -- khách online đang lưu trú
('P202', 'LP04', '202', N'Trống'),        -- có phiếu đã duyệt sau 5 ngày
('P203', 'LP03', '203', N'Trống'),
('P301', 'LP05', '301', N'Trống'),
('P302', 'LP02', '302', N'Chờ dọn dẹp'),  -- khách vừa trả phòng sáng nay
('P303', 'LP05', '303', N'Đang ở');       -- khách vãng lai đang lưu trú

INSERT INTO TienIch (MaTienIch, TenTienIch, Icon) VALUES
('TI01', N'Wifi tốc độ cao',   'fa-wifi'),
('TI02', N'Tivi thông minh',   'fa-tv'),
('TI03', N'Điều hòa nhiệt độ', 'fa-snowflake'),
('TI04', N'Bồn tắm massage',   'fa-bath'),
('TI05', N'Minibar',           'fa-wine-glass');

INSERT INTO Phong_TienIch (MaPhong, MaTienIch) VALUES
('P101', 'TI01'), ('P101', 'TI03'),
('P102', 'TI01'), ('P102', 'TI02'), ('P102', 'TI03'),
('P103', 'TI01'), ('P103', 'TI03'),
('P201', 'TI01'), ('P201', 'TI02'), ('P201', 'TI03'), ('P201', 'TI05'),
('P202', 'TI01'), ('P202', 'TI02'), ('P202', 'TI03'), ('P202', 'TI04'), ('P202', 'TI05'),
('P203', 'TI01'), ('P203', 'TI02'), ('P203', 'TI03'), ('P203', 'TI05'),
('P301', 'TI01'), ('P301', 'TI02'), ('P301', 'TI03'),
('P302', 'TI01'), ('P302', 'TI02'), ('P302', 'TI03'),
('P303', 'TI01'), ('P303', 'TI02'), ('P303', 'TI03');

INSERT INTO HinhAnh (MaHinhAnh, MaPhong, DuongDanAnh) VALUES
('HA01', 'P101', '/assets/img/rooms/p101_front.jpg'),
('HA02', 'P101', '/assets/img/rooms/p101_bed.jpg'),
('HA03', 'P202', '/assets/img/rooms/p202_suite.jpg'),
('HA04', 'P301', '/assets/img/rooms/p301_family.jpg'),
('HA05', 'P301', '/assets/img/rooms/p301_bathroom.jpg');

-- LaTraiNghiem = 1: giới thiệu ở mục "Trải nghiệm" trang chủ (chỉ khi dịch vụ đang Sẵn sàng)
INSERT INTO DichVu (MaDichVu, TenDichVu, GiaDichVu, DonViTinh, TrangThai, LaTraiNghiem, MoTa) VALUES
('DV01', N'Dọn phòng thêm',     100000, N'Lần',  N'Sẵn sàng', 0, NULL),
('DV02', N'Giặt ủi quần áo',     50000, N'Kg',   N'Sẵn sàng', 0, NULL),
('DV03', N'Thuê xe máy',        150000, N'Ngày', N'Sẵn sàng', 0, NULL),
('DV04', N'Spa & Massage',      500000, N'Lượt', N'Sẵn sàng', 1, N'Liệu trình massage và chăm sóc cơ thể giúp cân bằng lại sau những ngày dài di chuyển.'),
('DV05', N'Ăn sáng Buffet',     200000, N'Vé',   N'Sẵn sàng', 1, N'Bữa sáng tự chọn với nguyên liệu địa phương, phục vụ mỗi sáng trong không gian yên tĩnh.'),
('DV06', N'Đưa đón sân bay',    300000, N'Lượt', N'Đã hết',   1, N'Xe đón tận sân bay và đưa quý khách về khách sạn, sắp xếp trước theo lịch trình.');

-- Đồ minibar (buồng phòng ghi khi kiểm tra phòng)
INSERT INTO DichVu (MaDichVu, TenDichVu, GiaDichVu, DonViTinh, TrangThai, LaTraiNghiem, LaMinibar, MoTa) VALUES
('DV07', N'Minibar - Nước khoáng', 40000, N'Chai', N'Sẵn sàng', 0, 1, NULL),
('DV08', N'Minibar - Bia',         70000, N'Lon',  N'Sẵn sàng', 0, 1, NULL),
('DV09', N'Minibar - Snack',       60000, N'Gói',  N'Sẵn sàng', 0, 1, NULL);

-- Quy tắc giá: cuối tuần luôn áp dụng; dịp lễ sắp tới; ưu đãi Suite mùa thấp điểm
INSERT INTO QuyTacGia (MaQuyTac, TenQuyTac, MaLoai, TuNgay, DenNgay, CacThu, PhanTramDieuChinh, UuTien, KichHoat) VALUES
('QT001', N'Phụ thu cuối tuần',      NULL,   NULL,                  NULL,                  '5,6', 20,  1, 1),
('QT002', N'Lễ hội cuối năm',        NULL,   CAST(@H + 25 AS DATE), CAST(@H + 28 AS DATE), NULL,  50,  5, 1),
('QT003', N'Ưu đãi Suite thấp điểm', 'LP04', CAST(@H + 40 AS DATE), CAST(@H + 70 AS DATE), NULL,  -15, 2, 1);

-- ---------- 4. KHUYẾN MÃI (đã hết hạn / đang áp dụng / sắp diễn ra) ----------
INSERT INTO KhuyenMai (MaKhuyenMai, TenKhuyenMai, LoaiGiam, GiaTriGiam, NgayBatDau, NgayKetThuc) VALUES
('KM01', N'Chào hè sôi động',     N'Phần trăm', 10,     CAST(@H - 120 AS DATE), CAST(@H - 60 AS DATE)),
('KM02', N'Mừng lễ lớn',          N'Số tiền',   200000, CAST(@H - 50 AS DATE),  CAST(@H - 45 AS DATE)),
('KM03', N'Thành viên mới',       N'Phần trăm', 5,      CAST(@H - 200 AS DATE), CAST(@H + 200 AS DATE)),
('KM04', N'Cuối tuần thư giãn',   N'Số tiền',   150000, CAST(@H - 10 AS DATE),  CAST(@H + 30 AS DATE)),
('KM05', N'Siêu sale cuối năm',   N'Phần trăm', 30,     CAST(@H + 30 AS DATE),  CAST(@H + 35 AS DATE));

-- ---------- 5. PHIẾU ĐẶT PHÒNG ----------
-- DP01-DP06: đã trả phòng (có hoá đơn đã thanh toán)
-- PDP0000001 + DP07: đang lưu trú | PDP0000002-04: sắp đến | PDP0000005: khách đã huỷ
INSERT INTO PhieuDatPhong (MaDatPhong, MaKhachHang, MaNhanVien, NgayDat, NgayCheckInDuKien, NgayCheckOutDuKien,
                           NgayCheckInThucTe, NgayCheckOutThucTe, SoLuongKhach, TrangThai, TienCoc, GhiChu, MaKhuyenMai) VALUES
('DP01', 'KH01', 'NV02', @H - 35, @H - 30, @H - 28, DATEADD(HOUR, 14, @H - 30), DATEADD(MINUTE, 690, @H - 28), 2, N'Đã trả phòng', 0,       N'Khách check-in đúng giờ', 'KM03'),
('DP02', 'KH05', 'NV02', @H - 25, @H - 20, @H - 18, DATEADD(HOUR, 14, @H - 20), DATEADD(HOUR, 11, @H - 18),    4, N'Đã trả phòng', 0,       N'Gia đình đi nghỉ mát', NULL),
('DP03', 'KH02', 'NV03', @H - 15, @H - 12, @H - 10, DATEADD(HOUR, 15, @H - 12), DATEADD(HOUR, 10, @H - 10),    1, N'Đã trả phòng', 0,       N'Cần phòng yên tĩnh', NULL),
('DP04', 'KH04', 'NV02', @H - 9,  @H - 7,  @H - 5,  DATEADD(HOUR, 14, @H - 7),  DATEADD(HOUR, 11, @H - 5),     2, N'Đã trả phòng', 0,       N'Đặt online qua Web', 'KM04'),
('DP05', 'KH06', 'NV03', @H - 4,  @H - 4,  @H - 3,  DATEADD(HOUR, 16, @H - 4),  DATEADD(MINUTE, 810, @H - 3),  1, N'Đã trả phòng', 600000,  N'Khách vãng lai, trả phòng muộn 13:30', NULL),
('DP06', 'KH03', 'NV02', @H - 6,  @H - 2,  @H,      DATEADD(HOUR, 15, @H - 2),  DATEADD(MINUTE, -90, @Nay),    2, N'Đã trả phòng', 0,       N'Trả phòng sáng nay', NULL),
('PDP0000001', 'KH01', 'NV02', @H - 5, @H - 1, @H + 2, DATEADD(MINUTE, 870, @H - 1), NULL,                     2, N'Đã nhận phòng', 0,      N'Đặt online qua Web', 'KM03'),
('DP07', 'KH07', 'NV02', DATEADD(MINUTE, -60, @Nay), @H, @H + 1, DATEADD(MINUTE, -60, @Nay), NULL,              2, N'Đã nhận phòng', 750000, N'Khách vãng lai', NULL),
('PDP0000002', 'KH02', NULL, @H - 1, @H + 1,  @H + 3,  NULL, NULL, 1, N'Chờ duyệt', 0, N'Đặt online qua Web', 'KM03'),
('PDP0000003', 'KH04', NULL, @H - 2, @H + 5,  @H + 7,  NULL, NULL, 2, N'Đã duyệt',  0, N'Đặt online qua Web', NULL),
('PDP0000004', 'KH05', NULL, @H - 3, @H,      @H + 2,  NULL, NULL, 2, N'Đã duyệt',  0, N'Khách đến nhận phòng hôm nay', NULL),
('PDP0000005', 'KH01', NULL, @H - 4, @H + 10, @H + 12, NULL, NULL, 3, N'Đã hủy',    0,
    N'Đặt online qua Web | [Huỷ ' + CONVERT(VARCHAR(10), @H - 2, 103) + N' 09:15 - Khách hàng] Thay đổi kế hoạch', NULL);

INSERT INTO ChiTietDatPhong (MaCTDP, MaDatPhong, MaPhong, HinhThucThue, GiaThueThucTe) VALUES
('CT01', 'DP01', 'P101', N'Theo ngày',  500000),
('CT02', 'DP02', 'P301', N'Theo ngày', 1500000),
('CT03', 'DP03', 'P102', N'Theo ngày',  800000),
('CT04', 'DP04', 'P202', N'Theo ngày', 2500000),
('CT05', 'DP05', 'P201', N'Theo ngày', 1200000),
('CT06', 'DP06', 'P302', N'Theo ngày',  800000),
('CT07', 'PDP0000001', 'P201', N'Theo ngày', 1200000),
('CT08', 'DP07', 'P303', N'Theo ngày', 1500000),
('CT09', 'PDP0000002', 'P101', N'Theo ngày',  500000),
('CT10', 'PDP0000003', 'P202', N'Theo ngày', 2500000),
('CT11', 'PDP0000004', 'P102', N'Theo ngày',  800000),
('CT12', 'PDP0000005', 'P301', N'Theo ngày', 1500000);

INSERT INTO SuDungDichVu (MaSDDV, MaDatPhong, MaDichVu, SoLuong, NgaySuDung, GiaThucTe) VALUES
('SD01', 'DP01', 'DV05', 2, DATEADD(HOUR, 7,  @H - 29), 200000),
('SD02', 'DP02', 'DV04', 2, DATEADD(HOUR, 19, @H - 20), 500000),
('SD03', 'DP02', 'DV01', 1, DATEADD(HOUR, 9,  @H - 19), 100000),
('SD04', 'DP04', 'DV05', 4, DATEADD(HOUR, 7,  @H - 6),  200000),
('SD05', 'DP06', 'DV03', 1, DATEADD(HOUR, 9,  @H - 1),  150000),
('SD06', 'PDP0000001', 'DV02', 2, DATEADD(HOUR, 20, @H - 1), 50000),
('SD07', 'PDP0000001', 'DV05', 2, DATEADD(HOUR, 7,  @H),     200000);

-- ---------- 6. HOÁ ĐƠN, THANH TOÁN, LỊCH SỬ GIAO DỊCH (chỉ phiếu đã trả phòng) ----------
-- Tiền phòng = giá × số đêm thực tế; KM "Phần trăm" giảm trên tiền phòng; TienPhat = phụ thu trả muộn
INSERT INTO HoaDon (MaHoaDon, MaDatPhong, MaNhanVienLap, NgayLapHoaDon, TongTienPhong, SoDem, TongTienDichVu, MaKhuyenMai, GiamGiaKhuyenMai, TienPhat, TienCoc, TrangThai) VALUES
('HD01', 'DP01', 'NV02', DATEADD(MINUTE, 685, @H - 28), 1000000, 2,  400000, 'KM03',  50000,      0,      0, N'Đã thanh toán'),
('HD02', 'DP02', 'NV02', DATEADD(MINUTE, 655, @H - 18), 3000000, 2, 1100000, NULL,        0,      0,      0, N'Đã thanh toán'),
('HD03', 'DP03', 'NV03', DATEADD(MINUTE, 595, @H - 10), 1600000, 2,       0, NULL,        0,      0,      0, N'Đã thanh toán'),
('HD04', 'DP04', 'NV02', DATEADD(MINUTE, 655, @H - 5),  5000000, 2,  800000, 'KM04', 150000,      0,      0, N'Đã thanh toán'),
('HD05', 'DP05', 'NV03', DATEADD(MINUTE, 805, @H - 3),  1200000, 1,       0, NULL,        0, 360000, 600000, N'Đã thanh toán'),
('HD06', 'DP06', 'NV02', DATEADD(MINUTE, -95, @Nay),    1600000, 2,  150000, NULL,        0,      0,      0, N'Đã thanh toán');

-- Số tiền thanh toán = phần còn lại sau khi trừ cọc (lấy từ cột tính toán để luôn khớp)
INSERT INTO ThanhToan (MaThanhToan, MaHoaDon, PhuongThuc, NgayThanhToan, SoTienDaTra, GhiChu, LoaiGiaoDich, MaNhanVien)
SELECT v.MaThanhToan, h.MaHoaDon, v.PhuongThuc, DATEADD(MINUTE, 5, h.NgayLapHoaDon), h.SoTienConLai, v.GhiChu, N'Thu', h.MaNhanVienLap
FROM HoaDon h
JOIN (VALUES
    ('TT01', 'HD01', N'Tiền mặt',            N'Khách thanh toán trực tiếp'),
    ('TT02', 'HD02', N'Chuyển khoản',        N'Chuyển khoản Vietcombank'),
    ('TT03', 'HD03', N'Thẻ tín dụng', NULL),
    ('TT04', 'HD04', N'Chuyển khoản',        NULL),
    ('TT05', 'HD05', N'Tiền mặt',            N'Đã trừ tiền cọc 600.000đ'),
    ('TT06', 'HD06', N'Thẻ tín dụng', NULL)
) v (MaThanhToan, MaHoaDon, PhuongThuc, GhiChu) ON v.MaHoaDon = h.MaHoaDon;

-- Kế toán hoàn 100.000đ cho HD03 vì khách phàn nàn cách âm (đánh giá DG03)
INSERT INTO ThanhToan (MaThanhToan, MaHoaDon, PhuongThuc, NgayThanhToan, SoTienDaTra, GhiChu, LoaiGiaoDich, MaNhanVien) VALUES
('TT07', 'HD03', N'Chuyển khoản', DATEADD(HOUR, 10, @H - 8), 100000, N'Hoàn tiền: Phòng cách âm kém, khách phàn nàn', N'Hoàn', 'NV08');

INSERT INTO LichSuGiaoDich (MaGiaoDich, MaThanhToan, CongThanhToan, SoTien, TrangThaiGiaoDich, ThoiGianGiaoDich, NoiDungChuyenKhoan)
SELECT 'GD_' + RIGHT('000000' + SUBSTRING(t.MaThanhToan, 3, 10), 6), t.MaThanhToan, t.PhuongThuc, t.SoTienDaTra,
       N'Thành công', t.NgayThanhToan,
       CASE WHEN t.LoaiGiaoDich = N'Hoàn' THEN N'Hoàn tiền hóa đơn ' ELSE N'Khách thanh toán hóa đơn ' END + t.MaHoaDon
FROM ThanhToan t;

-- ---------- 6b. CÔNG VIỆC BUỒNG PHÒNG ----------
-- Lịch sử: mỗi lần trả phòng tạo 1 việc dọn, đã hoàn thành. P302 (trả phòng sáng nay) đã giao cho Ngô Thị Mai, chưa dọn.
INSERT INTO PhanCongDonPhong (MaPhanCong, MaPhong, MaNhanVien, MaDatPhong, LoaiCongViec, TrangThai, ThoiGianTao, ThoiGianGiao, ThoiGianBatDau, ThoiGianHoanThanh, GhiChu) VALUES
('PC0001', 'P101', 'NV06', 'DP01', N'Dọn sau trả phòng', N'Hoàn thành', DATEADD(MINUTE, 700, @H - 28), DATEADD(MINUTE, 710, @H - 28), DATEADD(MINUTE, 720, @H - 28), DATEADD(MINUTE, 770, @H - 28), NULL),
('PC0002', 'P301', 'NV07', 'DP02', N'Dọn sau trả phòng', N'Hoàn thành', DATEADD(MINUTE, 665, @H - 18), DATEADD(MINUTE, 680, @H - 18), DATEADD(MINUTE, 690, @H - 18), DATEADD(MINUTE, 760, @H - 18), N'Thay thêm ga giường phụ'),
('PC0003', 'P102', 'NV06', 'DP03', N'Dọn sau trả phòng', N'Hoàn thành', DATEADD(MINUTE, 605, @H - 10), DATEADD(MINUTE, 615, @H - 10), DATEADD(MINUTE, 620, @H - 10), DATEADD(MINUTE, 665, @H - 10), NULL),
('PC0004', 'P202', 'NV07', 'DP04', N'Dọn sau trả phòng', N'Hoàn thành', DATEADD(MINUTE, 665, @H - 5),  DATEADD(MINUTE, 670, @H - 5),  DATEADD(MINUTE, 680, @H - 5),  DATEADD(MINUTE, 790, @H - 5),  N'Phòng Suite, kiểm tra kỹ minibar'),
('PC0005', 'P201', 'NV06', 'DP05', N'Dọn sau trả phòng', N'Hoàn thành', DATEADD(MINUTE, 815, @H - 3),  DATEADD(MINUTE, 820, @H - 3),  DATEADD(MINUTE, 830, @H - 3),  DATEADD(MINUTE, 880, @H - 3),  NULL),
('PC0006', 'P302', 'NV06', 'DP06', N'Dọn sau trả phòng', N'Đã giao',    DATEADD(MINUTE, -85, @Nay),    DATEADD(MINUTE, -60, @Nay),    NULL, NULL, NULL);

-- ---------- 7. ĐÁNH GIÁ (chỉ phòng khách đã ở và đã trả phòng) ----------
INSERT INTO DanhGia (MaDanhGia, MaKhachHang, MaPhong, MaDatPhong, SoSao, NoiDung, NgayDanhGia, AnDanh) VALUES
('DG01', 'KH01', 'P101', 'DP01', 5, N'Phòng sạch sẽ, nhân viên nhiệt tình!',       @H - 27, 0),
('DG02', 'KH05', 'P301', 'DP02', 4, N'Phòng rộng nhưng wifi đôi lúc hơi chậm.',    @H - 17, 0),
('DG03', 'KH02', 'P102', 'DP03', 3, N'Bình thường, cách âm chưa tốt lắm.',         @H - 9,  1),
('DG04', 'KH04', 'P202', 'DP04', 5, N'Tuyệt vời, sẽ quay lại lần sau.',            @H - 4,  0);

-- ---------- 8. ĐIỂM TÍCH LUỸ = 1 điểm / 10.000đ giá trị các hoá đơn đã thanh toán ----------
UPDATE k SET DiemTichLuy = ISNULL((
    SELECT SUM(CAST(FLOOR(h.TongTienCanThanhToan / 10000) AS INT))
    FROM HoaDon h JOIN PhieuDatPhong p ON p.MaDatPhong = h.MaDatPhong
    WHERE p.MaKhachHang = k.MaKhachHang AND h.TrangThai = N'Đã thanh toán'), 0)
  - ISNULL((
    SELECT SUM(CAST(FLOOR(t.SoTienDaTra / 10000) AS INT))
    FROM ThanhToan t JOIN HoaDon h ON h.MaHoaDon = t.MaHoaDon JOIN PhieuDatPhong p ON p.MaDatPhong = h.MaDatPhong
    WHERE p.MaKhachHang = k.MaKhachHang AND t.LoaiGiaoDich = N'Hoàn'), 0)
FROM KhachHang k;

-- Lịch sử điểm: mỗi hoá đơn đã thanh toán một dòng tích điểm, mỗi lần hoàn tiền một dòng trừ điểm
INSERT INTO LichSuDiem (MaLichSu, MaKhachHang, ThoiGian, SoDiem, Loai, NoiDung, MaHoaDon, MaNhanVien)
SELECT 'LSD' + RIGHT('000000' + CAST(ROW_NUMBER() OVER (ORDER BY x.ThoiGian, x.MaHoaDon) AS VARCHAR(6)), 6), x.*
FROM (
    SELECT p.MaKhachHang, h.NgayLapHoaDon AS ThoiGian, CAST(FLOOR(h.TongTienCanThanhToan / 10000) AS INT) AS SoDiem,
           N'Tích điểm' AS Loai, N'Thanh toán hoá đơn ' + h.MaHoaDon AS NoiDung, h.MaHoaDon, h.MaNhanVienLap AS MaNhanVien
    FROM HoaDon h JOIN PhieuDatPhong p ON p.MaDatPhong = h.MaDatPhong
    WHERE h.TrangThai = N'Đã thanh toán' AND FLOOR(h.TongTienCanThanhToan / 10000) > 0
    UNION ALL
    SELECT p.MaKhachHang, t.NgayThanhToan, -CAST(FLOOR(t.SoTienDaTra / 10000) AS INT),
           N'Hoàn tiền', N'Hoàn tiền hoá đơn ' + h.MaHoaDon, h.MaHoaDon, t.MaNhanVien
    FROM ThanhToan t JOIN HoaDon h ON h.MaHoaDon = t.MaHoaDon JOIN PhieuDatPhong p ON p.MaDatPhong = h.MaDatPhong
    WHERE t.LoaiGiaoDich = N'Hoàn' AND FLOOR(t.SoTienDaTra / 10000) > 0
) x;

-- Khách VIP mẫu (lễ tân thấy sở thích / ghi chú khi khách đến)
UPDATE KhachHang SET LaVIP = 1, SoThich = N'Gối mềm, tầng cao, không dùng hoa có mùi', GhiChuNoiBo = N'Khách quen, thường đặt Suite dịp kỷ niệm.'
WHERE MaKhachHang = 'KH04';

-- Kênh đặt phòng và sổ tiền cọc của dữ liệu mẫu (cọc khách vãng lai thu bằng tiền mặt lúc nhận phòng)
UPDATE PhieuDatPhong SET NguonDat = CASE WHEN MaDatPhong LIKE 'PDP%' THEN N'Online' ELSE N'Vãng lai' END;
INSERT INTO GiaoDichCoc (MaDatPhong, LoaiGiaoDich, SoTien, PhuongThuc, ThoiGian, MaNhanVien, GhiChu)
SELECT MaDatPhong, N'Thu', TienCoc, N'Tiền mặt', ISNULL(NgayCheckInThucTe, NgayDat), MaNhanVien, N'Cọc 50% khách vãng lai'
FROM PhieuDatPhong WHERE TienCoc > 0;
GO

/* =====================================================================
   PHẦN 3: TÓM TẮT SAU KHI CHẠY
   ===================================================================== */
SELECT N'Phòng' AS Bang, COUNT(*) AS SoDong FROM Phong
UNION ALL SELECT N'Phiếu đặt phòng', COUNT(*) FROM PhieuDatPhong
UNION ALL SELECT N'Hoá đơn', COUNT(*) FROM HoaDon
UNION ALL SELECT N'Khách hàng', COUNT(*) FROM KhachHang
UNION ALL SELECT N'Tài khoản', COUNT(*) FROM TaiKhoan
UNION ALL SELECT N'Công việc buồng phòng', COUNT(*) FROM PhanCongDonPhong
UNION ALL SELECT N'Quy tắc giá', COUNT(*) FROM QuyTacGia;

-- Phòng và phiếu đặt đang hiệu lực (nếu có)
SELECT p.MaPhong, p.TrangThai, a.MaDatPhong, a.TrangThai AS TrangThaiPhieu,
       CONVERT(VARCHAR(10), a.NgayCheckInDuKien, 103) AS NhanPhong, CONVERT(VARCHAR(10), a.NgayCheckOutDuKien, 103) AS TraPhong
FROM Phong p
OUTER APPLY (
    SELECT pd.MaDatPhong, pd.TrangThai, pd.NgayCheckInDuKien, pd.NgayCheckOutDuKien
    FROM ChiTietDatPhong c JOIN PhieuDatPhong pd ON pd.MaDatPhong = c.MaDatPhong
    WHERE c.MaPhong = p.MaPhong AND pd.TrangThai IN (N'Chờ duyệt', N'Đã duyệt', N'Đã nhận phòng')
) a
ORDER BY p.MaPhong, a.NgayCheckInDuKien;
GO
