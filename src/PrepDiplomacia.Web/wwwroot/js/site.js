/* ============================================================
   Prep Diplomacia — JavaScript del sitio
   - Toggle del menú mobile
   - Submit AJAX del newsletter del footer
   - Auto-hide de toasts
   ============================================================ */
(function () {
    'use strict';

    /* ── Mobile nav ─────────────────────────────────────────── */
    var toggle = document.querySelector('.pd-nav__toggle');
    var nav    = document.querySelector('.pd-nav');
    if (toggle && nav) {
        toggle.addEventListener('click', function () {
            var open = nav.classList.toggle('open');
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
        });
    }

    /* ── Calendly (popup) ───────────────────────────────────── */
    // Cualquier elemento con [data-calendly] abre el popup de Calendly.
    // Si el widget todavía no cargó, cae al href en una pestaña nueva.
    document.addEventListener('click', function (e) {
        var el = e.target.closest('[data-calendly]');
        if (!el) return;
        var url = el.getAttribute('data-calendly');
        if (!url) return;
        if (window.Calendly && typeof window.Calendly.initPopupWidget === 'function') {
            e.preventDefault();
            window.Calendly.initPopupWidget({ url: url });
        }
        // Si Calendly no está disponible, no hacemos preventDefault:
        // el navegador sigue el href (target="_blank") como respaldo.
    });

    /* ── Newsletter del footer (AJAX) ──────────────────────── */
    var nlForm = document.getElementById('pd-nl-form');
    var nlMsg  = document.getElementById('pd-nl-msg');
    if (nlForm && nlMsg) {
        nlForm.addEventListener('submit', function (e) {
            e.preventDefault();
            var data = new FormData(nlForm);

            fetch(nlForm.action, {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: data
            })
            .then(function (r) { return r.json(); })
            .then(function (json) {
                nlMsg.textContent  = json.mensaje || (json.ok ? '¡Listo!' : 'Hubo un error.');
                nlMsg.style.color  = json.ok ? 'var(--dorado-cl)' : '#ffb4b4';
                if (json.ok) nlForm.reset();
            })
            .catch(function () {
                nlMsg.textContent = 'No pudimos procesar tu suscripción. Intentá de nuevo en unos minutos.';
                nlMsg.style.color = '#ffb4b4';
            });
        });
    }

    /* ── Auto-hide toasts ───────────────────────────────────── */
    var toasts = document.querySelectorAll('.pd-toast');
    toasts.forEach(function (t) {
        setTimeout(function () {
            t.style.transition = 'opacity 0.4s';
            t.style.opacity = '0';
            setTimeout(function () { t.remove(); }, 400);
        }, 5000);
    });

    /* ── Smooth scroll para anclas ─────────────────────────── */
    document.querySelectorAll('a[href^="#"]').forEach(function (a) {
        a.addEventListener('click', function (e) {
            var target = document.querySelector(a.getAttribute('href'));
            if (target) {
                e.preventDefault();
                target.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        });
    });
})();
