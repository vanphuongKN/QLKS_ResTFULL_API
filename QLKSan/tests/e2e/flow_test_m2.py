"""Kiểm tra Mức 2: vai trò Buồng phòng / Kế toán, ma trận phân quyền, luồng buồng phòng, quản lý nhân viên, kế toán & báo cáo.
CHỈ chạy trên database thử dựng từ DB/Quanlykhachsanc#.sql (script ghi dữ liệu và không tự dọn)."""
import os
from csrfhelp import with_token
import html, http.cookiejar, io, json, re, ssl, sys, urllib.parse, urllib.request, urllib.error, zipfile, datetime

WEB, API = os.environ.get('QLKSAN_WEB', 'https://localhost:7234'), os.environ.get('QLKSAN_API', 'https://localhost:7001')
CTX = ssl._create_unverified_context()
results = []
today = datetime.date.today()

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k): return None

def web_session():
    return urllib.request.build_opener(urllib.request.HTTPSHandler(context=CTX), urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()), NoRedirect)

def web(op, method, path, form=None, raw=False):
    form = with_token(op, method, form)
    data = urllib.parse.urlencode(form, doseq=True).encode() if form is not None else None
    try:
        with op.open(urllib.request.Request(WEB + path, data=data, method=method)) as r:
            body = r.read()
            return r.status, r.headers.get('Location'), (body if raw else html.unescape(body.decode('utf-8', 'replace'))), r.headers
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get('Location'), html.unescape(e.read().decode('utf-8', 'replace')), e.headers

def web_post(op, path, form):
    s, loc, body, _ = web(op, 'POST', path, form)
    if s == 302 and loc:
        u = urllib.parse.urlparse(loc); _, _, body, _ = web(op, 'GET', u.path + ('?' + u.query if u.query else ''))
    return s, loc, body

def web_login(user, pwd='123456'):
    op = web_session()
    s, loc, body, _ = web(op, 'POST', '/Auth/Login', {'username': user, 'password': pwd})
    return op, s, loc, body

def api(method, path, token=None, body=None):
    req = urllib.request.Request(API + path, data=json.dumps(body).encode() if body is not None else None, method=method)
    req.add_header('Content-Type', 'application/json')
    if token: req.add_header('Authorization', 'Bearer ' + token)
    try:
        with urllib.request.urlopen(req, context=CTX) as r:
            raw = r.read().decode(); return r.status, json.loads(raw) if raw else None
    except urllib.error.HTTPError as e:
        raw = e.read().decode()
        try: return e.code, json.loads(raw) if raw else None
        except Exception: return e.code, raw

def login(user, pwd='123456'):
    return api('POST', '/api/auth/login', body={'TenDangNhap': user, 'MatKhau': pwd})

def tok(user): return login(user)[1]['AccessToken']

def alert(body):
    m = re.search(r'alert-(success|danger)[^>]*>\s*(?:<[^>]+>\s*)*([^<]+)', body)
    return (m.group(1), m.group(2).strip()) if m else (None, None)

def check(name, cond, info=''):
    results.append(bool(cond)); print(f"{'OK  ' if cond else 'FAIL'} {name}{' | ' + str(info) if info != '' else ''}")

A, L, BP, KT, K = tok('admin1'), tok('letan1'), tok('buongphong1'), tok('ketoan1'), tok('khach1')
room = lambda ma: api('GET', f'/api/rooms/{ma}', A)[1]
open_tasks = lambda: api('GET', '/api/housekeeping/tasks', A)[1]

