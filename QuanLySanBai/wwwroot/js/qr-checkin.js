(() => {
    const activeCleanups = new Set();

    function stopAll() {
        activeCleanups.forEach(fn => fn());
    }
    window.addEventListener('pagehide', stopAll);

    document.querySelectorAll('[data-qr-scanner]').forEach(root => {
        const startBtn = root.querySelector('[data-qr-start]');
        const stopBtn = root.querySelector('[data-qr-stop]');
        const videoWrap = root.querySelector('[data-qr-video-wrap]');
        const video = root.querySelector('video');
        const statusEl = root.querySelector('[data-qr-status]');
        const form = root.querySelector('form') || document.querySelector(root.dataset.qrForm || '');
        const input = root.querySelector('[data-qr-input]') || (form ? form.querySelector('input[name="code"], input[name="Code"]') : null);

        if (!startBtn || !video || !form || !input) return;

        let stream = null;
        let timer = null;
        let busy = false;
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d', { willReadFrequently: true });
        const nativeDetector = typeof window.BarcodeDetector === 'function'
            ? new window.BarcodeDetector({ formats: ['qr_code'] })
            : null;

        function setStatus(msg, isError = false) {
            if (!statusEl) return;
            statusEl.textContent = msg;
            statusEl.className = isError ? 'small text-danger mt-2 mb-0' : 'small text-muted mt-2 mb-0';
        }

        function stop() {
            if (timer) {
                clearTimeout(timer);
                timer = null;
            }
            if (stream) {
                stream.getTracks().forEach(t => t.stop());
                stream = null;
            }
            video.srcObject = null;
            if (videoWrap) videoWrap.classList.add('d-none');
            startBtn.classList.remove('d-none');
            if (stopBtn) stopBtn.classList.add('d-none');
            activeCleanups.delete(stop);
        }

        function onDecoded(value) {
            const text = (value || '').trim();
            if (!text) return false;
            stop();
            input.value = text;
            setStatus('Đã quét mã QR, đang kiểm tra check-in...');
            form.requestSubmit ? form.requestSubmit() : form.submit();
            return true;
        }

        async function scanFrame() {
            if (!stream) return;
            if (!busy && video.readyState >= video.HAVE_CURRENT_DATA && video.videoWidth > 0) {
                busy = true;
                try {
                    if (typeof window.jsQR === 'function' && ctx) {
                        const scale = Math.min(1, 640 / video.videoWidth);
                        canvas.width = Math.max(1, Math.round(video.videoWidth * scale));
                        canvas.height = Math.max(1, Math.round(video.videoHeight * scale));
                        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
                        const img = ctx.getImageData(0, 0, canvas.width, canvas.height);
                        const decoded = window.jsQR(img.data, canvas.width, canvas.height, { inversionAttempts: 'dontInvert' });
                        if (decoded && onDecoded(decoded.data)) return;
                    } else if (nativeDetector) {
                        const codes = await nativeDetector.detect(video);
                        if (codes.length && onDecoded(codes[0].rawValue)) return;
                    }
                } catch {
                    // Ignore transient decode frame errors and continue scanning.
                } finally {
                    busy = false;
                }
            }
            if (stream) timer = setTimeout(scanFrame, 150);
        }

        async function start() {
            if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
                setStatus('Trình duyệt chỉ cấp quyền camera khi mở bằng HTTPS hoặc localhost. Bạn vẫn có thể nhập mã thủ công.', true);
                return;
            }
            stopAll();
            setStatus('Đang xin quyền mở camera...');
            try {
                stream = await navigator.mediaDevices.getUserMedia({
                    video: { facingMode: { ideal: 'environment' } },
                    audio: false
                });
                activeCleanups.add(stop);
                video.srcObject = stream;
                await video.play();
                if (videoWrap) videoWrap.classList.remove('d-none');
                startBtn.classList.add('d-none');
                if (stopBtn) stopBtn.classList.remove('d-none');
                setStatus('Đưa camera vào mã QR Check-in trên điện thoại khách...');
                timer = setTimeout(scanFrame, 200);
            } catch (err) {
                stop();
                const name = err && err.name;
                if (name === 'NotAllowedError' || name === 'PermissionDeniedError') {
                    setStatus('Trình duyệt bị từ chối quyền camera. Hãy cấp quyền camera trên thanh địa chỉ rồi thử lại, hoặc nhập mã thủ công.', true);
                } else if (name === 'NotFoundError' || name === 'DevicesNotFoundError') {
                    setStatus('Không tìm thấy camera trên thiết bị này. Hãy nhập mã đặt sân hoặc quét bằng máy đọc mã.', true);
                } else {
                    setStatus('Không thể mở camera (' + (name || 'lỗi thiết bị') + '). Hãy kiểm tra xem ứng dụng khác có đang dùng camera không.', true);
                }
            }
        }

        startBtn.addEventListener('click', start);
        if (stopBtn) stopBtn.addEventListener('click', () => {
            stop();
            setStatus('Đã tắt camera.');
        });
    });
})();
