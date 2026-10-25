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
| POST | `/walk-in` | FrontDesk | Nhận khách vãng lai và check-in ngay; cọc tự tính theo chính sách, thu bằng `PhuongThucCoc` (ghi sổ cọc) |
| POST | `/front-desk` | FrontDesk | Lễ tân đặt phòng cho ngày tới: thông tin khách (nhận diện theo CCCD), `MaLoai` hoặc `MaPhong`, `CheckIn`, `CheckOut`, `SoNguoi`, `NguonDat` (Điện thoại / Email / Tại quầy / Đại lý), `GioDenDuKien`, `YeuCauDacBiet`, `MaKhuyenMai`, `TienCoc` + `PhuongThucCoc`. Phiếu ở trạng thái Đã duyệt |
| POST | `/{id}/deposits` | FrontDesk | Thu thêm cọc / đảm bảo `{ "SoTien", "PhuongThuc", "GhiChu" }` cho phiếu chưa trả phòng |
| POST | `/{id}/approve` | FrontDesk | Duyệt phiếu chờ duyệt |
| POST | `/{id}/change-room` | FrontDesk | Đổi phòng đã xếp `{ "MaPhong": ... }` (chỉ phiếu chưa nhận phòng) |
| POST | `/{id}/cancel` | FrontDesk, Customer | Huỷ/từ chối `{ "LyDo": ..., "HoanCoc": false }`. Khách chỉ huỷ được trước ngày nhận phòng; lễ tân chọn hoàn cọc (ghi dòng hoàn vào sổ cọc) hoặc giữ cọc |
| POST | `/{id}/check-in` | FrontDesk | Nhận phòng (đúng ngày). Body tuỳ chọn `{ "SoLuongKhach", "TienCoc", "PhuongThucCoc", "KhachDiCung" }`: số khách thực tế, cọc thu thêm, người đi cùng |
| POST | `/{id}/services` | FrontDesk | Gọi dịch vụ cho khách đang ở |
| POST | `/{id}/services/{maSDDV}/void` | FrontDesk | Huỷ dịch vụ gọi nhầm `{ "SoLuong", "LyDo" }` (bớt số lượng hoặc huỷ hẳn; hoá đơn chưa thanh toán) |
| GET | `/{id}/folio` | Cashier | Bảng kê lưu trú: giá từng đêm (kèm quy tắc giá), dịch vụ, phụ thu, khuyến mãi, ưu đãi hạng, VAT, sổ cọc, các khoản đã thu, còn phải thu. Chưa thanh toán: tính tới hiện tại, không lưu |
| GET | `/{id}/stay-dates/quote?ngayNhan&ngayTra` | FrontDesk | Báo giá đổi ngày: số đêm, tiền phòng cũ – mới theo giá từng đêm, phòng còn trống không |
| PUT | `/{id}/stay-dates` | FrontDesk | Gia hạn / rút ngắn (đang ở: chỉ `NgayTra`, không trước hôm nay, tối thiểu 1 đêm) hoặc đổi ngày (chưa nhận phòng: `NgayNhan`, `NgayTra`); 409 khi vướng phiếu khác |
| POST | `/{id}/move` | FrontDesk | Đổi phòng khi đang ở `{ "MaPhong", "LyDo", "GiuGiaCu": true }`: phòng mới phải sạch, trống đến ngày trả; phòng cũ chuyển Đang dọn + tạo việc buồng phòng; ghi lịch sử đổi phòng |
| PUT | `/{id}/companions` | FrontDesk | Khai báo người đi cùng `{ "KhachDiCung": [{ "HoTen", "CCCD_Passport", "QuocTich" }] }` (thay toàn bộ); số khách tự tăng theo, không vượt sức chứa. Sổ lưu trú có thêm dòng cho từng người |

## Quầy lễ tân — `/api/front-desk`

| Phương thức | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/today` | FrontDesk | Màn hình đầu ca: khách đến / khách đi (kể cả quá hạn trả) / đang lưu trú hôm nay, phiếu sắp đến, phòng trống tối nay, việc buồng phòng chưa xong, yêu cầu khách đang mở, bàn giao của ca gần nhất. Mỗi phiếu kèm VIP, sở thích, hạn chế, giờ đến, yêu cầu riêng, trạng thái phòng |
| GET | `/shift` | FrontDesk | Ca hiện tại của người đăng nhập (từ lần chốt ca gần nhất trong 24 giờ hoặc từ 0h): tiền thu / chi theo phương thức (thanh toán, hoàn tiền hoá đơn, thu / hoàn cọc; không tính điểm thưởng), tiền mặt hệ thống, chi tiết giao dịch |
| POST | `/shift/close` | FrontDesk | Chốt ca `{ "TienMatThucTe", "GhiChuBanGiao" }`: lưu tổng theo phương thức, tiền mặt hệ thống / thực tế, chênh lệch |
| GET | `/shifts?soNgay` | FrontDesk | Các lần chốt ca / bàn giao gần đây (mặc định 7 ngày) |

## Yêu cầu khách — `/api/guest-requests`

| Phương thức | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/?loc&maDatPhong` | FrontDesk | Sổ yêu cầu: `loc=mo` (mới / đang xử lý, giờ hẹn gần nhất trước), `xong` (hoàn thành / huỷ 7 ngày), trống: 30 ngày |
| POST | `/` | FrontDesk | Ghi yêu cầu `{ "MaDatPhong", "MaPhong", "Loai", "NoiDung", "ThoiGianHen" }`; loại: Báo thức, Đặt xe / đưa đón, Nhắn gửi, Đồ thất lạc, Buồng phòng, Kỹ thuật, Khác. Loại Buồng phòng tự tạo (hoặc gộp vào) việc buồng phòng của phòng; loại Kỹ thuật tự mở phiếu kỹ thuật; xử lý xong thì yêu cầu tự hoàn thành |
| PATCH | `/{id}` | FrontDesk | Đổi trạng thái `{ "TrangThai": "InProgress" \| "Done" \| "Cancelled", "KetQua" }` (huỷ phải có lý do; yêu cầu đã xong không đổi được) |

