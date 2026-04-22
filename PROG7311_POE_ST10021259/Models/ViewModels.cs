using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace PROG7311_POE_ST10021259.Models
{
    public class ServiceRequestCreateViewModel
    {
        //user input fields
        [Required]
        [Display(Name = "Contract")]
        public int ContractId { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Cost (USD)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cost must be greater than zero.")]
        public decimal CostUsd { get; set; }

        // show what the API has done
        [Display(Name = "Current USD → ZAR Rate")]
        public decimal ExchangeRate { get; set; }

        [Display(Name = "Estimated Cost (ZAR)")]
        public decimal CostZar { get; set; }

        public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;

        // Dropdown data
        public IEnumerable<SelectListItem> Contracts { get; set; } = new List<SelectListItem>();
    }

    //contract filter for status and between dates
    public class ContractFilterViewModel
    {
        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        public ContractStatus? Status { get; set; }

        public IEnumerable<Contract> Results { get; set; } = new List<Contract>();
    }
}
