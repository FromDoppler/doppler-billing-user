using System;

namespace Doppler.BillingUser.Mappers.AddOn
{
    public class AddOnPlanUser
    {
        public int IdUser { get; set; }
        public int IdAddOnPlan { get; set; }
        public bool Activated { get; set; }
        public int? IdBillingCredit { get; set; }
        public DateTime? ActivationDate { get; set; }
        public DateTime? ExperirationDate { get; set; }
    }
}
