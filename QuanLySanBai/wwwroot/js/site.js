document.addEventListener('submit', event => {
    const form = event.target;
    const message = form.dataset.confirm;
    if (message && !window.confirm(message)) {
        event.preventDefault();
        return;
    }
    const section = form.closest('details.config-section');
    const summary = section?.querySelector('summary')?.textContent;
    if (summary) sessionStorage.setItem(sectionKey, summary);
    setTimeout(() => form.querySelectorAll('button[type="submit"], button:not([type])').forEach(b => b.disabled = true), 0);
});
const sections = [...document.querySelectorAll('details.config-section')];
const facilityInput = document.querySelector('details.config-section input[name="FacilityId"], details.config-section input[name="facilityId"]');
const sectionKey = 'config-section:' + (facilityInput?.value || (location.pathname + location.search));
const lastSection = sessionStorage.getItem(sectionKey);
if (lastSection && sections.some(s => s.querySelector('summary').textContent === lastSection)) {
    sections.forEach(section => { section.open = section.querySelector('summary').textContent === lastSection; });
}
sections.forEach(section => section.addEventListener('toggle', () => {
    if (section.open) sessionStorage.setItem(sectionKey, section.querySelector('summary').textContent);
}));
