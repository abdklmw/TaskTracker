using System.Collections.Generic;

namespace TaskTracker.Models
{
    public interface IPaginationViewModel
    {
        int CurrentPage { get; set; }
        int TotalPages { get; set; }
        int TotalRecords { get; set; }
        int RecordLimit { get; set; }
        IDictionary<string, string> RouteValues { get; set; }
    }
}