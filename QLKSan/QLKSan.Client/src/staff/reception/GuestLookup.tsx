import { useEffect, useRef, useState } from 'react';
import { frontDeskApi, type CustomerLookup } from '../../api/staff';

export interface ThongTinKhach { CCCD_Passport: string; Ho: string; Ten: string; SDT: string; Email?: string }

// Tra khách cũ theo CCCD / SĐT (như Views/Reception/_GuestLookup của MVC): điền sẵn họ tên, liên hệ và cảnh báo
// VIP / hạn chế / ghi chú nội bộ. Nhập CCCD đủ số (9–12 chữ số) trong form cũng tự tra.
export function GuestLookup({ khach, onDien }: { khach: ThongTinKhach; onDien: (k: Partial<ThongTinKhach>) => void }) {
  const [q, setQ] = useState('');
  const [kq, setKq] = useState<{ ds?: CustomerLookup[]; thongBao?: string } | null>(null);
  const [hoSo, setHoSo] = useState<{ k: CustomerLookup; tenDaNhap?: string } | null>(null);
  const daTra = useRef<string | null>(null);

  const dung = (k: CustomerLookup, tenDaNhap?: string) => {
    const lh = k.LienHeDiaChis?.[0];
    onDien({ CCCD_Passport: k.CCCD_Passport ?? '', Ho: k.Ho ?? '', Ten: k.Ten ?? '', ...(lh?.SDT ? { SDT: lh.SDT } : {}), ...(lh?.Email ? { Email: lh.Email } : {}) });
    setKq(null);
    setHoSo({ k, tenDaNhap });
  };
  const tra = async (chuoi: string, tuDong: boolean) => {
    const v = chuoi.trim();
    if (v.length < 6) { setKq({ thongBao: 'Nhập tối thiểu 6 chữ số.' }); return; }
    try {
      const ds = await frontDeskApi.lookup(v);
      if (!ds.length) { setKq(tuDong ? null : { thongBao: 'Không có hồ sơ khớp — khách mới, hệ thống sẽ tạo hồ sơ khi lưu.' }); setHoSo(null); return; }
      // Nhập CCCD đủ số và khớp đúng một hồ sơ: điền ngay
      if (tuDong && ds.length === 1) { dung(ds[0], `${khach.Ho.trim()} ${khach.Ten.trim()}`.trim()); return; }
      setKq({ ds });
    } catch (e) {
      setKq({ thongBao: (e as Error).message });
    }
  };
  // Tự tra khi CCCD trong form đủ 9–12 chữ số (một lần cho mỗi số)
  useEffect(() => {
    const v = khach.CCCD_Passport.trim();
    if (/^[0-9]{9,12}$/.test(v) && v !== daTra.current) { daTra.current = v; void tra(v, true); }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [khach.CCCD_Passport]);

  const k = hoSo?.k;
  const tenHoSo = k ? `${k.Ho} ${k.Ten}` : '';
  return (
    <div className="tra-khach">
      <label className="field"><span>Tra khách cũ</span>
        <span className="o-ghep">
          <input type="search" value={q} onChange={(e) => setQ(e.target.value)} placeholder="Nhập đúng số CCCD/Passport hoặc số điện thoại" autoComplete="off"
            onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); void tra(q, false); } }} aria-label="Tra khách cũ" />
          <button type="button" className="btn btn-outline btn-sm" onClick={() => tra(q, false)}>Tra cứu</button>
        </span>
      </label>
      {kq?.thongBao && <small className="muted">{kq.thongBao}</small>}
      {kq?.ds?.map((x) => (
        <div key={x.MaKhachHang} className="tk-muc">
          <div>
            <strong>{x.Ho} {x.Ten}</strong>{x.LaVIP && <span className="the-vip">VIP</span>}{x.HanChe && <span className="the-han-che">Hạn chế</span>}
            <small className="muted">CCCD {x.CCCD_Passport}{x.LienHeDiaChis?.[0]?.SDT ? ` · ${x.LienHeDiaChis[0].SDT}` : ''} · {x.SoLanDatPhong} lần lưu trú</small>
          </div>
          <button type="button" className="btn btn-gold btn-sm" onClick={() => dung(x)}>Dùng hồ sơ này</button>
        </div>
      ))}
      {k && (
        <div className="cham-soc" role="status">
          <div className="cs-dau">
            <strong>{tenHoSo}</strong>{k.LaVIP && <span className="the-vip">VIP</span>}{k.HanChe && <span className="the-han-che">Hạn chế</span>}
            <small className="muted">{k.ThanhVien?.Hang ? `Hạng ${k.ThanhVien.Hang} · ` : ''}{k.SoLanDatPhong} lần lưu trú · {k.MaKhachHang}</small>
          </div>
          {k.HanChe && <p className="gc-do">Khách đang bị hạn chế: {k.LyDoHanChe || 'không ghi lý do'}. Cân nhắc trước khi nhận đặt phòng.</p>}
          {k.SoThich && <p>♥ Sở thích: {k.SoThich}</p>}
          {k.GhiChuNoiBo && <p className="muted">Ghi chú nội bộ: {k.GhiChuNoiBo}</p>}
          {hoSo?.tenDaNhap && hoSo.tenDaNhap.toLowerCase() !== tenHoSo.toLowerCase() &&
            <p className="gc-do">Tên đã nhập "{hoSo.tenDaNhap}" khác hồ sơ. Phiếu sẽ gắn với hồ sơ đang lưu theo CCCD này.</p>}
        </div>
      )}
    </div>
  );
}
