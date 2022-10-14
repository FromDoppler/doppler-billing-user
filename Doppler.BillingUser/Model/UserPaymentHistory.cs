using System;

namespace Doppler.BillingUser.Model
{
    public class UserPaymentHistory
    {
        public int IdUserPaymentHistory { get; set; }
        public int IdUser { get; set; }
        public int IdPaymentMethod { get; set; }
        public int IdPlan { get; set; }
        public int? IdBillingCredit { get; set; }
        public string Status { get; set; }
        public string Source { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime Date { get; set; }
    }
}
