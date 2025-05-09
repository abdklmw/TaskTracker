using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
namespace TaskTracker.Models
{
    public class InvoiceProduct : InvoiceItemBase
    {
        private static readonly Dictionary<string, string> FrequencyMap = new()
        {
            { "Monthly", "AddMonths" },
            { "Yearly", "AddYears" }
        };
        private static readonly HashSet<string> ValidMethodNames = new(FrequencyMap.Values);
        [Required]
        public int ProductID { get; set; }
        [Required]
        public DateOnly ProductInvoiceDate { get; set; }
        [Description("Recurring charge")]
        public bool IsRecurring { get; set; } = false;
        private string _recurringFrequency = FrequencyMap["Yearly"];
        [Description("Recurring charge frequency")]
        public string RecurringFrequency
        {
            get
            {
                var userFriendlyName = FrequencyMap.FirstOrDefault(kvp => kvp.Value == _recurringFrequency).Key;
                return userFriendlyName ?? _recurringFrequency;
            }
            set
            {
                if (FrequencyMap.TryGetValue(value, out var methodName))
                {
                    _recurringFrequency = methodName;
                }
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
        public virtual required Product Product { get; set; }
        public DateOnly GetNextChargeDate(int intervals = 1)
        {
            if (!IsRecurring)
            {
                return ProductInvoiceDate;
            }
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