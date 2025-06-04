namespace TaskTracker.Models.Product
{
    public class ProductRowViewModel
    {
        public Product? Product { get; set; }
        public string Parity { get; set; }

        public ProductRowViewModel()
        {
            Parity = "odd";
        }

        public ProductRowViewModel(Product product, int index)
        {
            Parity = index % 2 == 0 ? "even" : "odd";
            Product = product;
        }
    }
}