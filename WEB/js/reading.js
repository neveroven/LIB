// Reading page logic: простая фиксация прогресса по скроллу

document.addEventListener('DOMContentLoaded', () => {
    const readerPanel = document.querySelector('.reader-panel');
    const contentEl = document.getElementById('bookContent');
    const progressBar = document.getElementById('readerProgressBar');
    const progressText = document.getElementById('readerProgressText');

    if (!readerPanel || !contentEl || !progressBar || !progressText || !window.READING_PROGRESS) {
        return;
    }

    const { bookFileId, initialPercent, apiUrl } = window.READING_PROGRESS;
    let lastSent = initialPercent || 0;

    // Восстанавливаем прокрутку
    if (initialPercent && contentEl.scrollHeight > 0) {
        const target = (initialPercent / 100) * (contentEl.scrollHeight - contentEl.clientHeight);
        contentEl.scrollTop = target;
        updateUi(initialPercent);
    }

    function updateUi(percent) {
        progressBar.style.width = `${percent}%`;
        progressText.textContent = `${Math.round(percent)}%`;
    }

    // Троттлинг отправки прогресса
    let sendTimer = null;
    function scheduleSend(percent) {
        if (sendTimer) return;
        sendTimer = setTimeout(() => {
            sendTimer = null;
            sendProgress(percent);
        }, 800);
    }

    function sendProgress(percent) {
        // Не спамим одинаковыми значениями
        if (Math.abs(percent - lastSent) < 1) {
            return;
        }
        lastSent = percent;
        fetch(apiUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                book_file_id: bookFileId,
                progress_percent: percent,
                current_page: 0,
                total_pages: 0
            })
        }).catch(() => {
            // молча игнорируем временные ошибки сети
        });
    }

    function computePercent() {
        const maxScroll = contentEl.scrollHeight - contentEl.clientHeight;
        if (maxScroll <= 0) return 0;
        const percent = (contentEl.scrollTop / maxScroll) * 100;
        return Math.min(100, Math.max(0, percent));
    }

    contentEl.addEventListener('scroll', () => {
        const percent = computePercent();
        updateUi(percent);
        scheduleSend(percent);
    });
});

