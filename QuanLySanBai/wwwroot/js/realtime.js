(() => {
    if (!window.signalR) return;
    const courtId = location.pathname.match(/^\/Courts\/Details\/(\d+)/i)?.[1]
        || (/^\/Courts\/Details$/i.test(location.pathname) ? new URLSearchParams(location.search).get('id') : null);
    const signedIn = document.body.dataset.signedIn === 'true';
    if (!signedIn && !courtId) return;
    const connection = new signalR.HubConnectionBuilder().withUrl('/updates').withAutomaticReconnect().build();
    let timer;
    let dirty = false;
    let connectedBefore = false;
    document.addEventListener('input', event => { if (event.target.closest('form[method="post"]')) dirty = true; });
    function refresh(force = false) {
        if (force || !dirty) {
            clearTimeout(timer);
            timer = setTimeout(() => location.reload(), 300);
        } else if (!document.getElementById('realtime-refresh')) {
            const notice = document.createElement('div');
            notice.id = 'realtime-refresh';
            notice.className = 'alert alert-info';
            notice.setAttribute('role', 'status');
            notice.append('Dữ liệu đã thay đổi. ');
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'btn btn-outline-primary';
            button.textContent = 'Tải dữ liệu mới';
            button.onclick = () => location.reload();
            notice.append(button);
            document.querySelector('main')?.prepend(notice);
        }
    }
    connection.on('PermissionsChanged', () => refresh(true));
    connection.on('CalendarChanged', () => refresh());
    connection.onreconnected(() => refresh());
    connection.onclose(() => setTimeout(start, 5000));
    async function start() {
        try {
            await connection.start();
            if (courtId) await connection.invoke('WatchCourt', Number(courtId));
            if (connectedBefore) refresh();
            connectedBefore = true;
        } catch { if (connection.state === signalR.HubConnectionState.Disconnected) setTimeout(start, 5000); }
    }
    // Refresh after returning to a suspended tab, including notifications missed offline.
    document.addEventListener('visibilitychange', () => { if (!document.hidden) refresh(); });
    start();
})();
