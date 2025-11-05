using System;

namespace Doppler.BillingUser.Model
{
    public class ImportedBillingDetail
    {
        public int IdImportedBillingDetail { get; set; }
        public string Month { get; set; }
        public string ExtraMonth { get; set; }
        public decimal ExtraAmount { get; set; }
        public int Extra { get; set; }
        public decimal Amount { get; set; }
        public decimal? LandingsAmount { get; set; }
        public decimal? ConversationsAmount { get; set; }
        public decimal? ConversationsExtraAmount { get; set; }
        public string ConversationsExtraMonth { get; set; }
        public int? ConversationsExtra { get; set; }
        public decimal? PrintsAmount { get; set; }
        public decimal? PrintsExtraAmount { get; set; }
        public string PrintsExtraMonth { get; set; }
        public int? PrintsExtra { get; set; }
        public decimal? PushNotificationsAmount { get; set; }
        public decimal? PushNotificationsExtraAmount { get; set; }
        public string PushNotificationsExtraMonth { get; set; }
        public int? PushNotificationsExtra { get; set; }
    }
}
