document.addEventListener('DOMContentLoaded', function () {
    // Handle Create New button click
    const createNewBtn = document.getElementById('create-new-btn');
    if (createNewBtn) {
        createNewBtn.addEventListener('click', function () {
            document.getElementById('create-form').style.display = 'block';
            this.style.display = 'none';
        });
    }

    // Handle Cancel button click for create
    const cancelCreateBtn = document.querySelector('.cancel-create-btn');
    if (cancelCreateBtn) {
        cancelCreateBtn.addEventListener('click', function () {
            document.getElementById('create-form').style.display = 'none';
            document.getElementById('create-new-btn').style.display = 'inline-block';
        });
    }

    // Handle Edit button click
    document.querySelectorAll('.edit-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const id = this.getAttribute('data-id');
            document.getElementById(`display-row-${id}`).style.display = 'none';
            document.getElementById(`edit-row-${id}`).style.display = 'table-row';
        });
    });

    // Handle Cancel button click for edit
    document.querySelectorAll('.cancel-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const id = this.getAttribute('data-id');
            document.getElementById(`edit-row-${id}`).style.display = 'none';
            document.getElementById(`display-row-${id}`).style.display = 'table-row';
        });
    });

    // Fade out success alerts after 15 seconds
    setTimeout(() => {
        const alerts = document.querySelectorAll('.alert-success');
        alerts.forEach(alert => {
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 1000);
        });
    }, 15000);

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