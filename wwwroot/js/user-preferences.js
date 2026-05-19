// One-time cleanup: remove stale localStorage keys from the old client-side persistence.
// Preferences are now stored in the database per-user.
(function () {
    var staleKeys = ['globalClientId', 'recordLimit_invoices', 'recordLimit_timeentries', 'recordLimit_expenses'];
    staleKeys.forEach(function (key) {
        localStorage.removeItem(key);
    });
})();
