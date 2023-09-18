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
    }
}
