document.addEventListener('DOMContentLoaded', function () {

    // ── Global Client Persistence ──────────────────────────────────────

    var globalClientSelect = document.getElementById('global-client-select');

    if (globalClientSelect) {
        // Save selection to localStorage whenever it changes
        globalClientSelect.addEventListener('change', function () {
            var value = globalClientSelect.value;
            if (value && value !== '0') {
                localStorage.setItem('globalClientId', value);
            } else {
                localStorage.removeItem('globalClientId');
            }
        });

        // On page load: restore from localStorage if session is out of sync
        var storedClientId = localStorage.getItem('globalClientId');
        var sessionClientId = (window.AppSettings && window.AppSettings.GlobalClientId)
            ? String(window.AppSettings.GlobalClientId)
            : '0';

        if (storedClientId && storedClientId !== '0' && storedClientId !== sessionClientId) {
            // Session doesn't match stored preference — sync by submitting the form
            globalClientSelect.value = storedClientId;
            var form = document.getElementById('global-client-form');
            if (form) {
                form.submit();
            }
        } else if ((!storedClientId || storedClientId === '0') && sessionClientId !== '0') {
            // localStorage says "All Clients" but session has a client — clear session
            globalClientSelect.value = '0';
            var form = document.getElementById('global-client-form');
            if (form) {
                form.submit();
            }
        }
    }

    // ── Record Limit Persistence (per page) ────────────────────────────

    var currentPage = (window.AppSettings && window.AppSettings.CurrentPage) || '';
    if (!currentPage) return;

    var storageKey = 'recordLimit_' + currentPage;
    var recordLimitSelect = document.getElementById('recordLimit');

    if (recordLimitSelect) {
        // Save selection to localStorage whenever it changes
        recordLimitSelect.addEventListener('change', function () {
            localStorage.setItem(storageKey, recordLimitSelect.value);
        });

        // On page load: if recordLimit is NOT in the URL, apply the stored value
        var urlParams = new URLSearchParams(window.location.search);
        var urlRecordLimit = urlParams.get('recordLimit');

        if (urlRecordLimit) {
            // URL has an explicit value — save it as the new preference
            localStorage.setItem(storageKey, urlRecordLimit);
        } else {
            // No recordLimit in URL — check localStorage
            var storedLimit = localStorage.getItem(storageKey);
            if (storedLimit && storedLimit !== recordLimitSelect.value) {
                // Redirect with the stored limit applied
                urlParams.set('recordLimit', storedLimit);
                urlParams.set('page', '1');
                window.location.search = urlParams.toString();
            }
        }
    }
});