print('===== 2.1 Vai trò mới & trang sau đăng nhập =====')
s, info = login('buongphong1')
check('buongphong1 đăng nhập → vai trò Housekeeping, gắn NV06', info['VaiTro'] == 'Housekeeping' and info['MaNhanVien'] == 'NV06', f"{info['VaiTro']} {info['HoTen']}")
s, info = login('ketoan1')
check('ketoan1 đăng nhập → vai trò Accountant, gắn NV08', info['VaiTro'] == 'Accountant' and info['MaNhanVien'] == 'NV08', f"{info['VaiTro']} {info['HoTen']}")
bpw, s, loc, _ = web_login('buongphong1')
check('Web: buongphong1 → trang "Việc của tôi"', loc and loc.startswith('/Housekeeping/MyTasks'), loc)
ktw, s, loc, _ = web_login('ketoan1')
check('Web: ketoan1 → trang Báo cáo', loc and loc.startswith('/Accounting/Reports'), loc)
ltw = web_login('letan1')[0]; adw = web_login('admin1')[0]
_, _, body, _ = web(ktw, 'GET', '/Accounting/Reports')
menu = body[body.find('sidebar-nav'):body.find('sidebar-logout')]
check('Menu kế toán: có Hoá đơn, Báo cáo, Phòng nghỉ; không có Lễ tân/Dịch vụ/Buồng phòng',
      'Hoá đơn' in menu and 'Báo cáo' in menu and 'Phòng nghỉ' in menu and 'Lễ tân' not in menu and 'Dịch vụ' not in menu and 'Buồng phòng' not in menu)
check('Topbar hiển thị "Kế toán"', 'Kế toán' in body[body.find('topbar'):])
_, _, body, _ = web(bpw, 'GET', '/Housekeeping/MyTasks')
menu = body[body.find('sidebar-nav'):body.find('sidebar-logout')]
check('Menu buồng phòng: chỉ "Việc của tôi"', 'Việc của tôi' in menu and 'Hoá đơn' not in menu and 'Lễ tân' not in menu and 'Phòng nghỉ' not in menu)

print('\n===== 2.2 Ma trận phân quyền (API) =====')
matrix = [
    ('Kế toán', KT, 'GET', '/api/rooms', 200), ('Kế toán', KT, 'GET', '/api/bookings', 200),
    ('Kế toán', KT, 'GET', '/api/invoices', 200), ('Kế toán', KT, 'GET', '/api/reports/revenue', 200),
    ('Kế toán', KT, 'GET', '/api/dashboard', 200), ('Kế toán', KT, 'POST', '/api/bookings/PDP0000002/approve', 403),
    ('Kế toán', KT, 'POST', '/api/bookings/walk-in', 403), ('Kế toán', KT, 'GET', '/api/staff', 403),
    ('Kế toán', KT, 'GET', '/api/housekeeping', 403),
    ('Buồng phòng', BP, 'GET', '/api/housekeeping', 200), ('Buồng phòng', BP, 'GET', '/api/housekeeping/tasks/mine', 200),
    ('Buồng phòng', BP, 'GET', '/api/bookings', 403), ('Buồng phòng', BP, 'GET', '/api/bookings/PDP0000001', 403),
    ('Buồng phòng', BP, 'GET', '/api/invoices', 403), ('Buồng phòng', BP, 'GET', '/api/reports/revenue', 403),
    ('Buồng phòng', BP, 'GET', '/api/customers', 403), ('Buồng phòng', BP, 'POST', '/api/housekeeping/tasks/PC0006/assign', 403),
    ('Buồng phòng', BP, 'POST', '/api/invoices', 403), ('Buồng phòng', BP, 'GET', '/api/housekeeping/staff', 403),
    ('Lễ tân', L, 'GET', '/api/invoices', 403), ('Lễ tân', L, 'GET', '/api/reports/revenue', 403),
    ('Lễ tân', L, 'POST', '/api/invoices/HD01/refunds', 403), ('Lễ tân', L, 'GET', '/api/housekeeping/staff', 200),
    ('Lễ tân', L, 'GET', '/api/housekeeping/tasks/mine', 403), ('Lễ tân', L, 'GET', '/api/dashboard', 403),
    ('Khách hàng', K, 'GET', '/api/housekeeping', 403), ('Khách hàng', K, 'GET', '/api/invoices', 403),
    ('Admin', A, 'GET', '/api/reports/occupancy', 200), ('Admin', A, 'GET', '/api/housekeeping/tasks/mine', 200),
]
bad = []
for role, t, m, p, exp in matrix:
    body = {'LyDo': 'x', 'SoTien': 1000, 'PhuongThuc': 'x', 'MaNhanVien': 'NV06'} if m in ('POST', 'PATCH') else None
    s, _ = api(m, p, t, body)
    if s != exp: bad.append(f'{role} {m} {p}: {s} (cần {exp})')
check(f'{len(matrix)} cặp vai trò × endpoint đúng quyền', not bad, '; '.join(bad))

