import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { useQueries, useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { Loading } from '../components/common';
import { tenHangPhong, tien } from '../utils/format';

// Trang giới thiệu (Câu chuyện, Ẩm thực, Spa, Thư viện, Đặc quyền): cùng nội dung với Views/Home của MVC,
// thông tin khách sạn / chương trình thành viên lấy từ GET /api/hotel

const useHotel = () => useQuery({ queryKey: ['hotel'], queryFn: api.hotel, staleTime: 5 * 60_000 });

function HeroTrang({ anh, eyebrow, children, mo }: { anh: string; eyebrow: string; children: ReactNode; mo?: ReactNode }) {
  return (
    <section className="hero hero-trang" style={{ backgroundImage: `linear-gradient(rgba(20,26,36,.5), rgba(20,26,36,.72)), url(${anh})` }}>
      <div className="container hero-inner">
        <p className="eyebrow">{eyebrow}</p>
        {children}
        {mo}
      </div>
    </section>
  );
}

function GioiThieu({ eyebrow, tieuDe, anh, alt, dao, children }: { eyebrow: string; tieuDe: string; anh: string; alt: string; dao?: boolean; children: ReactNode }) {
  return (
    <section className="section container">
      <div className={`gioi-thieu${dao ? ' gioi-thieu-dao' : ''}`}>
        <div>
          <p className="eyebrow">{eyebrow}</p>
          <h2>{tieuDe}</h2>
          {children}
        </div>
        <img src={anh} alt={alt} />
      </div>
    </section>
  );
}

function BaCot({ eyebrow, tieuDe, cot }: { eyebrow?: string; tieuDe?: string; cot: [string, string][] }) {
  return (
    <section className="section container">
      {tieuDe && <div className="section-head"><p className="eyebrow">{eyebrow}</p><h2>{tieuDe}</h2></div>}
      <div className="ba-cot">
        {cot.map(([t, n], i) => (
          <div key={t} className="cot">
            <span className="cot-so">{String(i + 1).padStart(2, '0')}</span>
            <h3>{t}</h3>
            <p>{n}</p>
          </div>
        ))}
      </div>
    </section>
  );
}

function KeuGoi({ eyebrow, tieuDe, children }: { eyebrow: string; tieuDe: string; children?: ReactNode }) {
  return (
    <section className="section section-sand keu-goi">
      <div className="container">
        <p className="eyebrow">{eyebrow}</p>
        <h2>{tieuDe}</h2>
        {children}
        <Link to="/tim-phong" className="btn btn-gold">Đặt phòng</Link>
      </div>
    </section>
  );
}

const telHref = (s: string) => `tel:${s.replace(/\s/g, '')}`;

export function AboutPage() {
  const h = useHotel().data;
  const ten = h?.Ten ?? 'LUXE RESERVE';
  return (
    <>
      <HeroTrang anh="/images/hero.png" eyebrow="Câu chuyện của chúng tôi">
        <h1>Hiếu khách là <em>một nghệ thuật</em></h1>
        <p className="hero-sub">{h?.MoTaNgan || 'Một chốn nghỉ riêng tư, nơi mỗi chi tiết đều được chăm chút.'}</p>
      </HeroTrang>
      <GioiThieu eyebrow={ten} tieuDe="Nơi kỳ nghỉ bắt đầu từ sự thong thả" anh="/images/room_ocean.png" alt={`Phòng nghỉ tại ${ten}`}>
        <p>Chúng tôi tin rằng sự sang trọng không nằm ở những điều phô trương, mà ở cảm giác được chăm sóc một cách tự nhiên: căn phòng đã sẵn sàng trước giờ bạn đến, lời nhắn được ghi nhớ, và không ai phải hỏi lại điều bạn đã dặn.</p>
        <p>Mỗi hạng phòng được thiết kế cho một nhịp nghỉ riêng — từ những đêm ngắn cho chuyến công tác đến kỳ nghỉ dài ngày cùng gia đình — với ánh sáng tự nhiên, chất liệu ấm và tầm nhìn mở ra khung cảnh xung quanh.</p>
        <Link to="/#hang-phong">Khám phá hạng phòng</Link>
      </GioiThieu>
      <BaCot eyebrow="Điều chúng tôi theo đuổi" tieuDe="Ba cam kết với mỗi vị khách" cot={[
        ['Minh bạch', 'Giá từng đêm hiển thị rõ trước khi đặt, hoá đơn ghi đủ từng khoản — không có phụ phí bất ngờ khi trả phòng.'],
        ['Cá nhân hoá', 'Yêu cầu riêng bạn gửi khi đặt phòng được chuyển thẳng đến lễ tân, để mọi thứ sẵn sàng trước khi bạn đến.'],
        ['Gắn bó', 'Mỗi kỳ nghỉ đều được ghi nhận bằng điểm thành viên — càng quay lại, đặc quyền càng nhiều.']
      ]} />
      <KeuGoi eyebrow="Ghé thăm chúng tôi" tieuDe="Chúng tôi luôn sẵn sàng đón bạn">
        <p>{h?.DiaChi ?? ''}{h?.Hotline ? ` · ${h.Hotline}` : ''}</p>
      </KeuGoi>
    </>
  );
}

export function DiningPage() {
  const h = useHotel().data;
  const ten = h?.Ten ?? 'LUXE RESERVE';
  return (
    <>
      <HeroTrang anh="/images/room_ocean.png" eyebrow="Ẩm thực">
        <h1>Hương vị của <em>những buổi sớm</em></h1>
        <p className="hero-sub">Từ bữa sáng thong thả đến bữa tối riêng tư — ẩm thực là một phần của kỳ nghỉ, không chỉ là bữa ăn.</p>
      </HeroTrang>
      <GioiThieu eyebrow="Bữa sáng" tieuDe="Bắt đầu ngày mới thật chậm" anh="/images/hero.png" alt={`Không gian ẩm thực tại ${ten}`}>
        <p>Bữa sáng tự chọn với nguyên liệu tươi theo mùa, bánh nướng trong ngày và những món địa phương quen thuộc, phục vụ trong không gian yên tĩnh nhìn ra khung cảnh xung quanh.</p>
        <p>Bữa sáng có thể được thêm vào kỳ nghỉ của bạn qua lễ tân và được ghi vào hoá đơn khi trả phòng.</p>
      </GioiThieu>
      <GioiThieu eyebrow="Phục vụ tại phòng" tieuDe="Riêng tư theo cách của bạn" anh="/images/room_presidential.png" alt="Phục vụ tại phòng" dao>
        <p>Một bữa tối nhẹ sau chuyến bay dài, hay bữa sáng muộn trên ban công — hãy cho lễ tân biết, chúng tôi sẽ chuẩn bị theo thời gian bạn mong muốn.</p>
        <p>Có yêu cầu về chế độ ăn hoặc dịp đặc biệt? Ghi chú ngay trong phần <strong>Yêu cầu riêng</strong> khi đặt phòng.</p>
      </GioiThieu>
      <KeuGoi eyebrow="Đặt bàn & yêu cầu" tieuDe="Liên hệ lễ tân để sắp xếp">
        {h?.Hotline && <p>Gọi <a href={telHref(h.Hotline)}>{h.Hotline}</a> hoặc nhắn trong phần yêu cầu riêng khi đặt phòng.</p>}
      </KeuGoi>
    </>
  );
}

export function SpaPage() {
  const h = useHotel().data;
  const ten = h?.Ten ?? 'LUXE RESERVE';
  return (
    <>
      <HeroTrang anh="/images/room_garden.png" eyebrow="Spa & thư giãn">
        <h1>Cân bằng lại <em>từ bên trong</em></h1>
        <p className="hero-sub">Liệu trình chăm sóc cơ thể trong không gian tĩnh lặng — khoảng lặng giữa những ngày di chuyển.</p>
      </HeroTrang>
      <GioiThieu eyebrow="Liệu trình" tieuDe="Chậm lại, hít thở, buông bỏ" anh="/images/room_garden.png" alt={`Không gian thư giãn tại ${ten}`}>
        <p>Các liệu trình massage và chăm sóc cơ thể được điều chỉnh theo nhu cầu của từng vị khách — giảm mệt mỏi sau chuyến đi, thư giãn cơ hay đơn giản là một giờ hoàn toàn cho riêng mình.</p>
        <p>Đặt lịch qua lễ tân trong thời gian lưu trú; chi phí được ghi vào hoá đơn phòng.</p>
      </GioiThieu>
      <BaCot cot={[
        ['Trước liệu trình', 'Đến sớm vài phút để thư giãn và trao đổi với chuyên viên về mong muốn của bạn.'],
        ['Trong liệu trình', 'Không gian yên tĩnh, ánh sáng dịu và tinh dầu nhẹ — mọi thứ được chuẩn bị để bạn hoàn toàn thả lỏng.'],
        ['Sau liệu trình', 'Dành thêm thời gian nghỉ ngơi và bổ sung nước; hạn chế hoạt động mạnh trong vài giờ đầu.']
      ]} />
      <KeuGoi eyebrow="Đặt lịch" tieuDe="Dành cho mình một khoảng lặng">
        {h?.Hotline && <p>Liên hệ lễ tân qua <a href={telHref(h.Hotline)}>{h.Hotline}</a> để chọn khung giờ phù hợp.</p>}
      </KeuGoi>
    </>
  );
}

export function MembershipPage() {
  const { session } = useAuth();
  const h = useHotel().data;
  const ct = h?.ThanhVien;
  const hangs = ct?.Hangs ?? [];
  const so = (n?: number) => (n ?? 0).toLocaleString('vi-VN');
  return (
    <>
      <HeroTrang anh="/images/room_presidential.png" eyebrow="Chương trình thành viên" mo={
        session?.vaiTro === 'Customer'
          ? <Link to="/tai-khoan" className="btn btn-outline btn-sang">Xem hạng của tôi</Link>
          : !session ? <Link to="/dang-ky" className="btn btn-outline btn-sang">Trở thành thành viên</Link> : null
      }>
        <h1>Càng quay lại, <em>càng được trân trọng</em></h1>
        <p className="hero-sub">Tích điểm sau mỗi kỳ nghỉ, đổi điểm trừ thẳng vào hoá đơn và nhận ưu đãi theo hạng — tự động, không cần thẻ.</p>
      </HeroTrang>
      <BaCot eyebrow="Cách hoạt động" tieuDe="Tích điểm & đổi điểm" cot={[
        ['Tích điểm', `Nhận 1 điểm cho mỗi ${tien(ct?.SoTienMotDiem)} thanh toán khi trả phòng — tính cả tiền phòng và dịch vụ trong kỳ nghỉ.`],
        ['Đổi điểm', `Mỗi điểm trị giá ${tien(ct?.GiaTriMotDiem)}, đổi từ ${ct?.DiemDoiToiThieu ?? 0} điểm trở lên để trừ vào hoá đơn ngay tại quầy lễ tân.`],
        ['Lên hạng', 'Hạng xét theo tổng điểm đã tích luỹ — đổi điểm không làm giảm hạng. Ưu đãi tiền phòng được áp dụng tự động trên hoá đơn.']
      ]} />
      {hangs.length > 0 && (
        <section className="section container">
          <div className="section-head"><p className="eyebrow">Hạng thành viên</p><h2>Đặc quyền theo từng hạng</h2></div>
          <div className="hang-tv">
            {hangs.map((hg, i) => (
              <div key={hg.Ten} className={`hang${i === hangs.length - 1 ? ' hang-noi-bat' : ''}`}>
                <p className="eyebrow">{hg.DiemToiThieu === 0 ? 'Khi đăng ký' : `Từ ${so(hg.DiemToiThieu)} điểm`}</p>
                <h3>{hg.Ten}</h3>
                <div className="hang-pt">{(hg.PhanTramGiamPhong ?? 0) > 0 ? `−${hg.PhanTramGiamPhong}%` : '—'}<small>{(hg.PhanTramGiamPhong ?? 0) > 0 ? 'tiền phòng' : ''}</small></div>
                <ul>{hg.QuyenLoi?.map((q) => <li key={q}>{q}</li>)}</ul>
              </div>
            ))}
          </div>
          <p className="muted small canh-giua">Các quyền lợi như nâng hạng phòng, nhận phòng sớm hay trả phòng muộn được phục vụ khi còn phòng trống.</p>
        </section>
      )}
      <KeuGoi eyebrow="Bắt đầu tích điểm" tieuDe="Kỳ nghỉ tiếp theo đang chờ">
        <p>Đặt phòng trực tuyến bằng tài khoản thành viên để mọi kỳ nghỉ đều được ghi nhận.</p>
      </KeuGoi>
    </>
  );
}

// Thư viện ảnh: bộ ảnh của từng hạng phòng (giá cao trước; chưa có bộ ảnh thì ảnh đại diện), lọc theo hạng, xem ảnh lớn
export function GalleryPage() {
  const loai = useQuery({ queryKey: ['room-types'], queryFn: api.roomTypes });
  const ds = useMemo(() => [...(loai.data ?? [])].sort((a, b) => (b.GiaThueNgay ?? 0) - (a.GiaThueNgay ?? 0)), [loai.data]);
  const chiTiet = useQueries({ queries: ds.map((l) => ({ queryKey: ['room-type', l.MaLoai], queryFn: () => api.roomType(l.MaLoai!) })) });
  const albums = chiTiet.map((q) => q.data).filter((x) => !!x);
  const anh = useMemo(() => {
    const out: { url: string; chuThich: string; maLoai: string }[] = [];
    for (const a of albums) {
      const urls = (a.HinhAnhs ?? []).map((h) => h.DuongDan).filter((u): u is string => !!u);
      if (!urls.length && a.AnhDaiDien) urls.push(a.AnhDaiDien);
      for (const url of new Set(urls)) out.push({ url, chuThich: tenHangPhong(a.TenLoai), maLoai: a.MaLoai! });
    }
    return out;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [albums.map((a) => a.MaLoai).join()]);
  const [loc, setLoc] = useState('');
  const [lon, setLon] = useState<{ url: string; alt: string } | null>(null);
  const hop = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const d = hop.current;
    if (!d) return;
    if (lon && !d.open) d.showModal();
    if (!lon && d.open) d.close();
  }, [lon]);

  const dangTai = loai.isLoading || chiTiet.some((q) => q.isLoading);
  return (
    <div className="container section">
      <div className="section-head canh-giua">
        <p className="eyebrow">Thư viện ảnh</p>
        <h1>Không gian {albums.length > 0 ? 'của chúng tôi' : ''}</h1>
        <p className="muted">Chọn một ảnh để xem lớn; chọn tên hạng phòng để xem chi tiết và đặt phòng.</p>
      </div>
      <div className="loc-anh" role="group" aria-label="Lọc theo hạng phòng">
        <button type="button" className={loc === '' ? 'active' : ''} aria-pressed={loc === ''} onClick={() => setLoc('')}>Tất cả</button>
        {albums.map((a) => (
          <button key={a.MaLoai} type="button" className={loc === a.MaLoai ? 'active' : ''} aria-pressed={loc === a.MaLoai} onClick={() => setLoc(a.MaLoai!)}>{tenHangPhong(a.TenLoai)}</button>
        ))}
      </div>
      {dangTai ? <Loading /> : !anh.length && <p className="muted canh-giua">Thư viện ảnh đang được cập nhật.</p>}
      <div className="thu-vien">
        {anh.filter((x) => !loc || x.maLoai === loc).map((x) => (
          <figure key={x.maLoai + x.url}>
            <button type="button" className="anh-nut" onClick={() => setLon({ url: x.url, alt: x.chuThich })} aria-label={`Xem lớn ảnh ${x.chuThich}`}>
              <img src={x.url} alt={x.chuThich} loading="lazy" />
            </button>
            <figcaption><Link to={`/hang-phong/${x.maLoai}`}>{x.chuThich}</Link></figcaption>
          </figure>
        ))}
      </div>
      <dialog ref={hop} className="anh-lon" aria-label="Ảnh lớn" onClose={() => setLon(null)} onClick={(e) => { if (e.target === hop.current) setLon(null); }}>
        {lon && <><button type="button" className="anh-lon-dong" aria-label="Đóng" onClick={() => setLon(null)}>×</button><img src={lon.url} alt={lon.alt} /></>}
      </dialog>
    </div>
  );
}
