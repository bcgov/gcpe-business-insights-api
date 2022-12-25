using Microsoft.Extensions.Hosting;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gcpe.Hub.BusinessInsights.API.Entities
{
    public class Url
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Href { get; set; }

        [ForeignKey("TranslationItemId")]
        public TranslationItem TranslationItem { get; set; }
        public int TranslationItemId { get; set; }
        public DateTimeOffset PublishDateTime { get; set; }
    }
}