print('\n===== Phân quyền trang Web =====')
pages = [('ketoan1', ktw, '/Accounting/Invoices', 200), ('ketoan1', ktw, '/Room/Status', 200), ('ketoan1', ktw, '/Reception', 302),
         ('ketoan1', ktw, '/Housekeeping/Tasks', 302), ('ketoan1', ktw, '/Admin', 302),
         ('buongphong1', bpw, '/Housekeeping/MyTasks', 200), ('buongphong1', bpw, '/Housekeeping/Tasks', 302),
         ('buongphong1', bpw, '/Accounting/Reports', 302), ('buongphong1', bpw, '/Room/Status', 302),
         ('letan1', ltw, '/Accounting/Reports', 302), ('letan1', ltw, '/Housekeeping/Tasks', 200), ('letan1', ltw, '/Housekeeping/MyTasks', 302),
         ('admin1', adw, '/Accounting/Reports', 200), ('admin1', adw, '/Housekeeping/MyTasks', 200)]
bad = []
for u, op, p, exp in pages:
    s, loc, _, _ = web(op, 'GET', p)
    if s != exp or (exp == 302 and 'AccessDenied' not in (loc or '')): bad.append(f'{u} {p}: {s} {loc or ""}')
check(f'{len(pages)} trang Web đúng quyền (302 = chuyển tới trang "không có quyền")', not bad, '; '.join(bad))
_, _, body, _ = web(ktw, 'GET', '/Room/Details/P101')
check('Kế toán xem chi tiết phòng nhưng không thấy nút đổi trạng thái', 'UpdateStatus' not in body)

print('\n===== 2.4 Luồng buồng phòng =====')
_, _, body, _ = web(ltw, 'GET', '/Housekeeping/Tasks')
check('Lễ tân thấy bảng phân công: P302 đã giao cho Ngô Thị Mai', 'Phân công dọn phòng' in body and 'Phòng 302' in body and 'Ngô Thị Mai' in body)
_, _, body, _ = web(bpw, 'GET', '/Housekeeping/MyTasks')
check('Buồng phòng thấy việc P302 trong "Việc của tôi"', 'Phòng 302' in body)
s, loc, body = web_post(bpw, '/Housekeeping/Start', {'id': 'PC0006'})
t = next(x for x in open_tasks() if x['MaPhanCong'] == 'PC0006')
check('Bắt đầu dọn → Đang dọn', alert(body)[0] == 'success' and t['TrangThai'] == 'InProgress', alert(body)[1])
s, loc, body = web_post(bpw, '/Housekeeping/Complete', {'id': 'PC0006'})
check('Dọn xong → phòng P302 Trống, việc biến khỏi danh sách đang mở', alert(body)[0] == 'success' and room('P302')['TrangThai'] == 'Available'
      and not any(x['MaPhanCong'] == 'PC0006' for x in open_tasks()), alert(body)[1])
check('  Việc đã xong hiện ở mục "Đã hoàn thành hôm nay"', 'Đã hoàn thành hôm nay' in body and 'Phòng 302' in body)

# Trả phòng khách vãng lai P303 → tự tạo việc dọn
stay = api('GET', '/api/rooms/P303/current-stay', L)[1]
web_post(ltw, '/Checkout/GenerateInvoice', {'maDatPhong': stay['MaDatPhong'], 'maKhuyenMai': '', 'maPhong': 'P303'})
inv = api('GET', '/api/rooms/P303/current-stay', L)[1]['HoaDon']
s, loc, body = web_post(ltw, '/Checkout/Pay', {'maHoaDon': inv['MaHoaDon'], 'phuongThuc': 'Tiền mặt'})
new_task = next((x for x in open_tasks() if x['MaPhong'] == 'P303'), None)
check('Trả phòng P303 → tự tạo việc "Dọn sau trả phòng" chờ giao', new_task and new_task['TrangThai'] == 'Unassigned'
      and new_task['MaDatPhong'] == stay['MaDatPhong'] and new_task['LoaiCongViec'] == 'Dọn sau trả phòng', new_task and new_task['MaPhanCong'])
