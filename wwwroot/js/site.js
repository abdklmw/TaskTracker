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

    // Handle Edit button click
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

    // Handle Start Timer button click
    $('.start-timer-btn').click(function (e) {
        e.preventDefault(); // Prevent default button behavior
        const form = document.getElementById('create-time-entry-form');
        const startInput = document.querySelector('#create-form input[name="StartDateTime"]');
        const actionInput = document.getElementById('form-action');
        try {
            if (startInput) {
                startInput.removeAttribute('required'); // Bypass StartDateTime validation
            }
            if (actionInput) {
                actionInput.value = 'StartTimer'; // Set action for controller
            }
            form.submit(); // Submit the form
        } catch (error) {
            console.error('Error submitting Start Timer form:', error);
        }
    });

    // Update hours spent for running timers
    function updateHoursSpent() {
        const hoursSpentElements = document.querySelectorAll('.hours-spent');
        hoursSpentElements.forEach(element => {
            const startUtc = new Date(element.dataset.startUtc);
            const timeEntryId = element.dataset.timeEntryId;
            if (!isNaN(startUtc)) {
                // Convert UTC start time to local time using dynamic userTimezoneOffset
                const startLocalMs = startUtc.getTime() + (userTimezoneOffset * 60 * 1000);
                const nowLocalMs = Date.now();
                const diffMs = nowLocalMs - startLocalMs;
                const hours = diffMs / (1000 * 60 * 60); // Convert to hours
                // Round up to nearest quarter hour (0.25)
                const roundedHours = Math.ceil(hours * 4) / 4;
                element.textContent = roundedHours.toFixed(2);
            } else {
                console.warn(`Invalid start time for time entry ${timeEntryId}`);
                element.textContent = 'N/A';
            }
        });
    }

    // Initial update and refresh every minute
    updateHoursSpent();
    setInterval(updateHoursSpent, 60 * 1000); // Update every minute
});