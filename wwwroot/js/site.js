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

    // Calculate TotalAmount as UnitAmount * Quantity for expense forms
    function calculateTotalAmount(form) {
        const unitAmountInput = form.querySelector('.unit-amount-input');
        const quantityInput = form.querySelector('.quantity-input');
        const totalAmountInput = form.querySelector('.total-amount-input');
        if (unitAmountInput && quantityInput && totalAmountInput) {
            const unitAmount = parseFloat(unitAmountInput.value) || 0;
            const quantity = parseInt(quantityInput.value) || 1;
            const totalAmount = unitAmount * quantity;
            totalAmountInput.value = totalAmount.toFixed(2);
            console.log(`Calculated TotalAmount: ${totalAmount} (UnitAmount: ${unitAmount}, Quantity: ${quantity})`);
        } else {
            console.warn('Missing inputs for TotalAmount calculation', { unitAmountInput, quantityInput, totalAmountInput });
        }
    }

    // Update Description and UnitAmount based on product selection
    window.updateProductFields = function (selectElement) {
        const form = selectElement.closest('form');
        const descriptionInput = form.querySelector('.description-input');
        const unitAmountInput = form.querySelector('.unit-amount-input');

        if (!descriptionInput || !unitAmountInput) {
            console.warn('Missing form elements', { descriptionInput, unitAmountInput });
            return;
        }

        const selectedOption = selectElement.options[selectElement.selectedIndex];
        console.log('Product selected:', {
            value: selectElement.value,
            sku: selectedOption.dataset.sku,
            name: selectedOption.dataset.name,
            price: selectedOption.dataset.price
        });

        if (selectedOption.value && selectedOption.dataset.sku && selectedOption.dataset.name) {
            descriptionInput.value = `${selectedOption.dataset.sku} - ${selectedOption.dataset.name}`;
            unitAmountInput.value = parseFloat(selectedOption.dataset.price || 0).toFixed(2);
        } else {
            descriptionInput.value = '';
            unitAmountInput.value = '';
        }

        calculateTotalAmount(form);
        validateExpenseCreateForm();
    };

    // Validate expense create form
    function validateExpenseCreateForm() {
        const expenseForm = document.querySelector('#create-form form[asp-action="Create"]');
        if (expenseForm) {
            const clientSelect = expenseForm.querySelector('select[name="ClientID"]');
            const descriptionInput = expenseForm.querySelector('.description-input');
            const unitAmountInput = expenseForm.querySelector('.unit-amount-input');
            const quantityInput = expenseForm.querySelector('.quantity-input');
            const totalAmountInput = expenseForm.querySelector('.total-amount-input');
            const createButton = expenseForm.querySelector('.create-btn');
            const isExpenseValid =
                clientSelect.value && clientSelect.value !== '0' &&
                descriptionInput.value.trim() &&
                unitAmountInput.value && parseFloat(unitAmountInput.value) >= 0 &&
                quantityInput.value && parseInt(quantityInput.value) >= 1 &&
                totalAmountInput.value && parseFloat(totalAmountInput.value) >= 0;
            createButton.disabled = !isExpenseValid;
            console.log('Expense form validation:', { isExpenseValid, client: clientSelect.value, description: descriptionInput.value });
        }
    }

    // Attach input listeners to expense create form
    const expenseCreateForm = document.querySelector('#create-form form[asp-action="Create"]');
    if (expenseCreateForm) {
        const inputs = expenseCreateForm.querySelectorAll('.unit-amount-input, .quantity-input, .product-select');
        inputs.forEach(input => {
            input.addEventListener('input', () => {
                calculateTotalAmount(expenseCreateForm);
                validateExpenseCreateForm();
            });
        });
        // Initialize TotalAmount and validation
        calculateTotalAmount(expenseCreateForm);
        validateExpenseCreateForm();
    }

    // Attach input listeners to expense edit forms
    document.querySelectorAll('.edit-mode form[asp-action="Edit"]').forEach(form => {
        const inputs = form.querySelectorAll('.unit-amount-input, .quantity-input, .product-select');
        inputs.forEach(input => {
            input.addEventListener('input', () => {
                calculateTotalAmount(form);
                // Note: Edit forms may have separate validation if needed
            });
        });
        // Initialize TotalAmount
        calculateTotalAmount(form);
    });
});