namespace TaskTracker.Models
{
    public class ProductRowViewModel
    {
        public Product? Product { get; set; }
        public string Parity { get; set; }

        public ProductRowViewModel()
        {
            this.Parity = "odd";
        }

        public ProductRowViewModel(Product product, int index)
        {
            this.Parity = (index % 2 == 0) ? "even" : "odd";
            this.Product = product;
        }
    }
}