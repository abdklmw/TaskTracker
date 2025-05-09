using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace TaskTracker.Models
{
    public class InvoiceProduct
    {
        // Dictionary mapping user-friendly frequency names to method names
        private static readonly Dictionary<string, string> FrequencyMap = new()
        {
            { "Monthly", "AddMonths" },
            { "Yearly", "AddYears" }
        };

        // List of valid method names for validation
        private static readonly HashSet<string> ValidMethodNames = new(FrequencyMap.Values);

        [Required]
        public int InvoiceID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        public DateOnly ProductInvoiceDate { get; set; }

        [Description("Recurring charge")]
        public bool IsRecurring { get; set; } = false;

        private string _recurringFrequency = FrequencyMap["Yearly"]; // Default to AddYears

        [Description("Recurring charge frequency")]
        public string RecurringFrequency
        {
            get
            {
                // Return the user-friendly name
                var userFriendlyName = FrequencyMap.FirstOrDefault(kvp => kvp.Value == _recurringFrequency).Key;
                return userFriendlyName ?? _recurringFrequency; // Fallback to method name if not found
            }
            set
            {
                // Check if value is a user-friendly name
                if (FrequencyMap.TryGetValue(value, out var methodName))
                {
                    _recurringFrequency = methodName; // Store method name
                }
                // Allow method name directly (for backward compatibility or database access)
                else if (ValidMethodNames.Contains(value))
                {
                    _recurringFrequency = value;
                }
                else
                {
                    throw new ArgumentException($"Invalid recurring frequency. Must be one of: {string.Join(", ", FrequencyMap.Keys)} or {string.Join(", ", ValidMethodNames)}");
                }
            }
        }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public int Quantity { get; set; }

        public decimal TotalAmount
        {
            get
            {
                return Quantity * Product.UnitPrice;
            }
        }

        public virtual required Invoice Invoice { get; set; }
        public virtual required Product Product { get; set; }

        /// <summary>
        /// Calculates the next charge date based on ProductInvoiceDate and RecurringFrequency.
        /// </summary>
        /// <param name="intervals">Number of frequency intervals to advance (default is 1).</param>
        /// <returns>The next charge date, or ProductInvoiceDate if not recurring.</returns>
        public DateOnly GetNextChargeDate(int intervals = 1)
        {
            if (!IsRecurring)
            {
                return ProductInvoiceDate;
            }

            // Use reflection to invoke the method named in _recurringFrequency
            MethodInfo? method = typeof(DateOnly).GetMethod(_recurringFrequency, new[] { typeof(int) });
            if (method == null || method.ReturnType != typeof(DateOnly))
            {
                throw new InvalidOperationException($"Invalid frequency method: {_recurringFrequency}");
            }

            try
            {
                return (DateOnly)method.Invoke(ProductInvoiceDate, new object[] { intervals })!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to invoke frequency method: {_recurringFrequency}", ex);
            }
        }
    }
}