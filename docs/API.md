# QLKSan.API — Danh sách endpoint

Địa chỉ khi phát triển: `https://localhost:7001`. Ở môi trường Development, Swagger có tại `/swagger`.

## Quy ước

**Xác thực.** Gửi header `Authorization: Bearer <token>`. Token lấy từ `POST /api/auth/login` (hoặc từ `register`).

**JSON.** Thuộc tính viết PascalCase. Enum truyền dạng chuỗi, ví dụ `"Approved"`.

**Mã lỗi.** Lỗi trả về theo ProblemDetails (`title`, `detail`, `errors`):

| Mã | Khi nào |
|---|---|
| 400 | Dữ liệu sai hoặc vi phạm chính sách |
| 401 | Chưa đăng nhập |
| 403 | Sai vai trò hoặc không phải dữ liệu của mình |
| 404 | Không tìm thấy |
| 409 | Trạng thái không cho phép thao tác (ví dụ duyệt phiếu đã trả phòng, hết phòng) |
| 429 | Tài khoản tạm khoá 15 phút sau 5 lần đăng nhập sai liên tiếp (quản lý đặt lại mật khẩu để mở khoá) |

**Đặt tên.**
- Thao tác nghiệp vụ dùng `POST /{resource}/{id}/{hanh-dong}` (kebab-case).
- Đổi trạng thái hoặc cờ dùng `PATCH /{resource}/{id}` với body tường minh, ví dụ `{ "TrangThai": "Available" }`.

**Nhóm vai trò** (theo `Common/AppRoles.cs`):

| Nhóm | Gồm |
|---|---|
| FrontDesk | Admin, Lễ tân |
| Cashier | Admin, Lễ tân, Kế toán |
| Finance | Admin, Kế toán |
| AllStaff | Mọi nhân viên |
| Customer | Khách hàng |

Cột **Quyền** trong các bảng dưới: "Công khai" = không cần đăng nhập; "Đăng nhập" = mọi tài khoản đã đăng nhập.

## Công khai / hệ thống

| Phương thức | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/health` | Công khai | Kiểm tra API và kết nối CSDL |
| GET | `/api/hotel` | Công khai | Liên hệ, giờ nhận/trả phòng, phụ thu trả muộn, chính sách huỷ, chương trình thành viên |

## Xác thực — `/api/auth`

| Phương thức | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| POST | `/login` | Công khai | Đăng nhập, trả JWT |
| POST | `/register` | Công khai | Khách đăng ký. Nếu CCCD đã có hồ sơ vãng lai và SĐT khớp thì liên kết tài khoản với hồ sơ cũ |
| POST | `/change-password` | Đăng nhập | Đổi mật khẩu (`MatKhauCu`, `MatKhauMoi`, `XacNhanMatKhauMoi`) |
| GET | `/me` | Đăng nhập | Thông tin người đang đăng nhập |

## Hạng phòng — `/api/room-types`

| Phương thức | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/` | Công khai | Danh sách hạng phòng kèm điểm đánh giá |
| GET | `/available?checkIn&checkOut&soNguoi` | Công khai | Hạng còn trống trong `[checkIn, checkOut)`, kèm báo giá từng đêm |
| GET | `/{maLoai}` | Công khai | Chi tiết: bộ ảnh, tiện nghi, đánh giá gần đây |
| GET | `/{maLoai}/reviews` | Công khai | Đánh giá của hạng (ẩn danh tính khách) |
| POST | `/` | Admin | Thêm hạng phòng |
| PUT | `/{maLoai}` | Admin | Sửa hạng phòng (gồm diện tích, giường, hướng nhìn, mô tả chi tiết) |
| DELETE | `/{maLoai}` | Admin | Xoá (409 nếu còn phòng thuộc hạng) |
| POST | `/{maLoai}/images` | Admin | Tải ảnh lên (multipart, trường `file`; JPG/PNG/WebP ≤ 5 MB) |
| DELETE | `/{maLoai}/images/{maHinhAnh}` | Admin | Xoá ảnh |

