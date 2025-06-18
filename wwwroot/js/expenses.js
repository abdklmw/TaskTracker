document.addEventListener('DOMContentLoaded', function () {

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

        if (selectedOption.value && selectedOption.dataset.sku && selectedOption.dataset.name) {
            descriptionInput.value = `${selectedOption.dataset.sku} - ${selectedOption.dataset.name}`;
            unitAmountInput.value = parseFloat(selectedOption.dataset.price || 0).toFixed(2);
        } else {
            descriptionInput.value = '';
            unitAmountInput.value = '';
        }

        calculateTotalAmount(form);
        validateExpenseForm(form, form.closest('#create-form') ? 'create' : 'edit');
    };

    // Consolidated validation function for expense create and edit forms
    function validateExpenseForm(form, formType) {
        const isCreateForm = formType === 'create';
        const clientSelect = form.querySelector('select[name="ClientID"]');
        const descriptionInput = form.querySelector('.description-input');
        const unitAmountInput = form.querySelector('.unit-amount-input');
        const quantityInput = form.querySelector('.quantity-input');
        const totalAmountInput = form.querySelector('.total-amount-input');
        const submitButton = form.querySelector(isCreateForm ? '.create-btn' : '.save-btn');

        const isExpenseValid =
            clientSelect?.value && clientSelect.value !== '0' &&
            descriptionInput?.value.trim() &&
            unitAmountInput?.value && parseFloat(unitAmountInput.value) >= 0 &&
            quantityInput?.value && parseInt(quantityInput.value) >= 1 &&
            totalAmountInput?.value && parseFloat(totalAmountInput.value) >= 0;

        if (submitButton) {
            submitButton.disabled = !isExpenseValid;
        }
    }

    // Handle input events for both create and edit expense forms using event delegation
    document.addEventListener('input', function (event) {
        const target = event.target;
        const form = target.closest('form[action="/Expenses/Create"], form[action^="/Expenses/Edit"]');

        if (!form) return; // Exit if not a relevant form

        const formType = form.matches('[action="/Expenses/Create"]') ? 'create' : 'edit';

        // Handle unit-amount-input or quantity-input changes
        if (target.classList.contains('unit-amount-input') || target.classList.contains('quantity-input')) {
            calculateTotalAmount(form);
            validateExpenseForm(form, formType);
        }
        // Handle additional inputs: product-select, description-input, or ClientID select
        else if (
            target.classList.contains('product-select') ||
            target.classList.contains('description-input') ||
            target.matches('select[name="ClientID"]')
        ) {
            validateExpenseForm(form, formType);
        }
    });

    // Initialize expense forms
    const expenseCreateForm = document.querySelector('#create-form form[action="/Expenses/Create"]');
    if (expenseCreateForm) {
        calculateTotalAmount(expenseCreateForm);
        validateExpenseForm(expenseCreateForm, 'create');
    } else {
        console.warn('Create form not found on DOMContentLoaded');
    }

    document.querySelectorAll('form[action^="/Expenses/Edit"]').forEach(form => {
        calculateTotalAmount(form);
        validateExpenseForm(form, 'edit');
    });
});