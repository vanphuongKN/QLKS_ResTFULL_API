import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { addDays, homNay } from '../utils/format';

// Tìm phòng trống: ngày nhận, ngày trả, số khách → /tim-phong?checkIn&checkOut&soNguoi
export function SearchForm({ checkIn, checkOut, soNguoi, compact }: { checkIn?: string; checkOut?: string; soNguoi?: number; compact?: boolean }) {
  const navigate = useNavigate();
  const [ci, setCi] = useState(checkIn ?? homNay());
  const [co, setCo] = useState(checkOut ?? addDays(checkIn ?? homNay(), 1));
  const [n, setN] = useState(soNguoi ?? 2);
  const [loi, setLoi] = useState('');

  function submit(e: FormEvent) {
    e.preventDefault();
    if (ci < homNay()) return setLoi('Ngày nhận phòng không được ở quá khứ.');
    if (co <= ci) return setLoi('Ngày trả phòng phải sau ngày nhận phòng.');
    setLoi('');
    navigate(`/tim-phong?checkIn=${ci}&checkOut=${co}&soNguoi=${n}`);
  }

  return (
    <form className={`search-form${compact ? ' compact' : ''}`} onSubmit={submit} aria-label="Tìm phòng trống">
      <label>
        <span>Nhận phòng</span>
        <input type="date" value={ci} min={homNay()} required
          onChange={(e) => { setCi(e.target.value); if (co <= e.target.value) setCo(addDays(e.target.value, 1)); }} />
      </label>
      <label>
        <span>Trả phòng</span>
        <input type="date" value={co} min={addDays(ci, 1)} required onChange={(e) => setCo(e.target.value)} />
      </label>
      <label>
        <span>Số khách</span>
        <select value={n} onChange={(e) => setN(Number(e.target.value))}>
          {[1, 2, 3, 4, 5, 6].map((x) => <option key={x} value={x}>{x} khách</option>)}
        </select>
      </label>
      <button type="submit" className="btn btn-gold">Tìm phòng</button>
      {loi && <p className="form-error" role="alert">{loi}</p>}
    </form>
  );
}
