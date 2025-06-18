document.addEventListener('DOMContentLoaded', function () {
    // Handle Create New button click for create forms
    $('#create-new-btn').click(function () {
        $("#create-form").show();
        $("#create-new-btn").hide();
    });

    // Handle Cancel button click for create forms
    $('.cancel-create-btn').click(function () {
        $('#create-form').css('display', 'none');
        $('#create-new-btn').css('display', '');
    });

    // Handle Edit button click for edit forms
    $('.edit-btn').click(function () {
        let id = $(this).data('id');
        $('#display-row-' + id).css('display', 'none');
        $('#edit-row-' + id).css('display', 'block');
    });

    // Handle Cancel button click for edit forms
    $('.cancel-btn').click(function () {
        let id = $(this).data('id');
        $('#edit-row-' + id).css('display', 'none');
        $('#display-row-' + id).css('display', 'block');
    });

    // Fade out success alerts after 3 seconds
    $('.alert-success').delay(3000).fadeOut(1000, function () {
        $(this).alert('close');
    });
});