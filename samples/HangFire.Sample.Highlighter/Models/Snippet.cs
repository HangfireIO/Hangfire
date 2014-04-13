using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace HangFire.Sample.Highlighter.Models
{
    public class Snippet
    {
        public int Id { get; set; }

        [Display(Name = "C# source")]
        [Required, AllowHtml]
        public string Source { get; set; }

        public string HighlightedSource { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? HighlightedAt { get; set; }
    }
}