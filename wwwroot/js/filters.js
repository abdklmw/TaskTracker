document.addEventListener('DOMContentLoaded', function () {
    const filterForm = document.getElementById('filter-form');
    const showFilterBtn = document.getElementById('show-filter-btn');
    const clearFilterBtn = document.getElementById('clear-filter-btn');

    if (filterForm && showFilterBtn) {
        // Show filter form and hide show button
        showFilterBtn.addEventListener('click', function () {
            filterForm.style.display = 'block';
            showFilterBtn.style.display = 'none';
        });

        // Clear filters and hide form
        clearFilterBtn.addEventListener('click', function () {
            // Redirect to Index with no filters
            window.location.href = '/TimeEntries/Index?recordLimit=' + filterForm.querySelector('[name="recordLimit"]').value;
        });
    }
});