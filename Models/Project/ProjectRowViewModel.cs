namespace TaskTracker.Models.Project
{
    public class ProjectRowViewModel
    {
        public Project? Project { get; set; }
        public string Parity { get; set; }

        public ProjectRowViewModel()
        {
            Parity = "odd";
        }

        public ProjectRowViewModel(Project project, int index)
        {
            Parity = index % 2 == 0 ? "even" : "odd";
            Project = project;
        }
    }
}
