$(document).ready(function () {
    $("#client-select").change(function () {
        var clientId = $(this).val();
        if (clientId == 0) {
            $("#time-entries-list").empty();
            $("#expenses-list").empty();
            $("#invoice-total").val("$0.00");
            $("#create-form").hide();
            return;
        }

        $.get("/Invoices/GetUnpaidItems", { clientId: clientId }, function (data) {
            $("#create-form").show();
            $("#time-entries-list").empty();
            if (data.timeEntries.length > 0) {
                data.timeEntries.forEach(function (item) {
                    var description = item.description || "No description";
                    var startDate = item.startDateTime ? new Date(item.startDateTime).toLocaleDateString("en-US", { month: "2-digit", day: "2-digit", year: "numeric" }) : "N/A";
                    var dateDisplay = startDate;
                    if (item.endDateTime) {
                        var endDate = new Date(item.endDateTime).toLocaleDateString("en-US", { month: "2-digit", day: "2-digit", year: "numeric" });
                        var startDateObj = new Date(item.startDateTime).toDateString();
                        var endDateObj = new Date(item.endDateTime).toDateString();
                        if (startDateObj !== endDateObj) {
                            dateDisplay = `${startDate} - ${endDate}`;
                        }
                    }
                    var rateSource = item.rateSource || "Settings";
                    $("#time-entries-list").append(
                        `<div class="form-check">
                            <input type="checkbox" class="form-check-input time-entry-checkbox" name="SelectedTimeEntryIDs" value="${item.timeEntryID}" data-amount="${item.totalAmount || 0}" />
                            <label class="form-check-label">
                                ${item.hoursSpent} hours at $<abbr title="Rate from ${rateSource.toLowerCase()}">${(item.hourlyRate || 0).toFixed(2)}</abbr>/hr = $${(item.totalAmount || 0).toFixed(2)}<br>
                                <small><strong>Date:</strong> ${dateDisplay} | <strong>Description:</strong> ${description}</small>
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
                    var description = item.description || "No description";
                    $("#expenses-list").append(
                        `<div class="form-check">
                            <input type="checkbox" class="form-check-input expense-checkbox" name="SelectedExpenseIDs" value="${item.expenseID}" data-amount="${item.totalAmount || 0}" />
                            <label class="form-check-label">
                                ${item.quantity} x $${(item.unitAmount || 0).toFixed(2)} = $${(item.totalAmount || 0).toFixed(2)}<br>
                                <small><strong>Description:</strong> ${description}</small>
                            </label>
                        </div>`
                    );
                });
            } else {
                $("#expenses-list").append("<p>No unpaid expenses found.</p>");
            }

            updateInvoiceTotal();
        }).fail(function (xhr) {
            console.error("Error loading unpaid items:", xhr.responseText);
            $("#time-entries-list").html("<p>Error loading time entries.</p>");
            $("#expenses-list").html("<p>Error loading expenses.</p>");
            $("#invoice-total").val("$0.00");
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
            var amount = parseFloat($(this).data("amount")) || 0;
            total += amount;
        });
        $(".expense-checkbox:checked").each(function () {
            var amount = parseFloat($(this).data("amount")) || 0;
            total += amount;
        });
        $("#invoice-total").val("$" + total.toFixed(2));
    }
});