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
        $form.find('select').val('0');
        $form.find('input[name="page"]').val('1');
        $form.submit();
    });

    // Debug: Log pagination clicks
    $(document).on('click', '.page-link', function (e) {
        console.log('Pagination link clicked:', $(this).attr('href')); // Debug
    });
});