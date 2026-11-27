(() => {
    let pendingConfirmForm = null;
    const confirmModalEl = document.getElementById('sbConfirmModal');
    const confirmMessageEl = document.getElementById('sbConfirmMessage');
    const confirmOkBtn = document.getElementById('sbConfirmOk');

    if (confirmOkBtn) {
        confirmOkBtn.addEventListener('click', () => {
            const target = pendingConfirmForm;
            pendingConfirmForm = null;
            if (!target) return;
            target.dataset.confirmed = '1';
            if (confirmModalEl && window.bootstrap) {
                window.bootstrap.Modal.getOrCreateInstance(confirmModalEl).hide();
            }
            if (target.requestSubmit) target.requestSubmit();
            else target.submit();
        });
    }
    if (confirmModalEl) {
        confirmModalEl.addEventListener('hidden.bs.modal', () => {
            pendingConfirmForm = null;
        });
    }

    const sections = [...document.querySelectorAll('details.config-section')];
    const facilityInput = document.querySelector('details.config-section input[name="FacilityId"], details.config-section input[name="facilityId"], input[name="id"]');
    const sectionKey = 'config-section:' + (facilityInput?.value || (location.pathname + location.search));
    const lastSection = sessionStorage.getItem(sectionKey);
    if (lastSection && sections.some(s => s.querySelector('summary').textContent === lastSection)) {
        sections.forEach(section => { section.open = section.querySelector('summary').textContent === lastSection; });
    }
    sections.forEach(section => section.addEventListener('toggle', () => {
        if (section.open) sessionStorage.setItem(sectionKey, section.querySelector('summary').textContent);
    }));

    document.addEventListener('submit', event => {
        const form = event.target;
        const message = form.dataset.confirm;
        if (message && form.dataset.confirmed !== '1') {
            if (confirmModalEl && confirmMessageEl && window.bootstrap) {
                event.preventDefault();
                pendingConfirmForm = form;
                confirmMessageEl.textContent = message;
                window.bootstrap.Modal.getOrCreateInstance(confirmModalEl).show();
                return;
            }
            if (!window.confirm(message)) {
                event.preventDefault();
                return;
            }
        }
        delete form.dataset.confirmed;

        const section = form.closest('details.config-section');
        const summary = section?.querySelector('summary')?.textContent;
        if (summary) sessionStorage.setItem(sectionKey, summary);

        if (!form.hasAttribute('data-no-loading')) {
            setTimeout(() => {
                if (event.defaultPrevented) return;
                form.querySelectorAll('button[type="submit"], button:not([type])').forEach(b => {
                    b.disabled = true;
                    b.classList.add('is-loading');
                });
            }, 0);
        }
    });

    document.querySelectorAll('.alert[data-auto-dismiss]').forEach(el => {
        const ms = Number(el.dataset.autoDismiss || 5500);
        if (ms <= 0) return;
        setTimeout(() => {
            if (window.bootstrap && window.bootstrap.Alert) {
                window.bootstrap.Alert.getOrCreateInstance(el).close();
            } else {
                el.remove();
            }
        }, ms);
    });
})();
