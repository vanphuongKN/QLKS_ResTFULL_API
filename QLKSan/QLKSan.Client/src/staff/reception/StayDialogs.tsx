import { useEffect, useState, type FormEvent } from 'react';
import { bookingApi, PHUONG_THUC_QUAY, type Companion, type FrontDeskStay, type StayQuote } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { Modal } from '../../components/ui/Dialog';
import { homNay, ngay, so, tien } from '../../utils/format';

// Hộp thoại thao tác trên một phiếu tại quầy (cùng nội dung và câu thông báo với các modal của Reception/Index MVC)
export type LoaiHop = 'nhanPhong' | 'doiNgay' | 'diCung' | 'donPhong' | 'thuCoc' | 'huy';
interface HopProps { k: FrontDeskStay; dangO: boolean; onDong: () => void }

const tieuDePhieu = (k: FrontDeskStay) => `#${k.MaDatPhong} · ${k.HoTenKhachHang}${k.SoPhong ? ` · phòng ${k.SoPhong}` : ''}`;
const LAM_MOI = [['front-desk'], ['rooms'], ['hk']];

// Danh sách người đi cùng: họ tên bắt buộc, CCCD / hộ chiếu, quốc tịch
export function NguoiDiCung({ ds, onDoi }: { ds: Companion[]; onDoi: (ds: Companion[]) => void }) {
  const sua = (i: number, k: keyof Companion, v: string) => onDoi(ds.map((x, j) => (j === i ? { ...x, [k]: v } : x)));
  return (
    <div className="nguoi-di-cung">
      {ds.map((x, i) => (
        <div key={i} className="ndc-dong">
          <input value={x.HoTen ?? ''} onChange={(e) => sua(i, 'HoTen', e.target.value)} placeholder="Họ tên" maxLength={100} required aria-label={`Họ tên người đi cùng ${i + 1}`} />
          <input value={x.CCCD_Passport ?? ''} onChange={(e) => sua(i, 'CCCD_Passport', e.target.value)} placeholder="CCCD / hộ chiếu" maxLength={20} aria-label={`CCCD / hộ chiếu người đi cùng ${i + 1}`} />
          <input value={x.QuocTich ?? ''} onChange={(e) => sua(i, 'QuocTich', e.target.value)} placeholder="Quốc tịch" maxLength={50} aria-label={`Quốc tịch người đi cùng ${i + 1}`} />
          <button type="button" className="btn btn-outline-danger btn-sm" aria-label={`Xoá người đi cùng ${i + 1}`} onClick={() => onDoi(ds.filter((_, j) => j !== i))}>×</button>
        </div>
      ))}
      <button type="button" className="link-btn small" onClick={() => onDoi([...ds, { HoTen: '', CCCD_Passport: '', QuocTich: '' }])}>+ Thêm người</button>
    </div>
  );
}
// Bỏ dòng trống họ tên, trim, chuỗi rỗng → null (như ReceptionController.Companions của MVC)
export const chuanHoaDiCung = (ds: Companion[]) => ds.filter((x) => x.HoTen?.trim()).map((x) => ({
  HoTen: x.HoTen!.trim(), CCCD_Passport: x.CCCD_Passport?.trim() || null, QuocTich: x.QuocTich?.trim() || null
}));
const diCungCua = (k: FrontDeskStay): Companion[] => (k.KhachDiCung ?? []).map((x) => ({ HoTen: x.HoTen ?? '', CCCD_Passport: x.CCCD_Passport ?? '', QuocTich: x.QuocTich ?? '' }));

