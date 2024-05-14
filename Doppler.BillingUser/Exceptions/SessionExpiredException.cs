using System;

namespace Doppler.BillingUser.Exceptions
{
    public class SessionExpiredException : Exception
    {
        public SessionExpiredException(): base("Session expired")
        {
        }
    }
}
