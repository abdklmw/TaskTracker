namespace TaskTracker.Models.Expense
{
    public class ExpenseRowViewModel
    {
        public Expense? Expense { get; set; }
        public string Parity { get; set; }
        public ExpenseRowViewModel()
        {
            Parity = "odd";
        }

        public ExpenseRowViewModel(Expense expense, int index)
        {
            Parity = index % 2 == 0 ? "even" : "odd";
            Expense = expense;
        }
    }
}