document.addEventListener('DOMContentLoaded', function () {
    // Handle Edit button click
    document.querySelectorAll('.edit-btn').forEach(button => {
        button.addEventListener('click', function () {
            const id = this.getAttribute('data-id');
            document.getElementById(`display-row-${id}`).style.display = 'none';
            document.getElementById(`edit-row-${id}`).style.display = 'table-row';
        });
    });

    // Handle Cancel button click for edit
    document.querySelectorAll('.cancel-btn').forEach(button => {
        button.addEventListener('click', function () {
            const id = this.getAttribute('data-id');
            document.getElementById(`display-row-${id}`).style.display = 'table-row';
            document.getElementById(`edit-row-${id}`).style.display = 'none';
        });
    });

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

    // Fade out success alert after 15 seconds
    const successAlert = document.querySelector('.alert-success');
    if (successAlert) {
        setTimeout(() => {
            successAlert.style.opacity = '0';
            setTimeout(() => {
                successAlert.style.display = 'none';
            }, 1000);
        }, 5000);
    }
});