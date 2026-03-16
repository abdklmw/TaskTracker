document.addEventListener('DOMContentLoaded', function () {

    // Calculate TotalAmount as UnitAmount * Quantity for expense forms
    function calculateTotalAmount(form) {
        const unitAmountInput = form.querySelector('.unit-amount-input');
        const quantityInput = form.querySelector('.quantity-input');
        const totalAmountInput = form.querySelector('.total-amount-input');
        if (unitAmountInput && quantityInput && totalAmountInput) {
            const unitAmount = parseFloat(unitAmountInput.value) || 0;
            const quantity = parseFloat(quantityInput.value) || 1;
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
    };

    // Auto-calculate total on unit amount or quantity input
    document.addEventListener('input', function (event) {
        const target = event.target;
        const form = target.closest('form[action*="/Expenses/Create"], form[action*="/Expenses/Edit"]');
        if (!form) return;

        if (target.classList.contains('unit-amount-input') || target.classList.contains('quantity-input')) {
            calculateTotalAmount(form);
        }
    });

    // Validate expense form — returns list of missing fields
    function getExpenseMissingFields(form, formType) {
        const isCreateForm = formType === 'create';
        const missing = [];

        const clientSelect = isCreateForm
            ? (form.querySelector('select[name="ClientID"]') || form.querySelector('input[type="hidden"][name="ClientID"]'))
            : (form.querySelector('select[name="Expense.ClientID"]') || form.querySelector('input[type="hidden"][name="Expense.ClientID"]'));
        const productSelect = form.querySelector('.product-select');
        const descriptionInput = form.querySelector('.description-input');
        const unitAmountInput = form.querySelector('.unit-amount-input');
        const quantityInput = form.querySelector('.quantity-input');

        if (!clientSelect || !clientSelect.value || clientSelect.value === '0') missing.push('Client');
        if (isCreateForm && (!productSelect || !productSelect.value)) missing.push('Product');
        if (!descriptionInput || !descriptionInput.value.trim()) missing.push('Description');
        if (!unitAmountInput || !unitAmountInput.value || parseFloat(unitAmountInput.value) === 0) missing.push('Unit Amount');
        if (!quantityInput || !quantityInput.value || parseInt(quantityInput.value) < 1) missing.push('Quantity');

        return missing;
    }

    // Validate on Create button click
    document.addEventListener('click', function (event) {
        const button = event.target.closest('.create-btn');
        if (!button) return;

        const form = button.closest('form[action*="/Expenses/Create"]');
        if (!form) return;

        const missing = getExpenseMissingFields(form, 'create');
        if (missing.length > 0) {
            event.preventDefault();
            alert('Please fill in the following fields: ' + missing.join(', '));
        }
    });

    // Validate on Save button click (edit forms)
    document.addEventListener('click', function (event) {
        const button = event.target.closest('.save-btn');
        if (!button) return;

        const form = button.closest('form[action*="/Expenses/Edit"]');
        if (!form) return;

        const missing = getExpenseMissingFields(form, 'edit');
        if (missing.length > 0) {
            event.preventDefault();
            alert('Please fill in the following fields: ' + missing.join(', '));
        }
    });

    // Initialize total amounts on page load
    const expenseCreateForm = document.querySelector('#create-form form[action*="/Expenses/Create"]');
    if (expenseCreateForm) {
        calculateTotalAmount(expenseCreateForm);
    }

    document.querySelectorAll('form[action*="/Expenses/Edit"]').forEach(form => {
        calculateTotalAmount(form);
    });
});
