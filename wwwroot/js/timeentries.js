document.addEventListener('DOMContentLoaded', function () {
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

    // Enable/disable Create and Start Timer buttons based on form input
    function validateTimeEntryCreateForm() {
        const form = document.getElementById('create-time-entry-form');
        if (form) {
            const clientSelect = form.querySelector('select[name="ClientID"]');
            const projectSelect = form.querySelector('select[name="ProjectID"]');
            const startInput = form.querySelector('input[name="StartDateTime"]');
            const endInput = form.querySelector('input[name="EndDateTime"]');
            const hoursInput = form.querySelector('input[name="HoursSpent"]');
            const descriptionInput = form.querySelector('textarea[name="Description"]');
            const createButton = form.querySelector('.create-btn');
            const startTimerButton = form.querySelector('.start-timer-btn');

            // Validate Create button: all fields must be filled
            const isCreateValid =
                clientSelect.value && clientSelect.value !== '0' &&
                projectSelect.value && projectSelect.value !== '0' &&
                startInput.value &&
                endInput.value &&
                hoursInput.value &&
                descriptionInput.value.trim();

            createButton.disabled = !isCreateValid;

            // Validate Start Timer button: ClientID, ProjectID, and Description required
            const isStartTimerValid =
                clientSelect.value && clientSelect.value !== '0' &&
                projectSelect.value && projectSelect.value !== '0' &&
                descriptionInput.value.trim();

            startTimerButton.disabled = !isStartTimerValid;
        }
    }

    // Attach input listeners to time entry create form
    const createTimeEntryForm = document.getElementById('create-time-entry-form');
    if (createTimeEntryForm) {
        const inputs = createTimeEntryForm.querySelectorAll('select, input, textarea');
        inputs.forEach(input => {
            input.addEventListener('input', validateTimeEntryCreateForm);
        });
        // Initial validation
        validateTimeEntryCreateForm();
    }

});