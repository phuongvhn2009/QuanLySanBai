document.addEventListener('submit', event => {
    const message = event.target.dataset.confirm;
    if (message && !window.confirm(message)) event.preventDefault();
});
const sections = [...document.querySelectorAll('details.config-section')];
const sectionKey = 'config-section:' + location.pathname + location.search;
const lastSection = sessionStorage.getItem(sectionKey);
if (lastSection) sections.forEach(section => { section.open = section.querySelector('summary').textContent === lastSection; });
sections.forEach(section => section.addEventListener('toggle', () => {
    if (section.open) sessionStorage.setItem(sectionKey, section.querySelector('summary').textContent);
}));
