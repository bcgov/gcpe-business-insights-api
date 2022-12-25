using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;

namespace Gcpe.Hub.BusinessInsights.API.Entities
{
    public class TranslationItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Key { get; set; }

        [Required]
        [MaxLength(50)]
        public string Ministry { get; set; }

        public DateTimeOffset PublishDateTime { get; set; }
        public ICollection<Url> Urls { get; set; } = new List<Url>();
    }
}
