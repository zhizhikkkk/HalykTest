using System;
using System.ComponentModel.DataAnnotations;

namespace GameStore.Models
{
    public class Currency
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [StringLength(60)]
        public string TITLE { get; set; }

        [Required]
        [StringLength(3)]
        public string CODE { get; set; }

        [Required]
        public decimal VALUE { get; set; }

        [Required]
        public DateTime A_DATE { get; set; }
    }
}