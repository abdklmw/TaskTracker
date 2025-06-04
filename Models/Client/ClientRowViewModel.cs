namespace TaskTracker.Models.Client
{
    public class ClientRowViewModel
    {
        public Client? Client { get; set; }
        public string Parity { get; set; }
        public ClientRowViewModel()
        {
            Parity = "odd";
        }
        public ClientRowViewModel(Client client, int index)
        {
            Parity = index % 2 == 0 ? "even" : "odd";
            Client = client;
        }
    }
}
