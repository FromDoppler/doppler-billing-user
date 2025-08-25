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
        public int NotAchieveMyExpectedGoalsReasonForPaidUser { get; set; }
        public int MyProjectIsOverReasonForPaidUser { get; set; }
        public int ExpensiveForMyBudgetReasonForPaidUser { get; set; }
        public int MissingFeaturesReasonForPaidUser { get; set; }
        public int NotWorkingProperlyReasonForPaidUser { get; set; }
        public int RegisteredByMistakeReasonPaidUser { get; set; }
        public int OthersReasonForPaidUser { get; set; }

        public int this[string propertyName]
        {
            get { return (int)this.GetType().GetProperty(propertyName).GetValue(this, null); }
            set { this.GetType().GetProperty(propertyName).SetValue(this, value, null); }
        }
    }
}