s, r = api('POST', f"/api/housekeeping/tasks/{new_task['MaPhanCong']}/assign", L, {'MaNhanVien': 'NV02'})
check('Giao việc cho lễ tân (không phải buồng phòng) → bị chặn', s == 400, r and r.get('detail'))
s, loc, body = web_post(ltw, '/Housekeeping/Assign', {'maPhanCong': new_task['MaPhanCong'], 'maNhanVien': 'NV06'})
check('Lễ tân giao việc P303 cho Ngô Thị Mai (qua Web)', alert(body) == ('success', 'Đã giao việc dọn phòng.'))
s, loc, body = web_post(bpw, '/Housekeeping/ReportIssue', {'id': new_task['MaPhanCong'], 'ghiChu': 'Vòi sen rò nước'})
t = next(x for x in open_tasks() if x['MaPhanCong'] == new_task['MaPhanCong'])
check('Báo sự cố → việc "Sự cố", phòng P303 chuyển Bảo trì', t['TrangThai'] == 'Issue' and 'Vòi sen rò nước' in t['GhiChu'] and room('P303')['TrangThai'] == 'Maintenance', t['GhiChu'])
_, _, body, _ = web(ltw, 'GET', '/Housekeeping/Tasks')
check('  Lễ tân thấy sự cố trong bảng phân công', 'Vòi sen rò nước' in body)
s, loc, body = web_post(ltw, '/Room/UpdateStatus', {'maPhong': 'P303', 'status': 'Available'})
check('Sửa xong, lễ tân đổi P303 sang Trống → sự cố được đóng', room('P303')['TrangThai'] == 'Available'
      and not any(x['MaPhanCong'] == new_task['MaPhanCong'] for x in open_tasks()))

# Dọn theo yêu cầu + quyền sở hữu công việc
api('PATCH', '/api/rooms/P203', L, {'TrangThai': 'Cleaning'})
t203 = next((x for x in open_tasks() if x['MaPhong'] == 'P203'), None)
check('Đổi tay P203 sang "Chờ dọn" → tự tạo việc "Dọn theo yêu cầu"', t203 and t203['LoaiCongViec'] == 'Dọn theo yêu cầu')
api('POST', f"/api/housekeeping/tasks/{t203['MaPhanCong']}/assign", L, {'MaNhanVien': 'NV07'})
s, r = api('POST', f"/api/housekeeping/tasks/{t203['MaPhanCong']}/complete", BP)
check('Buồng phòng hoàn thành việc giao cho người khác → bị chặn', s == 400 and 'không được giao cho bạn' in r['detail'], r.get('detail'))
s, r = api('POST', f"/api/housekeeping/tasks/{t203['MaPhanCong']}/complete", L)
check('Lễ tân (quản lý) xác nhận hoàn thành thay → P203 Trống', s == 200 and room('P203')['TrangThai'] == 'Available')

print('\n===== 2.3 Quản lý nhân viên =====')
s, loc, body = web_post(adw, '/Admin/AddStaff', {'tenNhanVien': 'Kế Toán Hai', 'chucVu': 'Kế toán', 'sdt': '0912345680', 'email': 'kt2@test.vn',
                                                 'tenDangNhap': 'ketoan2', 'matKhau': '123456', 'vaiTro': 'Accountant'})
s2, info = login('ketoan2')
check('Admin thêm nhân viên vai trò Kế toán (qua Web) → đăng nhập đúng vai trò', alert(body)[0] == 'success' and s2 == 200 and info['VaiTro'] == 'Accountant', info.get('VaiTro') if isinstance(info, dict) else info)
nv = info['MaNhanVien']
check('  Bảng nhân viên hiển thị vai trò "Kế toán"', 'Kế Toán Hai' in body and 'Kế toán' in body)
s, loc, body = web_post(adw, '/Admin/EditStaff', {'maNhanVien': nv, 'tenNhanVien': 'Kế Toán Hai', 'chucVu': 'Kế toán', 'sdt': '0912345680',
                                                  'email': 'kt2@test.vn', 'vaiTro': 'Accountant', 'trangThai': 'Resigned'})
