namespace Doppler.BillingUser.DopplerSecurity
{
    public static class Policies
    {
        public const string ONLY_SUPERUSER = nameof(ONLY_SUPERUSER);
        public const string OWN_RESOURCE_OR_SUPERUSER = nameof(OWN_RESOURCE_OR_SUPERUSER);
        public const string PROVISORY_USER_OR_SUPER_USER = nameof(PROVISORY_USER_OR_SUPER_USER);
        public const string OWN_RESOURCE_OR_SUPERUSER_OR_PROVISORY_USER = nameof(OWN_RESOURCE_OR_SUPERUSER_OR_PROVISORY_USER);
    }
}
