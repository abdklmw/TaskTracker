namespace TaskTracker.Models
{
    public class ExpenseRowViewModel
    {
        public Expense? Expense { get; set; }
        public string Parity { get; set; }
        public ExpenseRowViewModel()
        {
            this.Parity = "odd";
        }

        public ExpenseRowViewModel(Expense expense, int index)
        {
            this.Parity = (index % 2 == 0) ? "even" : "odd";
            this.Expense = expense;
        }
    }
}