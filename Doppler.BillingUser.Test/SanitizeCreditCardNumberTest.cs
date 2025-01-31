using Doppler.BillingUser.Utils;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class SanitizeCreditCardNumberTest
    {
        [Theory]
        [InlineData("\t\n6011000990139424", "6011000990139424")]
        [InlineData("6011 0009 9013 9424", "6011000990139424")]
        [InlineData("6011\u200B0009\u200E9013\u202E9424", "6011000990139424")]
        public void SanitizeCreditCardNumber_ShouldReturnSanitizedCreditCardNumber(string creditCardNumber, string expectedCardNumber)
        {
            // Act
            var sanitizedCreditCardNumber = CreditCardHelper.SanitizeCreditCardNumber(creditCardNumber);
            // Assert
            Assert.Equal(expectedCardNumber, sanitizedCreditCardNumber);
        }
    }
}
