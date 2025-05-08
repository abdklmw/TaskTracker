namespace TaskTracker.Models
{
    public class ProjectRowViewModel
    {
        public Project? Project { get; set; }
        public string Parity { get; set; }

        public ProjectRowViewModel()
        {
            this.Parity = "odd";
        }

        public ProjectRowViewModel(Project project, int index)
        {
            this.Parity = (index % 2 == 0) ? "even" : "odd";
            this.Project = project;
        }
    }
}
