using System.ComponentModel.DataAnnotations;

namespace Doppler.BillingUser.ApiModels
{
    public class ReprocessByTransferUserData
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string UserLastname { get; set; }
        [Required]
        public string UserEmail { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
    }
}
