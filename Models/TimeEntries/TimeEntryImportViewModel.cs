using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Models.TimeEntries
{
    public class TimeEntryImportViewModel
    {
        [Required(ErrorMessage = "Please select a client.")]
        public int ClientID { get; set; }

        [Required(ErrorMessage = "Please select a project.")]
        public int ProjectID { get; set; }

        [Required(ErrorMessage = "Please upload a CSV file.")]
        [DataType(DataType.Upload)]
        public IFormFile CsvFile { get; set; }

        public List<SelectListItem> Clients { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Projects { get; set; } = new List<SelectListItem>();
    }
}