## Phòng — `/api/rooms`

| Phương thức | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/?trangThai` | AllStaff | Danh sách phòng |
| GET | `/available?checkIn&checkOut&soNguoi` | Công khai | Phòng trống theo khoảng ngày |
| GET | `/{maPhong}/quote?checkIn&checkOut` | Công khai | Báo giá từng đêm (giá mùa / cuối tuần / lễ) |
| GET | `/{maPhong}` | AllStaff | Chi tiết phòng |
| GET | `/{maPhong}/current-stay` | Cashier | Phiếu đang lưu trú (cho màn hình trả phòng) |
| POST / PUT / DELETE | `/`, `/{maPhong}` | Admin | Quản lý phòng |
| PATCH | `/{maPhong}` | FrontDesk | Đổi trạng thái thủ công `{ "TrangThai": ... }` |
| PUT | `/{maPhong}/amenities` | Admin | Đặt lại tiện nghi của phòng `{ "MaTienIchs": [...], "ApDungCaHang": false }` (true = áp cho mọi phòng cùng hạng) |

## Đặt phòng — `/api/bookings`

| Phương thức | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/?search` | Cashier | Lịch sử đặt phòng |
| GET | `/online` | Cashier | Phiếu online chưa nhận phòng |
| GET | `/active` | Cashier | Phiếu đang lưu trú |
| GET | `/timeline?tuNgay&denNgay` | Cashier | Lịch phòng |
| GET | `/{maDatPhong}` | Đăng nhập | Chi tiết phiếu (khách chỉ xem phiếu của mình) |
| POST | `/` | Customer | Đặt online. Body có `MaLoai` (tự xếp phòng) **hoặc** `MaPhong`, cùng `CheckIn`, `CheckOut`, `SoNguoi`, `MaKhuyenMai`, `YeuCauDacBiet`, `GioDenDuKien`. Còn phòng thì phiếu ở trạng thái Đã duyệt; hết phòng trả 409 |
| POST | `/walk-in` | FrontDesk | Nhận khách vãng lai và check-in ngay |
| POST | `/{id}/approve` | FrontDesk | Duyệt phiếu chờ duyệt |
| POST | `/{id}/change-room` | FrontDesk | Đổi phòng đã xếp `{ "MaPhong": ... }` (chỉ phiếu chưa nhận phòng) |
| POST | `/{id}/cancel` | FrontDesk, Customer | Huỷ/từ chối `{ "LyDo": ... }`. Khách chỉ huỷ được trước ngày nhận phòng |
| POST | `/{id}/check-in` | FrontDesk | Nhận phòng (đúng ngày) |
| POST | `/{id}/services` | FrontDesk | Gọi dịch vụ cho khách đang ở |

## Quầy lễ tân — `/api/front-desk`

| Phương thức | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/today` | FrontDesk | Màn hình đầu ca: khách đến / khách đi (kể cả quá hạn trả) / đang lưu trú hôm nay, phiếu sắp đến, phòng trống tối nay, việc buồng phòng chưa xong. Mỗi phiếu kèm VIP, sở thích, hạn chế, giờ đến, yêu cầu riêng, trạng thái phòng |

## Hoá đơn — `/api/invoices`

| Phương thức | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/?tuNgay&denNgay&trangThai&search` | Finance | Danh sách hoá đơn |
| GET | `/{maHoaDon}` | Cashier, Customer | Chi tiết, giao dịch, số tiền còn hoàn được (khách chỉ xem hoá đơn của mình) |
| POST | `/` | Cashier | Lập hoặc tính lại hoá đơn `{ "MaDatPhong", "MaKhuyenMai" }`. Gồm số đêm thực tế, giá từng đêm, phụ thu trả muộn, khuyến mãi, ưu đãi hạng thành viên, tiền cọc |
| POST | `/{id}/payments` | Cashier | Thanh toán và trả phòng `{ "PhuongThuc", "DiemSuDung" }`. Điểm đổi được ghi thành khoản "Điểm thưởng" |
| POST | `/{id}/refunds` | Finance | Hoàn tiền `{ "SoTien", "PhuongThuc", "LyDo" }`. Không hoàn phần đã trả bằng điểm |

