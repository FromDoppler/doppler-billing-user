using System.ComponentModel;

namespace Doppler.BillingUser.Enums
{
    public enum UserAccountCancellationReasonEnum
    {
        [Description("expensiveForMyBudget")]
        ExpensiveForMyBudget = 8,
        [Description("missingFeatures")]
        MissingFeatures = 9,
        [Description("notWorkingProperly")]
        NotWorkingProperly = 10,
        [Description("myProjectIsOver")]
        MyProjectIsOver = 11,
        [Description("notAchieveMyExpectedGoals")]
        NotAchieveMyExpectedGoals = 12,
        [Description("others")]
        Others = 13,
        [Description("registeredByMistake")]
        RegisteredByMistake = 14
    }
}
