(() => {
    // 1. Live default court-type illustration preview when owner changes TypeId
    const typeSelect = document.getElementById('courtTypeSelect');
    const typePreviewImg = document.getElementById('courtTypePreviewImg');
    if (typeSelect && typePreviewImg) {
        const updateTypeImg = () => {
            const opt = typeSelect.options[typeSelect.selectedIndex];
            if (opt && opt.dataset.typeImg) {
                typePreviewImg.src = opt.dataset.typeImg;
            }
        };
        typeSelect.addEventListener('change', updateTypeImg);
    }

    // 2. Multi-image drag & drop + preview + progress upload
    const form = document.getElementById('courtImageUploadForm');
    if (!form) return;

    const dropzone = document.getElementById('courtImageDropzone');
    const fileInput = document.getElementById('courtImageFileInput');
    const previewGrid = document.getElementById('courtImagePreviewGrid');
    const statusBox = document.getElementById('courtUploadStatus');
    const progressWrap = document.getElementById('courtUploadProgressWrap');
    const progressBar = document.getElementById('courtUploadProgressBar');
    const submitBtn = document.getElementById('courtUploadSubmitBtn');

    const maxBytes = Number(form.dataset.maxBytes || 5242880);
    const maxRemaining = Number(form.dataset.remaining || 10);
    const allowedMime = new Set(['image/jpeg', 'image/jpg', 'image/png', 'image/webp']);
    const allowedExt = /\.(jpe?g|png|webp)$/i;

    let selectedFiles = [];

    function showStatus(msg, type = 'info') {
        if (!statusBox) return;
        statusBox.className = 'alert alert-' + type + ' py-2 small mt-2 mb-0';
        statusBox.textContent = msg;
        statusBox.classList.remove('d-none');
    }

    function clearStatus() {
        if (!statusBox) return;
        statusBox.classList.add('d-none');
        statusBox.textContent = '';
    }

    function renderPreviews() {
        previewGrid.innerHTML = '';
        if (selectedFiles.length === 0) {
            previewGrid.classList.add('d-none');
            submitBtn.disabled = true;
            submitBtn.textContent = 'Tải ảnh lên';
            return;
        }
        previewGrid.classList.remove('d-none');
        submitBtn.disabled = false;
        submitBtn.textContent = `Tải lên ${selectedFiles.length} ảnh đã chọn`;

        selectedFiles.forEach((file, idx) => {
            const col = document.createElement('div');
            col.className = 'col-6 col-sm-4 col-md-3';
            const url = URL.createObjectURL(file);
            const sizeKb = Math.max(1, Math.round(file.size / 1024));
            col.innerHTML = `
                <div class="card p-2 h-100">
                    <img src="${url}" alt="Xem trước" class="sb-upload-card-img mb-2" />
                    <div class="small text-truncate fw-semibold" title="${file.name.replace(/"/g, '&quot;')}">${file.name}</div>
                    <div class="d-flex justify-content-between align-items-center mt-1">
                        <span class="small text-muted">${sizeKb} KB</span>
                        <button type="button" class="btn btn-sm btn-outline-danger py-0 px-2" style="min-height:26px;" data-remove-idx="${idx}">Bỏ</button>
                    </div>
                </div>`;
            col.querySelector('img').addEventListener('load', () => URL.revokeObjectURL(url), { once: true });
            col.querySelector('[data-remove-idx]').addEventListener('click', () => {
                selectedFiles.splice(idx, 1);
                renderPreviews();
            });
            previewGrid.appendChild(col);
        });
    }

    function addFiles(fileList) {
        clearStatus();
        const errors = [];
        for (const f of Array.from(fileList || [])) {
            if (!allowedExt.test(f.name) || (f.type && !allowedMime.has(f.type.toLowerCase()))) {
                errors.push(`"${f.name}": chỉ hỗ trợ JPG, PNG, WebP.`);
                continue;
            }
            if (f.size <= 0 || f.size > maxBytes) {
                const maxMb = (maxBytes / (1024 * 1024)).toFixed(1).replace(/\.0$/, '');
                errors.push(`"${f.name}": vượt quá ${maxMb} MB.`);
                continue;
            }
            if (selectedFiles.length >= maxRemaining) {
                errors.push(`Chỉ có thể thêm tối đa ${maxRemaining} ảnh nữa cho sân này.`);
                break;
            }
            selectedFiles.push(f);
        }
        if (errors.length > 0) showStatus(errors.join(' '), 'warning');
        renderPreviews();
    }

    if (dropzone && fileInput) {
        dropzone.addEventListener('click', () => fileInput.click());
        dropzone.addEventListener('keydown', e => {
            if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); fileInput.click(); }
        });
        fileInput.addEventListener('change', () => {
            addFiles(fileInput.files);
            fileInput.value = '';
        });
        ['dragenter', 'dragover'].forEach(ev => dropzone.addEventListener(ev, e => {
            e.preventDefault();
            dropzone.classList.add('is-dragover');
        }));
        ['dragleave', 'drop'].forEach(ev => dropzone.addEventListener(ev, e => {
            e.preventDefault();
            dropzone.classList.remove('is-dragover');
        }));
        dropzone.addEventListener('drop', e => addFiles(e.dataTransfer?.files));
    }

    form.addEventListener('submit', e => {
        if (selectedFiles.length === 0) {
            e.preventDefault();
            showStatus('Vui lòng chọn ít nhất một tệp ảnh hợp lệ.', 'warning');
            return;
        }
        e.preventDefault();
        clearStatus();

        const fd = new FormData();
        form.querySelectorAll('input[type="hidden"]').forEach(h => fd.append(h.name, h.value));
        selectedFiles.forEach(f => fd.append('files', f));

        const xhr = new XMLHttpRequest();
        xhr.open('POST', form.action, true);
        xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');

        submitBtn.disabled = true;
        progressWrap?.classList.remove('d-none');
        if (progressBar) {
            progressBar.style.width = '5%';
            progressBar.textContent = '5%';
        }

        xhr.upload.addEventListener('progress', ev => {
            if (ev.lengthComputable && progressBar) {
                const pct = Math.min(99, Math.round((ev.loaded / ev.total) * 100));
                progressBar.style.width = pct + '%';
                progressBar.textContent = pct + '%';
            }
        });

        xhr.addEventListener('load', () => {
            try {
                const data = JSON.parse(xhr.responseText || '{}');
                if (xhr.status >= 200 && xhr.status < 300 && data.ok) {
                    if (progressBar) {
                        progressBar.style.width = '100%';
                        progressBar.textContent = '100%';
                    }
                    showStatus(data.message || 'Tải ảnh thành công!', 'success');
                    setTimeout(() => location.reload(), 450);
                    return;
                }
                showStatus(data.message || 'Tải ảnh thất bại. Vui lòng kiểm tra lại tệp ảnh.', 'danger');
            } catch {
                showStatus('Không thể tải ảnh lên (mã ' + xhr.status + ').', 'danger');
            }
            submitBtn.disabled = false;
            progressWrap?.classList.add('d-none');
        });

        xhr.addEventListener('error', () => {
            showStatus('Lỗi kết nối khi tải ảnh lên. Vui lòng thử lại.', 'danger');
            submitBtn.disabled = false;
            progressWrap?.classList.add('d-none');
        });

        xhr.send(fd);
    });
})();