function HopNhanPhong({ k, onDong }: HopProps) {
  const [soKhach, setSoKhach] = useState(k.SoKhach ?? 1);
  const [coc, setCoc] = useState(0);
  const [pt, setPt] = useState(PHUONG_THUC_QUAY[0]);
  const [ds, setDs] = useState(diCungCua(k));
  const nhan = useThaoTac({
    mutationFn: () => {
      const diCung = chuanHoaDiCung(ds);
      return bookingApi.checkIn(k.MaDatPhong!, { SoLuongKhach: soKhach, TienCoc: coc, PhuongThucCoc: pt, KhachDiCung: diCung.length ? diCung : null });
    },
    lamMoi: LAM_MOI, thanhCong: 'Khách đã nhận phòng thành công.', tienToLoi: 'Không thể check-in cho đơn này. ', onSuccess: onDong
  });
  return (
    <Modal open title="Nhận phòng" onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); nhan.mutate(); }}>
        <p>{tieuDePhieu(k)}</p>
        <div className="form-grid">
          <label className="field"><span>Số khách</span><input type="number" min={1} max={10} value={soKhach} onChange={(e) => setSoKhach(e.target.valueAsNumber || 1)} /></label>
          <label className="field"><span>Thu thêm cọc / đảm bảo</span>
            <span className="o-ghep"><input type="number" min={0} step={1000} value={coc} onChange={(e) => setCoc(e.target.valueAsNumber || 0)} aria-label="Số tiền cọc thêm" />
              <select value={pt} onChange={(e) => setPt(e.target.value)} aria-label="Phương thức">{PHUONG_THUC_QUAY.map((x) => <option key={x}>{x}</option>)}</select></span>
          </label>
        </div>
        <small className="muted">{(k.TienCoc ?? 0) > 0 ? `Đã thu cọc ${tien(k.TienCoc)}.` : 'Phiếu chưa có tiền cọc.'}</small>
        <fieldset className="ndc-khung"><legend>Người đi cùng</legend><NguoiDiCung ds={ds} onDoi={setDs} />
          <small className="muted">Họ tên bắt buộc; CCCD / hộ chiếu và quốc tịch nếu có. Dùng cho sổ khai báo lưu trú.</small></fieldset>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Đóng</button><button type="submit" className="btn btn-gold" disabled={nhan.isPending}>Xác nhận nhận phòng</button></div>
      </form>
    </Modal>
  );
}

function HopDoiNgay({ k, dangO, onDong }: HopProps) {
  const [nhan, setNhan] = useState((k.NgayNhan ?? '').slice(0, 10));
  const [tra, setTra] = useState((k.NgayTra ?? '').slice(0, 10));
  const [bg, setBg] = useState<{ q?: StayQuote; loi?: string } | null>(null);
  // Báo giá chênh lệch mỗi khi đổi ngày (đang ở chỉ đổi ngày trả); phòng kín lịch thì không cho lưu
  useEffect(() => {
    if (!tra || (tra === (k.NgayTra ?? '').slice(0, 10) && (dangO || nhan === (k.NgayNhan ?? '').slice(0, 10)))) { setBg(null); return; }
    let huy = false;
    bookingApi.quoteStay(k.MaDatPhong!, tra, dangO ? null : nhan).then((q) => !huy && setBg({ q }), (e: Error) => !huy && setBg({ loi: e.message }));
    return () => { huy = true; };
  }, [nhan, tra, dangO, k]);
  const luu = useThaoTac({
    mutationFn: () => bookingApi.changeStay(k.MaDatPhong!, tra, dangO ? null : nhan), lamMoi: LAM_MOI, tienToLoi: 'Không thể đổi ngày lưu trú. ',
    thanhCong: (b) => `Phiếu ${k.MaDatPhong}: lưu trú ${ngay(b.NgayCheckInDuKien).slice(0, 5)} → ${ngay(b.NgayCheckOutDuKien).slice(0, 5)}.`, onSuccess: onDong
  });
  const khongLuuDuoc = !!bg?.loi || (bg?.q && !bg.q.ConPhong);
  const cl = bg?.q?.ChenhLech ?? 0;
  return (
    <Modal open title={dangO ? 'Gia hạn / rút ngắn kỳ nghỉ' : 'Đổi ngày lưu trú'} onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e: FormEvent) => { e.preventDefault(); luu.mutate(); }}>
        <p>{tieuDePhieu(k)}</p>
        <div className="form-grid">
          {!dangO && <label className="field"><span>Ngày nhận</span><input type="date" min={homNay()} value={nhan} onChange={(e) => setNhan(e.target.value)} /></label>}
          <label className="field"><span>Ngày trả mới</span><input type="date" min={homNay()} value={tra} onChange={(e) => setTra(e.target.value)} required /></label>
        </div>
        {bg && (
          <div className={`bao-gia-ngay${khongLuuDuoc ? ' bao-gia-loi' : ''}`} role="status">
            {bg.loi ? bg.loi : (
              <>
                <div><span>Hiện tại</span><strong>{bg.q!.SoDemCu} đêm · {tien(bg.q!.TienPhongCu)}</strong></div>
                <div><span>Sau thay đổi</span><strong>{bg.q!.SoDemMoi} đêm · {tien(bg.q!.TienPhongMoi)}</strong></div>
                <div><span>Chênh lệch tiền phòng</span><strong>{cl > 0 ? '+' : ''}{tien(cl)}</strong></div>
                {!bg.q!.ConPhong && <p className="chu-loi small">{bg.q!.LyDo}</p>}
              </>
            )}
          </div>
        )}
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Đóng</button><button type="submit" className="btn btn-gold" disabled={luu.isPending || khongLuuDuoc}>Lưu thay đổi</button></div>
      </form>
    </Modal>
  );
}