## Khách hàng — `/api/customers`

| Phương thức | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/?search` | Cashier | Danh sách khách kèm hạng thành viên |
| GET | `/{maKhachHang}` | Cashier | Hồ sơ (VIP, sở thích, ghi chú nội bộ, hạn chế) và lịch sử đặt phòng kèm hoá đơn |
| PUT | `/{maKhachHang}` | FrontDesk | Cập nhật hồ sơ: thông tin cá nhân, liên hệ, `LaVIP`, `SoThich`, `GhiChuNoiBo`, `HanChe` + `LyDoHanChe` (khách bị hạn chế không đặt online được) |
| GET | `/{maKhachHang}/points` | Cashier | Lịch sử điểm (tích / đổi / hoàn tiền / điều chỉnh); tổng các dòng = số điểm hiện có |
| POST | `/{maKhachHang}/point-adjustments` | Admin | Cộng / trừ điểm thủ công `{ "SoDiem": ±n, "LyDo" }` |
| GET | `/by-cccd/{cccd}` | Cashier | Tìm theo CCCD/Passport |
| GET | `/me` | Customer | Hồ sơ của tôi (hạng, quyền lợi, điểm, tiến độ lên hạng; không gồm ghi chú nội bộ) |
| PUT | `/me/contact` | Customer | Sửa SĐT, email, địa chỉ, quốc gia, sở thích lưu trú |
| GET | `/me/points` | Customer | Lịch sử điểm của tôi |
| GET | `/me/bookings` | Customer | Kỳ nghỉ của tôi |
| GET | `/me/reviews` | Customer | Đánh giá tôi đã gửi |

## Đánh giá — `/api/reviews`

| Phương thức | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/?soSao&chuaPhanHoi&an` | Admin | Đánh giá (lọc theo số sao / chưa phản hồi / đang ẩn); quản trị thấy tên thật cả khi khách gửi ẩn danh |
| PUT | `/{id}/reply` | Admin | Phản hồi công khai `{ "NoiDung" }` (ghi đè phản hồi cũ) |
| DELETE | `/{id}/reply` | Admin | Gỡ phản hồi |
| PATCH | `/{id}` | Admin | Ẩn / hiện trên trang khách `{ "An": true \| false }`; đánh giá ẩn vẫn tính vào điểm trung bình |
| GET | `/featured?soLuong` | Công khai | Đánh giá từ 4 sao gần đây (ẩn danh tính) |
| POST | `/` | Customer | Đánh giá kỳ nghỉ đã trả phòng `{ "MaDatPhong", "SoSao", "NoiDung", "AnDanh" }`. Mỗi kỳ nghỉ chỉ đánh giá một lần (lần thứ hai trả 409) |

## Dịch vụ, khuyến mãi, giá

