using System;
using System.ComponentModel.DataAnnotations;

namespace FoodLens.Api.DTOs
{
    public class ExportRequestDto
    {
        [Required]
        public string UserId { get; set; } = "default";

        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }
    }
}
