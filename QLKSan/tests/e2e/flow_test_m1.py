"""Kiểm tra các luồng Mức 1 qua Web/API thật, ghi lại mọi bản ghi tạo ra và TỰ DỌN DẸP ở cuối (kể cả khi lỗi)."""
from csrfhelp import with_token
import datetime, html, http.cookiejar, json, os, random, re, ssl, subprocess, sys, threading, urllib.parse, urllib.request, urllib.error

WEB, API = os.environ.get('QLKSAN_WEB', 'https://localhost:7234'), os.environ.get('QLKSAN_API', 'https://localhost:7001')
CTX = ssl._create_unverified_context()
results = []
created = {'bookings': set(), 'customers': set(), 'accounts': set()}
today = datetime.date.today()
d = lambda n: (today + datetime.timedelta(days=n)).isoformat()
dvn = lambda n: (today + datetime.timedelta(days=n)).strftime('%d/%m/%Y')

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k): return None

def web_session():
    return urllib.request.build_opener(urllib.request.HTTPSHandler(context=CTX), urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()), NoRedirect)

def web(op, method, path, form=None):
    form = with_token(op, method, form)
    data = urllib.parse.urlencode(form, doseq=True).encode() if form is not None else None
    try:
        with op.open(urllib.request.Request(WEB + path, data=data, method=method)) as r:
            return r.status, r.headers.get('Location'), html.unescape(r.read().decode('utf-8', 'replace'))
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get('Location'), html.unescape(e.read().decode('utf-8', 'replace'))

def web_post(op, path, form):
    s, loc, body = web(op, 'POST', path, form)
    if s == 302 and loc:
        u = urllib.parse.urlparse(loc); s2, _, body = web(op, 'GET', u.path + ('?' + u.query if u.query else ''))
    return s, loc, body

def web_login(user):
    op = web_session(); web(op, 'POST', '/Auth/Login', {'username': user, 'password': '123456'}); return op

def api(method, path, token, body=None):
    req = urllib.request.Request(API + path, data=json.dumps(body).encode() if body is not None else None, method=method)
    req.add_header('Content-Type', 'application/json'); req.add_header('Authorization', 'Bearer ' + token)
    try:
        with urllib.request.urlopen(req, context=CTX) as r:
            raw = r.read().decode(); return r.status, json.loads(raw) if raw else None
    except urllib.error.HTTPError as e:
        raw = e.read().decode(); return e.code, json.loads(raw) if raw else None

def api_login(user):
    s, data = api_anon('POST', '/api/auth/login', {'TenDangNhap': user, 'MatKhau': '123456'}); return data

def api_anon(method, path, body):
    req = urllib.request.Request(API + path, data=json.dumps(body).encode(), method=method); req.add_header('Content-Type', 'application/json')
    with urllib.request.urlopen(req, context=CTX) as r: return r.status, json.loads(r.read())

def alert(body):
    m = re.search(r'alert-(success|danger)[^>]*>\s*(?:<[^>]+>\s*)*([^<]+)', body)
    return (m.group(1), m.group(2).strip()) if m else (None, None)

def check(name, cond, info=''):
    results.append(bool(cond)); print(f"{'OK  ' if cond else 'FAIL'} {name}{' | ' + str(info) if info != '' else ''}")

def track_booking(ma):
    if ma: created['bookings'].add(ma)
    return ma

def book_api(token, phong, nhan, tra, km=None, so=1):
    return api('POST', '/api/bookings', token, {'MaPhong': phong, 'SoNguoi': so, 'CheckIn': nhan, 'CheckOut': tra, 'MaKhuyenMai': km})

