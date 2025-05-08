using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskTracker.Models
{
    public enum InvoiceStatus
    {
        Draft,
        Sent,
        Paid,
        Void
    }

    public class Invoice
    {
        public int InvoiceID { get; set; }

        [Required]
        public int ClientID { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime InvoiceDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime? InvoiceSentDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime? PaidDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public InvoiceStatus Status { get; set; }

        public virtual required Client Client { get; set; }
        public virtual ICollection<InvoiceTimeEntry> InvoiceTimeEntries { get; set; } = new List<InvoiceTimeEntry>();
        public virtual ICollection<InvoiceProduct> InvoiceProducts { get; set; } = new List<InvoiceProduct>();
    }
}