function HopDiCung({ k, onDong }: HopProps) {
  const [ds, setDs] = useState(diCungCua(k));
  const luu = useThaoTac({
    mutationFn: () => bookingApi.companions(k.MaDatPhong!, chuanHoaDiCung(ds)), lamMoi: LAM_MOI, tienToLoi: 'Không thể lưu người đi cùng. ',
    thanhCong: () => `Đã cập nhật ${chuanHoaDiCung(ds).length} người đi cùng cho phiếu ${k.MaDatPhong}.`, onSuccess: onDong
  });
  return (
    <Modal open rong title="Người đi cùng" onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }}>
        <p>{tieuDePhieu(k)}</p>
        <NguoiDiCung ds={ds} onDoi={setDs} />
        <small className="muted">Danh sách được đưa vào sổ khách lưu trú. Số khách của phiếu tự tăng theo nếu cần (không vượt sức chứa phòng).</small>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Đóng</button><button type="submit" className="btn btn-gold" disabled={luu.isPending}>Lưu danh sách</button></div>
      </form>
    </Modal>
  );
}

function HopDonPhong({ k, onDong }: HopProps) {
  const [f, setF] = useState({ KhongLamPhienHomNay: !!k.KhongLamPhien, TuChoiDonHangNgay: !!k.TuChoiDonHangNgay, GioDonMongMuon: k.GioDonMongMuon ?? '' });
  const luu = useThaoTac({
    mutationFn: () => bookingApi.hkPrefs(k.MaDatPhong!, { ...f, GioDonMongMuon: f.GioDonMongMuon.trim() || null }), lamMoi: LAM_MOI,
    tienToLoi: 'Không lưu được dặn dò buồng phòng. ', onSuccess: onDong,
    thanhCong: (r) => `Đã lưu dặn dò buồng phòng cho phiếu ${k.MaDatPhong}` + (r.KhongLamPhienHomNay ? ' — không làm phiền hôm nay.' : '.')
  });
  return (
    <Modal open title="Dặn dò buồng phòng" onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }}>
        <p>{tieuDePhieu(k)}</p>
        <label className="check"><input type="checkbox" checked={f.KhongLamPhienHomNay} onChange={(e) => setF({ ...f, KhongLamPhienHomNay: e.target.checked })} /> Không làm phiền hôm nay (bỏ qua dọn phòng, chỉnh trang)</label>
        <label className="check"><input type="checkbox" checked={f.TuChoiDonHangNgay} onChange={(e) => setF({ ...f, TuChoiDonHangNgay: e.target.checked })} /> Khách không cần dọn hằng ngày trong kỳ nghỉ</label>
        <label className="field"><span>Giờ dọn khách muốn</span><input value={f.GioDonMongMuon} maxLength={30} placeholder="VD: Sau 14:00" onChange={(e) => setF({ ...f, GioDonMongMuon: e.target.value })} /></label>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Đóng</button><button type="submit" className="btn btn-gold" disabled={luu.isPending}>Lưu</button></div>
      </form>
    </Modal>
  );
}

