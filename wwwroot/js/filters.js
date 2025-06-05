$(document).ready(function () {
    // Show filter form
    $('.filter-btn').on('click', function (e) {
        console.log('Show filter button clicked'); // Debug
        $('#filter-form').show();
        $(this).hide();
    });

    // Clear filter
    $('.clear-filter-btn').on('click', function (e) {
        console.log('Clear filter button clicked'); // Debug
        var $form = $(this).closest('form');
        // Reset select elements
        $form.find('select').each(function () {
            $(this).val($(this).find('option:first').val());
        });
        // Clear date and number inputs
        $form.find('input[type="date"], input[type="number"]').val('');
        // Reset page to 1
        $form.find('input[name="page"]').val('1');
        // Preserve recordLimit
        $form.submit();
    });

    // Debug: Log pagination clicks
    $(document).on('click', '.page-link', function (e) {
        console.log('Pagination link clicked:', $(this).attr('href')); // Debug
    });
});