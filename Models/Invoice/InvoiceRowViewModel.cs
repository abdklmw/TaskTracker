namespace TaskTracker.Models.Invoice
{
    public class InvoiceRowViewModel
    {
        public Invoice? Invoice { get; set; }
        public string Parity { get; set; }
        public InvoiceRowViewModel()
        {
            Parity = "odd";
        }
        public InvoiceRowViewModel(Invoice Invoice, int index)
        {
            Parity = index % 2 == 0 ? "even" : "odd";
            this.Invoice = Invoice;
        }
    }
}
