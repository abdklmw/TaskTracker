$(document).ready(function () {
    // Show filter form
    $('.filter-btn').on('click', function (e) {
        console.log('Show filter button clicked'); // Debug
        $('#filter-form').show();
        $(this).hide();
    });

    // Toggle date inputs based on checkbox state
    function toggleDateInputs(checkboxId, startId, endId) {
        var isChecked = $(checkboxId).is(':checked');
        $(startId).prop('disabled', isChecked);
        $(endId).prop('disabled', isChecked);
    }

    // Initialize date input states based on checkbox
    toggleDateInputs('#invoicedDateAny', '#invoicedDateStart', '#invoicedDateEnd');
    toggleDateInputs('#invoiceSentDateAny', '#invoiceSentDateStart', '#invoiceSentDateEnd');
    toggleDateInputs('#paidDateAny', '#paidDateStart', '#paidDateEnd');

    // Bind checkbox change events
    $('#invoicedDateAny').change(function () {
        toggleDateInputs('#invoicedDateAny', '#invoicedDateStart', '#invoicedDateEnd');
    });
    $('#invoiceSentDateAny').change(function () {
        toggleDateInputs('#invoiceSentDateAny', '#invoiceSentDateStart', '#invoiceSentDateEnd');
    });
    $('#paidDateAny').change(function () {
        toggleDateInputs('#paidDateAny', '#paidDateStart', '#paidDateEnd');
    });

    // Clear filter
    $('.clear-filter-btn').on('click', function (e) {
        console.log('Clear filter button clicked'); // Debug
        var $form = $(this).closest('form');

        // Clear select elements (set to no selection for multi-select, first option for single select)
        $form.find('select[multiple]').val([]); // Clear multi-select (projectFilter)
        $form.find('select:not([multiple])').val('0'); // Set single select (clientFilter) to "Select Client"

        // Clear date, number, and checkbox inputs
        $form.find('input[type="date"], input[type="number"]').val('');
        $form.find('input[type="checkbox"]').prop('checked', false);

        // Enable all date inputs
        $form.find('input[type="date"]').prop('disabled', false);

        // Reset page to 1
        $form.find('input[name="page"]').val('1');

        // Submit form to apply cleared filters
        $form.submit();
    });

    // Debug: Log pagination clicks
    $(document).on('click', '.page-link', function (e) {
        console.log('Pagination link clicked:', $(this).attr('href')); // Debug
    });
});