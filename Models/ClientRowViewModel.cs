namespace TaskTracker.Models
{
    public class ClientRowViewModel
    {
        public Client? Client { get; set; }
        public string Parity { get; set; }
        public ClientRowViewModel()
        {
            this.Parity = "odd";
        }
        public ClientRowViewModel(Client client, int index)
        {
            this.Parity = (index % 2 == 0) ? "even" : "odd";
            this.Client = client;
        }
    }
}
