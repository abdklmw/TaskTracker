﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskTracker.Models.Invoice
{
    public enum InvoiceStatus
    {
        Draft,
        Sent,
        Paid,
        Canceled
    }

    public class Invoice
    {
        public int InvoiceID { get; set; }
        [Required]
        public int ClientID { get; set; }
        [Required]
        [Column(TypeName = "date")]
        [Description("Date the invoice was created")]
        public DateTime InvoiceDate { get; set; }
        [Column(TypeName = "date")]
        [Description("Date the invoice was sent")]
        public DateTime? InvoiceSentDate { get; set; }
        [Column(TypeName = "date")]
        [Description("Date the invoice was paid")]
        public DateTime? PaidDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        [Required]
        public InvoiceStatus Status { get; set; }
        public virtual Client.Client? Client { get; set; }
        public virtual ICollection<InvoiceTimeEntry> InvoiceTimeEntries { get; set; } = new List<InvoiceTimeEntry>();
        public virtual ICollection<InvoiceExpense> InvoiceExpenses { get; set; } = new List<InvoiceExpense>();
    }
}