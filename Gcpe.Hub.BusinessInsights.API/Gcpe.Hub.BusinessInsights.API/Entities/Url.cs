﻿using System;
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

        [ForeignKey("NewsReleaseItemId")]
        public int NewsReleaseItemId { get; set; }
        public NewsReleaseItem NewsReleaseItem { get; set; }
        public DateTimeOffset PublishDateTime { get; set; }
    }
}