| Phương thức | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/services` | Công khai | Danh sách dịch vụ (`LaTraiNghiem`, `MoTa` cho mục Trải nghiệm) |
| GET | `/api/services/{id}` | AllStaff | Chi tiết |
| POST / PUT / DELETE | `/api/services[/{id}]` | Admin | Quản lý dịch vụ |
| PATCH | `/api/services/{id}` | Admin | `{ "TrangThai": "Available" \| "Unavailable" }` |
| GET | `/api/promotions` | Admin | Danh sách khuyến mãi kèm điều kiện và số lượt đã dùng |
| GET | `/api/promotions/{ma}` | Đăng nhập | Kiểm tra mã: 200 đang áp dụng, 400 hết hạn / đã tắt / hết lượt, 404 không tồn tại |
| POST | `/api/promotions` | Admin | Thêm khuyến mãi; điều kiện tuỳ chọn `SoLuotToiDa`, `SoDemToiThieu`, `MaLoai` |
| PUT | `/api/promotions/{ma}` | Admin | Sửa khuyến mãi |
| PATCH | `/api/promotions/{ma}` | Admin | Bật / tắt `{ "KichHoat": true \| false }` |
| DELETE | `/api/promotions/{ma}` | Admin | Xoá mã chưa dùng (đã dùng → 409, nên tắt) |
| GET / POST | `/api/pricing-rules` | Admin | Quy tắc giá theo mùa, cuối tuần, lễ |
| PUT | `/api/pricing-rules/{id}` | Admin | Sửa quy tắc (giữ trạng thái bật/tắt; hoá đơn đã lập không đổi) |
| PATCH | `/api/pricing-rules/{id}` | Admin | `{ "KichHoat": true \| false }` |
| GET / POST | `/api/amenities` | Admin | Danh mục tiện nghi (kèm các phòng đang có) / thêm tiện nghi `{ "TenTienIch", "Icon": "fa-wifi" }` |
| PUT / DELETE | `/api/amenities/{id}` | Admin | Sửa / xoá tiện nghi (xoá thì gỡ khỏi mọi phòng) |
| DELETE | `/api/pricing-rules/{id}` | Admin | Xoá quy tắc |

## Vận hành

| Phương thức | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/housekeeping` | Admin, Lễ tân, Buồng phòng | Tổng quan buồng phòng |
| GET | `/api/housekeeping/tasks`, `/tasks/mine` | Như trên / Buồng phòng | Công việc dọn phòng |
| GET | `/api/housekeeping/staff` | FrontDesk | Nhân viên có thể giao việc |
| POST | `/api/housekeeping/tasks/{id}/assign` | FrontDesk | Giao việc |
| POST | `/api/housekeeping/tasks/{id}/start` · `/complete` · `/issue` | Buồng phòng | Bắt đầu / dọn xong / báo sự cố |
| POST | `/api/housekeeping/rooms/{maPhong}/clean` | FrontDesk | Xác nhận phòng đã dọn |
| GET | `/api/dashboard` | Finance | Bảng điều khiển: công suất / khách đến / khách đi hôm nay, doanh thu thuần – ADR – RevPAR 30 ngày so với kỳ trước, công suất đã đặt 14 ngày tới, việc cần xử lý, đánh giá mới |
| GET | `/api/reports/revenue`, `/revenue/export` | Finance | Báo cáo doanh thu (JSON / Excel) |
| GET | `/api/reports/occupancy` | Finance | Công suất phòng |
| GET | `/api/reports/guests`, `/guests/export` | Cashier | Sổ khách lưu trú (JSON / Excel) |
| GET / POST / PUT / DELETE | `/api/staff[/{id}]` | Admin | Quản lý nhân viên |
| POST | `/api/staff/{id}/reset-password` | Admin | Đặt lại mật khẩu (đồng thời mở khoá tài khoản đang bị khoá tạm) |
| GET | `/api/settings` | Admin | Cấu hình nghiệp vụ hiện hành (thông tin khách sạn, chính sách lưu trú, chương trình thành viên) và mục nào đã được tuỳ chỉnh |
| PUT | `/api/settings/hotel-info` · `/policy` · `/loyalty` | Admin | Lưu từng mục vào CSDL (bảng `CauHinh`); có hiệu lực ngay, không cần khởi động lại |
| GET | `/api/audit-logs?tuNgay&denNgay&doiTuong&nguoiDung&tuKhoa&trang&soDong` | Admin | Nhật ký thao tác: mọi thao tác thay đổi dữ liệu thành công (người thực hiện, thời gian, đối tượng, dữ liệu gửi lên — không lưu mật khẩu) và các lần tạm khoá tài khoản |
