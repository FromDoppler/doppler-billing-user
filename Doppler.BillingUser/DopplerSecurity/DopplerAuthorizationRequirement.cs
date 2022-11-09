using Microsoft.AspNetCore.Authorization;

namespace Doppler.BillingUser.DopplerSecurity
{
    public class DopplerAuthorizationRequirement : IAuthorizationRequirement
    {
        public bool AllowSuperUser { get; init; }
        public bool AllowOwnResource { get; init; }
        public bool AllowProvisoryUser { get; init; }
    }
}
