// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Custom validation for FutureDateAttribute
if ($.validator && $.validator.unobtrusive) {
    $.validator.unobtrusive.adapters.addBool('futuredate');
    $.validator.addMethod('futuredate', function (value, element, params) {
        if (!value) return true;
        
        var inputDate = new Date(value);
        var today = new Date();
        today.setHours(0, 0, 0, 0); // Ignore time part
        
        return inputDate >= today;
    });

    // Custom validation for DateGreaterThanAttribute
    $.validator.unobtrusive.adapters.add('dategreaterthan', ['property'], function (options) {
        options.rules['dategreaterthan'] = options.params;
        options.messages['dategreaterthan'] = options.message;
    });

    $.validator.addMethod('dategreaterthan', function (value, element, params) {
        if (!value) return true;

        // Find the element to compare with (assuming it has id matching the property name)
        var propertyName = params.property;
        
        // ASP.NET Core MVC usually prefixes inputs with parent property names, but typically id is generated as #PropertyName
        // This is a simple fallback that checks by id or name
        var compareElement = $('#' + propertyName);
        if (compareElement.length === 0) {
            compareElement = $('[name$="' + propertyName + '"]');
        }

        if (compareElement.length > 0 && compareElement.val()) {
            var inputDate = new Date(value);
            var compareDate = new Date(compareElement.val());
            return inputDate > compareDate;
        }

        return true;
    });
}

// ===== Giao diện nhân viên: lịch tiếng Việt cho ô ngày + tìm kiếm / phân trang cho bảng dài =====
window.lrStaffUi = (function () {
    // Đặt giá trị cho ô (kể cả ô ngày đã gắn lịch) — dùng trong các hộp thoại thêm / sửa
    function setValue(el, v) {
        if (!el) return;
        if (el._flatpickr) el._flatpickr.setDate(v || null, false);
        else el.value = v == null ? '' : v;
    }

    function setMin(el, v) {
        if (!el) return;
        el.min = v;
        if (el._flatpickr) el._flatpickr.set('minDate', v || null);
    }

    // Ô <input type=date>: hiển thị dd/mm/yyyy tiếng Việt, vẫn gửi yyyy-MM-dd lên máy chủ
    function initDates() {
        if (!window.flatpickr) return;
        if (flatpickr.l10ns && flatpickr.l10ns.vn) flatpickr.localize(flatpickr.l10ns.vn);
        document.querySelectorAll('input[type=date]').forEach(function (el) {
            if (el._flatpickr) return;
            var style = el.getAttribute('style');
            flatpickr(el, {
                altInput: true, altInputClass: el.className, altFormat: 'd/m/Y', dateFormat: 'Y-m-d', allowInput: true, disableMobile: true,
                minDate: el.min || null, maxDate: el.max || null,
                onReady: function (_, __, fp) { if (style) fp.altInput.setAttribute('style', style); if (el.required) fp.altInput.required = true; }
            });
        });
    }

    function normalize(s) {
        return (s || '').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd');
    }

    // Bảng nhiều dòng (> 12): thêm ô tìm kiếm (không dấu) và phân trang 15 dòng ngay trên bảng
    function initTables() {
        var PAGE = 15;
        document.querySelectorAll('.page-content table').forEach(function (table) {
            if (table.hasAttribute('data-no-tools') || !table.tBodies.length) return;
            var rows = Array.prototype.slice.call(table.tBodies[0].rows).filter(function (r) { return r.cells.length > 1; });
            if (rows.length <= 12) return;
            var bar = document.createElement('div');
            bar.className = 'table-tools';
            bar.innerHTML = '<input type="search" class="form-control form-control-sm" placeholder="Tìm trong bảng…" aria-label="Tìm trong bảng">'
                + '<div class="tt-pager"><span class="tt-info"></span><button type="button" data-p="-1" aria-label="Trang trước">‹</button><button type="button" data-p="1" aria-label="Trang sau">›</button></div>';
            var holder = table.closest('.table-responsive') || table;
            holder.parentNode.insertBefore(bar, holder);
            var input = bar.querySelector('input'), info = bar.querySelector('.tt-info'), page = 0;
            var text = rows.map(function (r) { return normalize(r.textContent); });
            function render() {
                var q = normalize(input.value.trim());
                var match = rows.filter(function (r, i) { return !q || text[i].indexOf(q) >= 0; });
                var pages = Math.max(1, Math.ceil(match.length / PAGE));
                page = Math.min(Math.max(page, 0), pages - 1);
                rows.forEach(function (r) { r.style.display = 'none'; });
                match.slice(page * PAGE, page * PAGE + PAGE).forEach(function (r) { r.style.display = ''; });
                info.textContent = match.length + ' dòng · trang ' + (page + 1) + '/' + pages;
                bar.querySelector('[data-p="-1"]').disabled = page === 0;
                bar.querySelector('[data-p="1"]').disabled = page >= pages - 1;
            }
            input.addEventListener('input', function () { page = 0; render(); });
            bar.querySelectorAll('[data-p]').forEach(function (b) {
                b.addEventListener('click', function () { page += parseInt(b.getAttribute('data-p')); render(); });
            });
            render();
        });
    }

    return { init: function () { initDates(); initTables(); }, setValue: setValue, setMin: setMin };
})();