s2, r = login('ketoan2')
check('Cho nghỉ việc → đăng nhập bị chặn (403)', alert(body)[0] == 'success' and s2 == 403 and 'ngừng hoạt động' in r['detail'], r.get('detail'))
_, _, b2, _ = web_login('ketoan2')[0:4] if False else (None, None, web(web_session(), 'GET', '/')[2], None)
op2 = web_session(); _, _, body2, _ = web(op2, 'POST', '/Auth/Login', {'username': 'ketoan2', 'password': '123456'})
check('  Trang đăng nhập Web hiện thông báo tài khoản ngừng hoạt động', 'ngừng hoạt động' in body2)
web_post(adw, '/Admin/EditStaff', {'maNhanVien': nv, 'tenNhanVien': 'Kế Toán Hai', 'chucVu': 'Lễ tân', 'sdt': '0912345680',
                                   'email': 'kt2@test.vn', 'vaiTro': 'Receptionist', 'trangThai': 'Working'})
s, loc, body = web_post(adw, '/Admin/ResetPassword', {'maNhanVien': nv, 'matKhauMoi': 'matkhaumoi'})
s1, _ = login('ketoan2'); s2, info = login('ketoan2', 'matkhaumoi')
check('Đổi vai trò sang Lễ tân + đặt lại mật khẩu → mật khẩu cũ hỏng, mới dùng được', s1 == 401 and s2 == 200 and info['VaiTro'] == 'Receptionist')
s, r = api('PUT', '/api/staff/NV01', A, {'TenNhanVien': 'Nguyễn Hoàng Đức', 'ChucVu': 'Quản lý', 'SDT': '0901234567', 'Email': 'admin1@ute.udn.vn', 'TrangThai': 'Resigned'})
check('Admin tự cho mình nghỉ việc → bị chặn', s == 400, r and r.get('detail'))
s, r = api('PUT', '/api/staff/NV01', A, {'TenNhanVien': 'Nguyễn Hoàng Đức', 'ChucVu': 'Quản lý', 'SDT': '0901234567', 'Email': 'admin1@ute.udn.vn', 'VaiTro': 'Receptionist'})
check('Admin tự hạ quyền mình → bị chặn', s == 400, r and r.get('detail'))
s, r = api('POST', '/api/staff', A, {'TenNhanVien': 'X', 'ChucVu': 'X', 'SDT': '0912345681', 'Email': 'x@x.vn', 'TenDangNhap': 'xx_khach', 'MatKhau': '123456', 'VaiTro': 'Customer'})
check('Tạo nhân viên với vai trò Khách hàng → bị chặn', s == 400, r and r.get('detail'))
s, loc, body = web_post(adw, '/Admin/DeleteStaff', {'maNhanVien': nv})
check('Xoá nhân viên chưa có lịch sử', alert(body)[0] == 'success')
s, r = api('DELETE', '/api/staff/NV06', A)
check('Xoá nhân viên đã có lịch sử công việc → gợi ý chuyển "Nghỉ việc"', s == 409 and 'Nghỉ việc' in r['detail'], r.get('detail'))

print('\n===== 2.5 Kế toán & báo cáo =====')
_, _, body, _ = web(ktw, 'GET', '/Accounting/Invoices')
check('Danh sách hoá đơn có hoá đơn mới vừa thanh toán', inv['MaHoaDon'] in body and 'HD01' in body)
_, _, body, _ = web(ktw, 'GET', '/Accounting/Invoices?search=cam')
check('Tìm "cam" (không dấu) → hoá đơn của Lê Thị Cẩm', 'HD03' in body and 'HD01' not in body)
d3 = api('GET', '/api/invoices/HD03', KT)[1]
check('Chi tiết HD03: có giao dịch hoàn 100.000đ, còn hoàn được 1.628.000đ',
      any(t['LoaiGiaoDich'] == 'Refund' and t['SoTienDaTra'] == 100000 for t in d3['GiaoDich']) and d3['CoTheHoan'] == 1628000, d3['CoTheHoan'])
