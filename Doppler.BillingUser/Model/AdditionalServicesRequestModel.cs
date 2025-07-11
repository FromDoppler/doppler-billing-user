using System.Collections.Generic;

namespace Doppler.BillingUser.Model
{
    public class AdditionalServicesRequestModel
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string ContactSchedule { get; set; }
        public List<string> Features { get; set; }
        public string SendingVolume { get; set; }
    }
}