def cleanup():
    b = [x for x in created['bookings'] if x]; c = [x for x in created['customers'] if x]; a = [x for x in created['accounts'] if x]
    if not (b or c or a): return
    lst = lambda xs: ','.join("'" + x.replace("'", "''") + "'" for x in xs) or "''"
    sql = f"""SET NOCOUNT ON; SET XACT_ABORT ON; BEGIN TRAN;
DELETE FROM LichSuGiaoDich WHERE MaThanhToan IN (SELECT MaThanhToan FROM ThanhToan WHERE MaHoaDon IN (SELECT MaHoaDon FROM HoaDon WHERE MaDatPhong IN ({lst(b)})));
DELETE FROM ThanhToan WHERE MaHoaDon IN (SELECT MaHoaDon FROM HoaDon WHERE MaDatPhong IN ({lst(b)}));
DELETE FROM HoaDon WHERE MaDatPhong IN ({lst(b)});
DELETE FROM SuDungDichVu WHERE MaDatPhong IN ({lst(b)});
DELETE FROM ChiTietDatPhong WHERE MaDatPhong IN ({lst(b)});
DELETE FROM PhieuDatPhong WHERE MaDatPhong IN ({lst(b)});
DELETE FROM DanhGia WHERE MaKhachHang IN ({lst(c)});
DELETE FROM LienHe_DiaChi WHERE MaKhachHang IN ({lst(c)}) AND NOT EXISTS (SELECT 1 FROM PhieuDatPhong p WHERE p.MaKhachHang = LienHe_DiaChi.MaKhachHang);
DELETE FROM KhachHang WHERE MaKhachHang IN ({lst(c)}) AND NOT EXISTS (SELECT 1 FROM PhieuDatPhong p WHERE p.MaKhachHang = KhachHang.MaKhachHang);
DELETE FROM TaiKhoan WHERE MaDangNhap IN ({lst(a)}) AND NOT EXISTS (SELECT 1 FROM KhachHang k WHERE k.MaDangNhap = TaiKhoan.MaDangNhap);
COMMIT;
SELECT (SELECT COUNT(*) FROM PhieuDatPhong WHERE MaDatPhong IN ({lst(b)})) AS PhieuConLai, (SELECT COUNT(*) FROM KhachHang WHERE MaKhachHang IN ({lst(c)})) AS KhachConLai;"""
    out = subprocess.run(['sqlcmd', '-S', 'localhost', '-U', 'sa', '-P', '12345', '-d', os.environ.get('QLKSAN_DB', 'Quanlykhachsanc#'), '-W', '-h', '-1', '-Q', sql], capture_output=True, text=True)
    print(f"\n[Dọn dẹp] Xoá {len(b)} phiếu {sorted(b)}, {len(c)} khách {sorted(c)}, {len(a)} tài khoản {sorted(a)} → còn lại (phiếu, khách): {out.stdout.split()}{out.stderr.strip()}")