dn = api('GET', f"/api/invoices/{inv['MaHoaDon']}", KT)[1]
check('Hoá đơn mới ghi nhận nhân viên thu là letan1 (NV02)', dn['GiaoDich'][0]['MaNhanVien'] == 'NV02' and dn['HoaDon']['TienCoc'] == 750000)
s, r = api('POST', '/api/invoices/HD01/refunds', KT, {'SoTien': 99999999, 'PhuongThuc': 'Chuyển khoản', 'LyDo': 'x'})
check('Hoàn quá số tiền → bị chặn', s == 400 and 'vượt quá' in r['detail'], r.get('detail'))
pts_before = api('GET', '/api/customers/KH01', L)[1]['DiemTichLuy']
s, loc, body = web_post(ktw, '/Accounting/Refund', {'id': 'HD01', 'soTien': 50000, 'phuongThuc': 'Tiền mặt', 'lyDo': 'Bồi thường ăn sáng trễ'})
pts_after = api('GET', '/api/customers/KH01', L)[1]['DiemTichLuy']
check('Kế toán hoàn 50.000đ cho HD01 (qua Web) → trừ 5 điểm tích luỹ', alert(body)[0] == 'success' and pts_before - pts_after == 5, f'{pts_before} → {pts_after}')
check('  Trang chi tiết hiển thị giao dịch hoàn', 'Bồi thường ăn sáng trễ' in body)

frm, to = (today - datetime.timedelta(days=40)).isoformat(), today.isoformat()
rep = api('GET', f'/api/reports/revenue?tuNgay={frm}&denNgay={to}', KT)[1]
all_inv = api('GET', '/api/invoices?trangThai=Paid', KT)[1]
check('Báo cáo: tổng doanh thu = tổng các hoá đơn đã thanh toán', abs(rep['TongDoanhThu'] - round(sum(i['TongTienCanThanhToan'] for i in all_inv))) <= len(all_inv),
      f"{rep['TongDoanhThu']:,.0f} ({rep['SoHoaDon']} HĐ)")
check('  Hoàn tiền = 100.000 + 50.000; doanh thu thuần = tổng − hoàn', rep['TongHoanTien'] == 150000 and rep['DoanhThuThuan'] == rep['TongDoanhThu'] - 150000)
coc = next((m for m in rep['TheoPhuongThuc'] if m['PhuongThuc'].startswith('Tiền cọc')), None)
check('  Có dòng "Tiền cọc" = 600.000 + 750.000', coc and coc['SoTien'] == 1350000, coc)
check('  Tổng theo phương thức (thu + cọc − hoàn) = doanh thu thuần', abs(sum(m['SoTien'] for m in rep['TheoPhuongThuc']) - rep['DoanhThuThuan']) <= 1)
check('  Có đối soát theo nhân viên thu (Trần Thị Bích)', any(n['TenNhanVien'] == 'Trần Thị Bích' for n in rep['TheoNhanVien']))
check('  Có doanh thu theo loại phòng và dịch vụ', len(rep['TheoLoaiPhong']) >= 3 and len(rep['TheoDichVu']) >= 3)
occ = api('GET', f'/api/reports/occupancy?tuNgay={frm}&denNgay={to}', KT)[1]
check('Công suất phòng: 9 phòng, có đêm phòng đã bán, tỷ lệ trong 0-100%', occ['TongSoPhong'] == 9 and occ['TongDemPhongDaBan'] > 0
      and all(0 <= d['TyLe'] <= 100 for d in occ['TheoNgay']), f"TB {occ['TyLeTrungBinh']}% · {occ['TongDemPhongDaBan']} đêm phòng")
_, _, body, _ = web(ktw, 'GET', f'/Accounting/Reports?tuNgay={frm}&denNgay={to}')
check('Trang báo cáo hiển thị doanh thu thuần', f"{rep['DoanhThuThuan']:,.0f}" in body)
s, _, content, headers = web(ktw, 'GET', f'/Accounting/Export?tuNgay={frm}&denNgay={to}', raw=True)
ok_xlsx = False; sheets = []
try:
    z = zipfile.ZipFile(io.BytesIO(content)); wb = z.read('xl/workbook.xml').decode('utf-8')
    sheets = re.findall(r'<(?:\w+:)?sheet\b[^>]*\bname="([^"]+)"', wb); ok_xlsx = True
except Exception as e:
    sheets = [str(e)]
check('Xuất Excel → file .xlsx hợp lệ, đủ các sheet', s == 200 and ok_xlsx and 'Tổng quan' in sheets and 'Hoá đơn' in sheets and 'Theo nhân viên thu' in sheets,
      f"{len(content)} bytes, sheet: {sheets}")

print(f"\n==> {sum(results)}/{len(results)} kiểm tra đạt")
sys.exit(0 if all(results) else 1)
