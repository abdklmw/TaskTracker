$(document).ready(function () {
    $(".cancel-create-btn").click(function () {
        $("#create-form").hide();
        $("#client-select").val(0);
        $("#time-entries-list").empty();
        $("#expenses-list").empty();
        $("#invoice-total").val("$0.00");
        $("#select-all-time-entries").prop("checked", false);
        $("#select-all-expenses").prop("checked", false);
    });

    $("#client-select").change(function () {
        var clientId = $(this).val();
        if (clientId == 0) {
            $("#time-entries-list").empty();
            $("#expenses-list").empty();
            $("#invoice-total").val("$0.00");
            return;
        }

        $.get("/Invoices/GetUnpaidItems", { clientId: clientId }, function (data) {
            $("#time-entries-list").empty();
            if (data.timeEntries.length > 0) {
                data.timeEntries.forEach(function (item) {
                    $("#time-entries-list").append(
                        `<div class="form-check">
                            <input type="checkbox" class="form-check-input time-entry-checkbox" name="SelectedTimeEntryIDs" value="${item.timeEntryID}" data-amount="${item.totalAmount}" />
                            <label class="form-check-label">
                                ${item.hoursSpent} hours at $${item.hourlyRate.toFixed(2)}/hr = $${item.totalAmount.toFixed(2)}
                            </label>
                        </div>`
                    );
                });
            } else {
                $("#time-entries-list").append("<p>No unpaid time entries found.</p>");
            }

            $("#expenses-list").empty();
            if (data.expenses.length > 0) {
                data.expenses.forEach(function (item) {
                    $("#expenses-list").append(
                        `<div class="form-check">
                            <input type="checkbox" class="form-check-input expense-checkbox" name="SelectedExpenseIDs" value="${item.expenseID}" data-amount="${item.totalAmount}" />
                            <label class="form-check-label">
                                ${item.description} (${item.quantity} x $${item.unitAmount.toFixed(2)}) = $${item.totalAmount.toFixed(2)}
                            </label>
                        </div>`
                    );
                });
            } else {
                $("#expenses-list").append("<p>No unpaid expenses found.</p>");
            }

            updateInvoiceTotal();
        });
    });

    $("#select-all-time-entries").change(function () {
        $(".time-entry-checkbox").prop("checked", $(this).is(":checked"));
        updateInvoiceTotal();
    });

    $("#select-all-expenses").change(function () {
        $(".expense-checkbox").prop("checked", $(this).is(":checked"));
        updateInvoiceTotal();
    });

    $(document).on("change", ".time-entry-checkbox, .expense-checkbox", function () {
        updateInvoiceTotal();
    });

    function updateInvoiceTotal() {
        var total = 0;
        $(".time-entry-checkbox:checked").each(function () {
            total += parseFloat($(this).data("amount"));
        });
        $(".expense-checkbox:checked").each(function () {
            total += parseFloat($(this).data("amount"));
        });
        $("#invoice-total").val("$" + total.toFixed(2));
    }
});