try:
    ADMIN = api_login('admin1')['AccessToken']
    LETAN = api_login('letan1')['AccessToken']
    K1 = api_login('khach1')['AccessToken']
    room = lambda ma: api('GET', f'/api/rooms/{ma}', ADMIN)[1]
    booking = lambda ma: api('GET', f'/api/bookings/{ma}', ADMIN)[1]
    free = api('GET', f'/api/rooms/available?checkIn={d(0)}&checkOut={d(25)}', ADMIN)[1]
    assert len(free) >= 2, 'Cần ít nhất 2 phòng trống suốt 25 ngày tới'
    R1, R2 = free[0], free[1]
    G1, G2 = R1['LoaiPhong']['GiaThueNgay'], R2['LoaiPhong']['GiaThueNgay']
    svc = next(s for s in api('GET', '/api/services', ADMIN)[1] if s['TrangThai'] == 'Available')
    # Giá theo mùa / cuối tuần: tiền phòng kỳ vọng lấy từ báo giá từng đêm của API thay vì giá niêm yết
    quote = lambda ma, a, b: api('GET', f'/api/rooms/{ma}/quote?checkIn={d(a)}&checkOut={d(b)}', ADMIN)[1]['TongTien']
    P2_1, P1_1, P1_2 = quote(R2['MaPhong'], 0, 1), quote(R1['MaPhong'], 0, 1), quote(R1['MaPhong'], 0, 2)
    print(f"Phòng test: {R1['MaPhong']} ({G1:,.0f}đ), {R2['MaPhong']} ({G2:,.0f}đ) | Hôm nay {dvn(0)}")

    # Khách test mới
    uname = f'm1test{random.randint(1000, 9999)}'
    kh = web_session()
    web_post(kh, '/Auth/Register', {'ho': 'Test', 'ten': 'Mức Một', 'sdt': '0912000444', 'cccd': str(random.randint(10**11, 10**12 - 1)),
                                   'ngaySinh': '1995-05-05', 'email': 'm1@test.vn', 'diaChi': 'Đà Nẵng', 'username': uname, 'password': '123456', 'confirmPassword': '123456'})
    KH = api_login(uname); created['accounts'].add(KH['MaDangNhap']); created['customers'].add(KH['MaKhachHang'])
    KT = KH['AccessToken']

    print('\n===== 1.2 Phòng trống theo khoảng ngày =====')
    s, body = web(kh, 'GET', f"/Customer/BookRoom?checkIn={d(3)}&checkOut={d(5)}&soNguoi=1")[0::2]
    check('Trang đặt phòng hiển thị hạng còn trống theo khoảng ngày chọn', s == 200 and f'Còn trống từ {dvn(3)} đến {dvn(5)}' in ' '.join(re.sub(r'<[^>]+>', ' ', body).split()))
    s, loc, body = web_post(kh, '/Customer/BookRoom', {'MaPhong': R1['MaPhong'], 'SoNguoi': 1, 'CheckIn': d(3), 'CheckOut': d(5), 'MaKhuyenMai': 'KM03'})
    A1 = track_booking(next((b['MaDatPhong'] for b in api('GET', '/api/customers/me/bookings', KT)[1]), None))
    check('Khách đặt online ngày +3→+5 (kèm KM03) qua Web', A1 and alert(body)[0] == 'success', f"{A1} | {alert(body)[1]}")
    check('  Chuyển tới trang chi tiết phiếu, báo đã xác nhận', loc == f'/Customer/Booking/{A1}' and alert(body) == ('success', 'Đặt phòng thành công — kỳ nghỉ của bạn đã được xác nhận.'), loc)
    check('  Phiếu online còn phòng được xác nhận ngay (Đã duyệt)', booking(A1)['TrangThai'] == 'Approved')
    check('  1.4 Mã KM03 được lưu vào phiếu', booking(A1)['MaKhuyenMai'] == 'KM03')
    check('  Phòng KHÔNG bị chuyển sang "Đã đặt" (nhận phòng sau 3 ngày)', room(R1['MaPhong'])['TrangThai'] == 'Available')
    s, r = book_api(K1, R1['MaPhong'], d(10), d(12)); track_booking(r and r.get('MaDatPhong'))
    check('Khách khác đặt CÙNG phòng ngày +10→+12 (không trùng) → được', s == 201, r and r.get('MaDatPhong'))
    s, r = book_api(K1, R1['MaPhong'], d(4), d(6))
    check('Đặt ngày +4→+6 (trùng phiếu +3→+5) → 409', s == 409 and 'đã có khách đặt' in r['detail'], r.get('detail'))
    s, r = book_api(K1, R1['MaPhong'], d(5), d(7)); A5 = track_booking(r and r.get('MaDatPhong'))
    check('Đặt nối tiếp ngày +5→+7 (ngày trả = ngày nhận) → được', s == 201, A5)
    ids = lambda q: [x['MaPhong'] for x in api('GET', f'/api/rooms/available?{q}', ADMIN)[1]]
    check('API phòng trống +3→+5 không có phòng test', R1['MaPhong'] not in ids(f'checkIn={d(3)}&checkOut={d(5)}'))
    check('API phòng trống +7→+9 có phòng test', R1['MaPhong'] in ids(f'checkIn={d(7)}&checkOut={d(9)}'))
    s, r = book_api(KT, R1['MaPhong'], d(14), d(15), km='KM01')
    check('Đặt kèm mã KM hết hạn → báo lỗi (không âm thầm bỏ mã)', s == 400 and 'Mã khuyến mãi' in r['detail'], r.get('detail'))

    # Hai khách đặt cùng phòng, cùng ngày, cùng lúc
    res = []
    def worker(tok): res.append(book_api(tok, R2['MaPhong'], d(20), d(22)))
    ts = [threading.Thread(target=worker, args=(t,)) for t in (KT, K1)]
    [t.start() for t in ts]; [t.join() for t in ts]
    for st, body in res:
        if st == 201: track_booking(body['MaDatPhong'])
    check('2 khách đặt cùng phòng cùng ngày CÙNG LÚC → chỉ 1 thành công', sorted(x[0] for x in res) == [201, 409], [x[0] for x in res])

    print('\n===== 1.3 Huỷ / từ chối đặt phòng =====')
    s, r = api('POST', f'/api/bookings/{A5}/cancel', KT, {'LyDo': 'thử'})
    check('Khách huỷ phiếu của NGƯỜI KHÁC → 403', s == 403, s)
    k1web = web_login('khach1')
    s, loc, body = web_post(k1web, '/Customer/CancelBooking', {'id': A5, 'lyDo': 'Đổi lịch công tác'})
    b5 = booking(A5)
    check('Khách tự huỷ phiếu trước ngày nhận phòng (qua Web)', alert(body)[0] == 'success' and b5['TrangThai'] == 'Cancelled', alert(body)[1])
    check('  Lý do huỷ được ghi vào phiếu', 'Đổi lịch công tác' in (b5['GhiChu'] or ''), b5['GhiChu'])
    check('  Phòng được giải phóng cho khoảng +5→+7', R1['MaPhong'] in ids(f'checkIn={d(5)}&checkOut={d(7)}'))
    s, loc, body = web_post(kh, '/Customer/MyStays', {}) if False else (None, None, None)
    ltweb = web_login('letan1')
    s, loc, body = web_post(ltweb, '/Reception/CancelBooking', {'id': A1, 'lyDo': ''}) if False else (None, None, None)
    # lễ tân huỷ phiếu đã xác nhận (+10→+12 của khach1; phiếu online được tự xác nhận)
    A3 = next(b for b in created['bookings'] if b and booking(b) and booking(b)['NgayCheckInDuKien'].startswith(d(10)))
    s, loc, body = web_post(ltweb, '/Reception/CancelBooking', {'id': A3, 'lyDo': 'Khách không xác nhận cọc'})
    b3 = booking(A3)
    check('Lễ tân HUỶ phiếu đã xác nhận (qua Web)', alert(body)[0] == 'success' and b3['TrangThai'] == 'Cancelled' and '[Huỷ' in b3['GhiChu'], b3['GhiChu'])
    s, r = api('POST', '/api/bookings/DP04/cancel', LETAN, {'LyDo': 'x'})
    check('Huỷ phiếu đã trả phòng → 409', s == 409, r and r.get('detail'))

    print('\n===== 1.5 + 1.4 Check-in đúng ngày, tính tiền theo ngày thực tế, giữ KM khi đặt =====')
    s, loc, body = web_post(ltweb, '/Reception/CheckInOnlineBooking', {'id': A1})
    check('Check-in trước ngày nhận phòng (+3) → bị chặn', alert(body)[0] == 'danger' and 'Chưa đến ngày nhận phòng' in alert(body)[1], alert(body)[1])
    s, r = book_api(KT, R2['MaPhong'], d(0), d(3), km='KM03'); C1 = track_booking(r and r.get('MaDatPhong'))
    check('Khách đặt phòng nhận HÔM NAY, ở 3 đêm (kèm KM03)', s == 201, C1)
    s, r = api('POST', f'/api/bookings/{C1}/cancel', KT, {'LyDo': 'x'})
    check('Khách tự huỷ vào ngày nhận phòng → bị chặn (phải liên hệ lễ tân)', s == 400 and 'liên hệ lễ tân' in r['detail'], r.get('detail'))
    web_post(ltweb, '/Reception/ApproveBooking', {'id': C1})
    s, loc, body = web_post(ltweb, '/Reception/CheckInOnlineBooking', {'id': C1})
    check('Lễ tân duyệt + check-in đúng ngày', booking(C1)['TrangThai'] == 'CheckedIn' and room(R2['MaPhong'])['TrangThai'] == 'Occupied', alert(body)[1])
    s, r = api('PATCH', f"/api/rooms/{R2['MaPhong']}", LETAN, {'TrangThai': 'Available'})
    check('Đổi tay trạng thái phòng đang có khách → 409', s == 409, r and r.get('detail'))
    s, r = api('PATCH', f"/api/rooms/{R1['MaPhong']}", LETAN, {'TrangThai': 'Booked'})
    check('Đặt tay trạng thái "Đã đặt" → bị chặn', s == 400, r and r.get('detail'))
    web_post(ltweb, '/Service/OrderService', {'maPhong': R2['MaPhong'], 'services[0].MaDichVu': svc['MaDichVu'], 'services[0].SoLuong': 1})
    s, _, body = web(ltweb, 'GET', f"/Checkout/Process?maPhong={R2['MaPhong']}")
    check('Form lập hoá đơn điền sẵn mã KM khách chọn khi đặt', 'value="KM03"' in body)
    s, loc, body = web_post(ltweb, '/Checkout/GenerateInvoice', {'maDatPhong': C1, 'maKhuyenMai': '', 'maPhong': R2['MaPhong']})
    inv = booking(C1)['HoaDon']
    check('Lập hoá đơn: tiền phòng tính 1 đêm THỰC TẾ (đặt 3 đêm, trả trong ngày)', inv['TongTienPhong'] == P2_1 and inv['SoDem'] == 1, f"{inv['TongTienPhong']:,.0f} ({inv['SoDem']} đêm)")
    check('  KM03 tự áp dụng từ phiếu đặt (lễ tân để trống ô mã)', inv['MaKhuyenMai'] == 'KM03' and inv['GiamGiaKhuyenMai'] == round(P2_1 * 0.05), f"giảm {inv['GiamGiaKhuyenMai']:,.0f}")
    check('  Không phụ thu (trả trong ngày nhận phòng)', inv['TienPhat'] == 0)
    expected_total = (P2_1 + svc['GiaDichVu'] - round(P2_1 * 0.05)) * 1.08
    check('  Tổng = (phòng + DV − KM) × 1,08', abs(inv['TongTienCanThanhToan'] - expected_total) < 1, f"{inv['TongTienCanThanhToan']:,.0f}")
    check('  Trang trả phòng hiển thị số đêm thực tế và "CÒN PHẢI THANH TOÁN"', '(1 đêm thực tế)' in body and 'CÒN PHẢI THANH TOÁN' in body)
    s, loc, body = web_post(ltweb, '/Checkout/Pay', {'maHoaDon': inv['MaHoaDon'], 'phuongThuc': 'Tiền mặt'})
    b = booking(C1)
    check('Thanh toán → đúng số tiền, phòng chuyển Đang dọn', b['HoaDon']['TrangThai'] == 'Paid' and abs(b['HoaDon']['ThanhToans'][0]['SoTienDaTra'] - round(expected_total)) <= 1
          and room(R2['MaPhong'])['TrangThai'] == 'Cleaning', f"{b['HoaDon']['ThanhToans'][0]['SoTienDaTra']:,.0f}")
    web_post(ltweb, '/Housekeeping/MarkClean', {'maPhong': R2['MaPhong']})

    print('\n===== 1.1 Trừ tiền cọc khách vãng lai =====')
    s, _, body = web(ltweb, 'POST', '/Reception/WalkIn', {'Ho': 'Test', 'Ten': 'Vãng Lai', 'SDT': '0912000555', 'CCCD_Passport': str(random.randint(10**11, 10**12 - 1)),
                                                         'NgayCheckIn': d(0), 'NgayCheckOut': d(4), 'MaPhong': R1['MaPhong'], 'SoNguoi': 1})
    check('Walk-in đến ngày +4 trùng phiếu đặt trước (+3→+5) → bị chặn', s == 200 and 'đã có khách đặt' in body)
    s, _, body = web(ltweb, 'POST', '/Reception/WalkIn', {'Ho': 'Test', 'Ten': 'Vãng Lai', 'SDT': '0912000555', 'CCCD_Passport': str(random.randint(10**11, 10**12 - 1)),
                                                         'NgayCheckIn': d(1), 'NgayCheckOut': d(2), 'MaPhong': R1['MaPhong'], 'SoNguoi': 1})
    check('Walk-in ngày nhận phòng không phải hôm nay → bị chặn', s == 200 and 'phải nhận phòng trong hôm nay' in body)
    cccd = str(random.randint(10**11, 10**12 - 1))
    s, loc, body = web_post(ltweb, '/Reception/WalkIn', {'Ho': 'Test', 'Ten': 'Vãng Lai', 'SDT': '0912000555', 'CCCD_Passport': cccd,
                                                        'NgayCheckIn': d(0), 'NgayCheckOut': d(2), 'MaPhong': R1['MaPhong'], 'SoNguoi': 1})
    D1 = next((x['MaDatPhong'] for x in api('GET', '/api/bookings/active', ADMIN)[1] if x['ChiTietDatPhongs'][0]['MaPhong'] == R1['MaPhong']), None)
    track_booking(D1)
    bd = booking(D1)
    if bd: created['customers'].add(bd['MaKhachHang'])
    check('Walk-in hôm nay → +2 (trước phiếu đặt +3) → được', bd and bd['TrangThai'] == 'CheckedIn', f"{D1}, mã khách {bd and bd['MaKhachHang']}")
    check('  Tiền cọc = 50% × tiền 2 đêm (theo giá từng đêm)', bd['TienCoc'] == round(P1_2 * 0.5), f"{bd['TienCoc']:,.0f}")
    web_post(ltweb, '/Checkout/GenerateInvoice', {'maDatPhong': D1, 'maKhuyenMai': '', 'maPhong': R1['MaPhong']})
    s, _, body = web(ltweb, 'GET', f"/Checkout/Process?maPhong={R1['MaPhong']}")
    inv = booking(D1)['HoaDon']
    coc = round(P1_2 * 0.5)
    con_lai = P1_1 * 1.08 - coc
    check('Hoá đơn ghi nhận tiền cọc', inv['TienCoc'] == coc)
    check('  Còn phải thu = tổng − cọc (âm = hoàn lại khách)', abs(inv['SoTienConLai'] - con_lai) < 1, f"tổng {inv['TongTienCanThanhToan']:,.0f} − cọc {inv['TienCoc']:,.0f} = {inv['SoTienConLai']:,.0f}")
    check('  Trang trả phòng hiển thị đúng số còn phải thu', f"{abs(inv['SoTienConLai']):,.0f}" in body)
    web_post(ltweb, '/Checkout/Pay', {'maHoaDon': inv['MaHoaDon'], 'phuongThuc': 'Chuyển khoản'})
    tt = booking(D1)['HoaDon']['ThanhToans'][0]
    check('Thanh toán chỉ thu phần còn lại, ghi chú đã trừ cọc', abs(tt['SoTienDaTra'] - max(0, round(con_lai))) <= 1 and 'Đã trừ tiền cọc' in (tt['GhiChu'] or ''),
          f"{tt['SoTienDaTra']:,.0f} | {tt['GhiChu']}")
    web_post(ltweb, '/Housekeeping/MarkClean', {'maPhong': R1['MaPhong']})
    check('Phòng test về trạng thái Trống', room(R1['MaPhong'])['TrangThai'] == 'Available' and room(R2['MaPhong'])['TrangThai'] == 'Available')

    print('\n===== 1.6 Mã tự sinh tăng dần =====')
    ma = sorted(b for b in created['bookings'] if b and b.startswith('PDP'))
    nums = [int(x[3:]) for x in ma]
    check('Mã phiếu online tăng dần liên tiếp, không ngẫu nhiên', nums == list(range(nums[0], nums[0] + len(nums))) if nums else False, ma)
    check('Mã khách vãng lai mới không còn dạng KH + 2 chữ số', bd and len(bd['MaKhachHang']) > 4, bd and bd['MaKhachHang'])
finally:
    cleanup()

print(f"\n==> {sum(results)}/{len(results)} kiểm tra đạt")
sys.exit(0 if all(results) else 1)
