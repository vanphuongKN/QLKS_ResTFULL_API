# Kiểm thử end-to-end

Các script gọi thẳng vào QLKSan.API và QLKSan.Web đang chạy (dữ liệu thật trong CSDL), không cần cài thêm thư viện Python.

## Chuẩn bị
1. Chạy **QLKSan.API** (`https://localhost:7001`) và **QLKSan.Web** (`https://localhost:7234`).
2. Tài khoản mẫu: `admin1`, `letan1`, `buongphong1`, `ketoan1`, `khach1` — mật khẩu `123456`.
3. Chạy từ thư mục này (`csrfhelp.py` phải nằm cùng thư mục). Trên Windows nên đặt `PYTHONIOENCODING=utf-8`.

Địa chỉ server mặc định là cổng chuẩn; chạy server ở cổng khác thì đặt biến môi trường `QLKSAN_API` / `QLKSAN_WEB` (ví dụ `QLKSAN_API=https://localhost:7101`), `QLKSAN_DB` cho tên database mà `flow_test_m1.py` dọn dữ liệu.

## Các script

| Script | Nội dung | Ghi dữ liệu? |
|---|---|---|
| `api_test.py` | 89 kiểm tra API: xác thực, phân quyền, danh mục công khai, validation, lỗi 404/400/409, CRUD | Chỉ CRUD tự xoá ngay (loại phòng, phòng, dịch vụ, nhân viên) |
| `web_auth_test.py` | Đăng nhập cookie, phân quyền trang, returnUrl, CSRF | Không |
| `web_crud_test.py` | Thao tác quản trị qua giao diện Web | Chỉ dữ liệu tự xoá ngay |
| `flow_test_m1.py` | Luồng Mức 1: phòng trống theo ngày, đặt trùng lúc, huỷ/từ chối, check-in đúng ngày, tính tiền theo ngày thực tế (số tiền kỳ vọng lấy từ báo giá nên đúng cả ngày cuối tuần/lễ), KM khi đặt, trừ tiền cọc | **Có** — tự dọn sạch ở cuối (cần `sqlcmd`, thông tin kết nối giống `appsettings.json`; đặt `QLKSAN_DB` nếu dùng database khác) |
| `flow_test_m2.py` | Mức 2: vai trò Buồng phòng/Kế toán, ma trận phân quyền API + Web, giao việc → dọn xong/báo sự cố, quản lý nhân viên (vai trò, nghỉ việc, đặt lại mật khẩu), hoàn tiền, báo cáo, xuất Excel | **Có, KHÔNG tự dọn** — chỉ chạy trên database vừa dựng từ `DB/Quanlykhachsanc#.sql` (cần tài khoản `buongphong1`, `ketoan1`) |
| `flow_test_m3.py` | Mức 3: quy tắc giá mùa/cuối tuần/lễ (phân quyền, validation, báo giá từng đêm, bật/tắt, form Bảng giá), cọc & hoá đơn theo giá đêm, lịch phòng, in hoá đơn (khách chỉ xem hoá đơn của mình), sổ khách lưu trú + Excel | **Có** (1 lượt khách vãng lai; quy tắc giá thử tự xoá) — chỉ chạy trên database vừa dựng từ script |
| `flow_test_khach.py` | Luồng khách hàng (kế hoạch nâng cấp): liên kết tài khoản khách vãng lai, đổi mật khẩu, sửa liên hệ… (bổ sung theo từng giai đoạn) | **Có, KHÔNG tự dọn** — chỉ chạy trên database vừa dựng từ script |
| `ui_shoot.mjs` | Chụp toàn trang mọi vai trò (desktop/điện thoại) bằng Chrome headless và đo lỗi bố cục: tràn ngang, ảnh hỏng, lỗi JS, file 404 → `_report.json` | Không |
| `snapshot_pages.py` + `compare_snaps.py` | Chụp nội dung chữ của các trang để so sánh trước/sau khi sửa giao diện | Không |

```bash
python api_test.py
python web_auth_test.py
python web_crud_test.py
python flow_test_m1.py
python flow_test_m3.py      # database vừa dựng từ script
python snapshot_pages.py snap_truoc   # ... sửa code ...
python snapshot_pages.py snap_sau && python compare_snaps.py snap_truoc snap_sau
```

Unit test (không cần chạy server): `dotnet test QLKSan.Tests`

Kiểm tra giao diện (cần Node 18+ và Chrome): mở Chrome headless trước rồi chạy script, ví dụ
```bash
"C:/Program Files/Google/Chrome/Application/chrome.exe" --headless=new --remote-debugging-port=9555 --ignore-certificate-errors --user-data-dir=%TEMP%\qlks-chrome
node ui_shoot.mjs out/mobile 390 844      # màn hình điện thoại
node ui_shoot.mjs out/desktop 1440 900    # màn hình máy tính
```
