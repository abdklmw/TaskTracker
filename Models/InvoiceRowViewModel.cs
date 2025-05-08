namespace TaskTracker.Models
{
    public class InvoiceRowViewModel
    {
        public Invoice? Invoice { get; set; }
        public string Parity { get; set; }
        public InvoiceRowViewModel()
        {
            this.Parity = "odd";
        }
        public InvoiceRowViewModel(Invoice Invoice, int index)
        {
            this.Parity = (index % 2 == 0) ? "even" : "odd";
            this.Invoice = Invoice;
        }
    }
}
