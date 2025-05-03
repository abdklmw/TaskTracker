document.addEventListener('DOMContentLoaded', function () {
    // Handle Create New button click
    $('#create-new-btn').click(function () {
        $('#create-form').css('display', 'block');
        $('#create-new-btn').css('display', 'none');
    });

    // Handle Cancel button click for create
    $('.cancel-create-btn').click(function () {
        $('#create-form').css('display', 'none');
        $('#create-new-btn').css('display', '');
    });

    $('.edit-btn').click(function () {
        let id = $(this).data('id');
        $('#display-row-' + id).css('display', 'none');
        $('#edit-row-' + id).css('display', 'block');
    });

    // Handle Cancel button click for edit
    $('.cancel-btn').click(function () {
        let id = $(this).data('id');
        $('#edit-row-' + id).css('display', 'none');
        $('#display-row-' + id).css('display', 'block');
    });

    // Fade out success alerts after 3 seconds
    $('.alert-success').delay(3000).fadeOut(1000, function () {
        $(this).alert('close');
    });

    // Calculate HoursSpent based on StartDateTime and EndDateTime
    function calculateHoursSpent(startInput, endInput, hoursInput) {
        if (startInput.value && endInput.value) {
            const start = new Date(startInput.value);
            const end = new Date(endInput.value);
            if (!isNaN(start) && !isNaN(end) && end > start) {
                const diffMs = end - start;
                const hours = diffMs / (1000 * 60 * 60); // Convert milliseconds to hours
                hoursInput.value = hours.toFixed(2); // Round to 2 decimal places
            } else {
                hoursInput.value = ''; // Clear if invalid, empty, or end <= start
            }
        } else {
            hoursInput.value = ''; // Clear if either input is empty
        }
    }

    // Create form: Attach listeners to StartDateTime and EndDateTime
    const createStartInput = document.querySelector('#create-form input[name="StartDateTime"]');
    const createEndInput = document.querySelector('#create-form input[name="EndDateTime"]');
    const createHoursInput = document.querySelector('#create-form input[name="HoursSpent"]');

    if (createStartInput && createEndInput && createHoursInput) {
        createStartInput.addEventListener('input', () => calculateHoursSpent(createStartInput, createEndInput, createHoursInput));
        createEndInput.addEventListener('input', () => calculateHoursSpent(createStartInput, createEndInput, createHoursInput));
    }

    // Edit forms: Attach listeners to all edit forms
    document.querySelectorAll('.edit-mode').forEach(form => {
        const startInput = form.querySelector('input[name="StartDateTime"]');
        const endInput = form.querySelector('input[name="EndDateTime"]');
        const hoursInput = form.querySelector('input[name="HoursSpent"]');
        if (startInput && endInput && hoursInput) {
            startInput.addEventListener('input', () => calculateHoursSpent(startInput, endInput, hoursInput));
            endInput.addEventListener('input', () => calculateHoursSpent(startInput, endInput, hoursInput));
        }
    });
});