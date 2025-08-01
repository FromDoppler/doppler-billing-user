namespace Doppler.BillingUser.Settings
{
    public class CancellationAccountSettings
    {
        public int NotAchieveMyExpectedGoalsReasonForFreeUser { get; set; }
        public int MyProjectIsOverReasonForFreeUser { get; set; }
        public int ExpensiveForMyBudgetReasonForFreeUser { get; set; }
        public int MissingFeaturesReasonForFreeUser { get; set; }
        public int NotWorkingProperlyReasonForFreeUser { get; set; }
        public int RegisteredByMistakeReasonForFreeUser { get; set; }
        public int OthersReasonForFreeUser { get; set; }

        public int this[string propertyName]
        {
            get { return (int)this.GetType().GetProperty(propertyName).GetValue(this, null); }
            set { this.GetType().GetProperty(propertyName).SetValue(this, value, null); }
        }
    }
}