## Hoá đơn — `/api/invoices`

| Phương thức | Đường dẫn | Quyền | Mô tả |
|---|---|---|---|
| GET | `/?tuNgay&denNgay&trangThai&search` | Finance | Danh sách hoá đơn |
| GET | `/{maHoaDon}` | Cashier, Customer | Chi tiết, giao dịch, số tiền còn hoàn được (khách chỉ xem hoá đơn của mình) |
| POST | `/` | Cashier | Lập hoặc tính lại hoá đơn `{ "MaDatPhong", "MaKhuyenMai" }`. Gồm số đêm thực tế, giá từng đêm, phụ thu trả muộn, khuyến mãi, ưu đãi hạng thành viên, tiền cọc |
| POST | `/{id}/payments` | Cashier | Thanh toán và trả phòng `{ "PhuongThuc", "DiemSuDung", "CacKhoan" }`. Điểm đổi được ghi thành khoản "Điểm thưởng". `CacKhoan` (tối đa 3 `{ "PhuongThuc", "SoTien", "GhiChu" }`) để tách thanh toán, tổng phải bằng số còn phải thu. Cọc thừa được ghi hoàn vào sổ cọc |
| PUT | `/{id}/company` | Cashier | Thông tin xuất hoá đơn VAT công ty `{ "TenCongTy", "MaSoThue", "DiaChiCongTy", "EmailHoaDon" }` (MST 10 số hoặc 10-3 số; trống = hoá đơn cá nhân) |
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
| GET | `/lookup?q` | FrontDesk | Tra khách cũ theo đúng số CCCD/Passport hoặc SĐT (≥ 6 ký tự, tối đa 5 hồ sơ, kèm VIP / hạn chế / sở thích / ghi chú nội bộ) |
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
| GET | `/api/housekeeping` | Admin, Lễ tân, Buồng phòng | Tổng quan buồng phòng: sơ đồ mọi phòng theo tầng (trạng thái, việc đang mở, khách đến / đi hôm nay), việc theo ưu tiên, số phòng có khách đến mà chưa sạch |
| GET | `/api/housekeeping/tasks`, `/tasks/mine` | Như trên / Buồng phòng | Công việc dọn phòng, sắp theo ưu tiên (`UuTien` 1 gấp: khách VIP đến hôm nay; 2 cao: khách đến hôm nay / yêu cầu của khách; 3 thường; 4 sự cố) kèm `LyDoUuTien`, `Tang` |
| GET | `/api/housekeeping/staff` | FrontDesk | Nhân viên có thể giao việc |
| POST | `/api/housekeeping/tasks/{id}/assign` | FrontDesk | Giao việc |
| POST | `/api/housekeeping/tasks/assign-bulk` | FrontDesk | Giao nhiều việc chưa bắt đầu cho một nhân viên `{ "MaNhanVien", "MaPhanCongs": [...] }` (vd: cả tầng) |
| POST | `/api/housekeeping/tasks/{id}/start` · `/complete` · `/issue` | Buồng phòng | Bắt đầu / dọn xong (phòng trống → "Chờ kiểm tra"; người có quyền kiểm tra tự hoàn thành thì coi như đạt) / báo sự cố (tự mở phiếu kỹ thuật khoá phòng) |
| POST | `/api/housekeeping/tasks/{id}/inspect` | RoomInspectors (Admin, Lễ tân) | Kiểm tra phòng `{ "Dat", "MucChuaDat": [...], "GhiChu" }` theo danh mục của hạng phòng (cấu hình `Housekeeping` trong appsettings.json). Đạt: phòng sẵn sàng; chưa đạt: trả lại nhân viên dọn lại |
| POST | `/api/housekeeping/rooms/{maPhong}/minibar` | Admin, Lễ tân, Buồng phòng | Ghi đồ minibar `{ "Items": [{ "MaDichVu", "SoLuong" }] }` vào bảng kê của khách đang ở; chỉ dịch vụ đánh dấu `LaMinibar` |
| GET | `/api/maintenance?loc` | Admin, Lễ tân, Buồng phòng | Phiếu kỹ thuật: `mo` (mới / đang sửa, khẩn trước), `xong`, trống: 30 ngày |
| POST | `/api/maintenance` | Admin, Lễ tân, Buồng phòng | Mở phiếu `{ "MaPhong", "MoTa", "MucDo": "Thường" \| "Khẩn", "KhoaPhong" }`; khoá phòng → Bảo trì (bỏ qua khi phòng có khách) |
| PATCH | `/api/maintenance/{id}` | FrontDesk | Đang sửa / đã xong / huỷ (huỷ cần lý do). Phiếu khoá phòng cuối cùng kết thúc → phòng Đang dọn + việc "Dọn sau sửa chữa"; yêu cầu khách loại Kỹ thuật liên kết tự hoàn thành |
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
