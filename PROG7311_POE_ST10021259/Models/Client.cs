using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;

namespace PROG7311_POE_ST10021259.Models
{
    public class Client
    {
        public int Id { get; set; }

        //gather the client data

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(150)]
        [Display(Name = "Client Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact details are required.")]
        [StringLength(250)]
        [Display(Name = "Contact Details")]
        public string ContactDetails { get; set; } = string.Empty;

        [Required(ErrorMessage = "Region is required.")]
        [StringLength(100)]
        public string Region { get; set; } = string.Empty;

        // Navigation property
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}