function HopThuCoc({ k, onDong }: HopProps) {
  const [soTien, setSoTien] = useState<number | ''>('');
  const [pt, setPt] = useState(PHUONG_THUC_QUAY[0]);
  const [ghiChu, setGhiChu] = useState('');
  const thu = useThaoTac({
    mutationFn: () => bookingApi.deposit(k.MaDatPhong!, Number(soTien), pt, ghiChu.trim() || null), lamMoi: LAM_MOI,
    thanhCong: () => `Đã thu cọc ${so(Number(soTien))}đ cho phiếu ${k.MaDatPhong}.`, tienToLoi: 'Không thể thu cọc. ', onSuccess: onDong
  });
  return (
    <Modal open title="Thu tiền cọc" onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); thu.mutate(); }}>
        <p>{tieuDePhieu(k)}</p>
        <div className="form-grid">
          <label className="field"><span>Số tiền</span><input type="number" min={1000} step={1000} required value={soTien} onChange={(e) => setSoTien(e.target.value === '' ? '' : e.target.valueAsNumber)} /></label>
          <label className="field"><span>Phương thức</span><select value={pt} onChange={(e) => setPt(e.target.value)}>{PHUONG_THUC_QUAY.map((x) => <option key={x}>{x}</option>)}</select></label>
          <label className="field span-2"><span>Ghi chú</span><input value={ghiChu} maxLength={255} placeholder="VD: Đảm bảo phát sinh minibar" onChange={(e) => setGhiChu(e.target.value)} /></label>
        </div>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Đóng</button><button type="submit" className="btn btn-gold" disabled={thu.isPending}>Thu cọc</button></div>
      </form>
    </Modal>
  );
}

function HopHuy({ k, onDong }: HopProps) {
  const tuChoi = k.TrangThai === 'Pending';
  const [lyDo, setLyDo] = useState('');
  const [hoanCoc, setHoanCoc] = useState(false);
  const huy = useThaoTac({
    mutationFn: () => bookingApi.cancel(k.MaDatPhong!, lyDo.trim(), hoanCoc), lamMoi: LAM_MOI,
    thanhCong: `Đã huỷ/từ chối phiếu đặt phòng ${k.MaDatPhong}.`, tienToLoi: 'Không thể huỷ phiếu. ', onSuccess: onDong
  });
  return (
    <Modal open title={tuChoi ? 'Từ chối phiếu đặt phòng' : 'Huỷ phiếu đặt phòng'} onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); huy.mutate(); }}>
        <p>{tieuDePhieu(k)}</p>
        <label className="field"><span>Lý do</span><textarea rows={2} maxLength={200} required value={lyDo} onChange={(e) => setLyDo(e.target.value)} /></label>
        {(k.TienCoc ?? 0) > 0 && (
          <div>
            <label className="check"><input type="checkbox" checked={hoanCoc} onChange={(e) => setHoanCoc(e.target.checked)} /> Hoàn lại tiền cọc {tien(k.TienCoc)}</label>
            <small className="muted">Không chọn: giữ cọc theo chính sách huỷ (ghi chú vào phiếu).</small>
          </div>
        )}
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Đóng</button><button type="submit" className="btn btn-danger" disabled={huy.isPending}>Xác nhận</button></div>
      </form>
    </Modal>
  );
}

export function HopPhieu({ loai, ...p }: HopProps & { loai: LoaiHop }) {
  switch (loai) {
    case 'nhanPhong': return <HopNhanPhong {...p} />;
    case 'doiNgay': return <HopDoiNgay {...p} />;
    case 'diCung': return <HopDiCung {...p} />;
    case 'donPhong': return <HopDonPhong {...p} />;
    case 'thuCoc': return <HopThuCoc {...p} />;
    case 'huy': return <HopHuy {...p} />;
  }
}
