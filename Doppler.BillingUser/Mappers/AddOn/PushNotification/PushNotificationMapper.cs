using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;
using System;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Validators;
using System.Collections.Generic;
using System.Linq;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.Mappers.BillingCredit.AddOns;
using Doppler.BillingUser.Mappers.OnSitePlan;
using Doppler.BillingUser.ExternalServices.FirstData;

namespace Doppler.BillingUser.Mappers.AddOn.PushNotification
{
    public class PushNotificationMapper(
        IPushNotificationPlanRepository pushNotificationPlanRepository,
        IBillingRepository billingRepository,
        IUserRepository userRepository,
        IClientManagerRepository clientManagerRepository,
        IUserAddOnRepository userAddOnRepository,
        IPushNotificationPlanUserRepository pushNotificationPlanUserRepository,
        IBillingCreditMapper billingCreditMapper) : IAddOnMapper
    {
        private readonly IPushNotificationPlanRepository pushNotificationPlanRepository = pushNotificationPlanRepository;
        private readonly IPushNotificationPlanUserRepository pushNotificationPlanUserRepository = pushNotificationPlanUserRepository;
        private readonly IBillingRepository billingRepository = billingRepository;
        private readonly IUserRepository userRepository = userRepository;
        private readonly IClientManagerRepository clientManagerRepository = clientManagerRepository;
        private readonly IUserAddOnRepository userAddOnRepository = userAddOnRepository;
        private readonly IBillingCreditMapper billingCreditMapper = billingCreditMapper;

        private readonly List<PaymentMethodEnum> AllowedPaymentMethodsForBilling =
        [
            PaymentMethodEnum.CC,
            PaymentMethodEnum.TRANSF,
            PaymentMethodEnum.MP,
            PaymentMethodEnum.DA
        ];

        private readonly List<CountryEnum> AllowedCountriesForTransfer =
        [
            CountryEnum.Colombia,
            CountryEnum.Mexico,
            CountryEnum.Argentina
        ];

        public Task<int> CreateAddOnPlanUserAsync(AddOnPlanUser addOnPlanUser)
        {
            var pushNotificationPlanUser = new PushNotificationPlanUser
            {
                Activated = addOnPlanUser.Activated,
                ActivationDate = addOnPlanUser.ActivationDate,
                ExperirationDate = addOnPlanUser.ExperirationDate,
                IdBillingCredit = addOnPlanUser.IdBillingCredit,
                IdPushNotificationPlan = addOnPlanUser.IdAddOnPlan,
                IdUser = addOnPlanUser.IdUser
            };

            return billingRepository.CreatePushNotificationPlanUserAsync(pushNotificationPlanUser);
        }

        public async Task<AddOnPlan> GetAddOnFreePlanAsync()
        {
            var pushNotificationPlan = await pushNotificationPlanRepository.GetFreePlan();
            return new AddOnPlan
            {
                Description = pushNotificationPlan.Description,
                Fee = pushNotificationPlan.Fee,
                FreeDays = pushNotificationPlan.FreeDays,
                PlanId = pushNotificationPlan.IdPushNotificationPlan,
                Quantity = pushNotificationPlan.Quantity,
            };
        }

        public AddOnPlanUser MapToPlanUser(int userId, int addOnPlanId, int? billingCreditId)
        {
            DateTime now = DateTime.UtcNow;

            return new AddOnPlanUser
            {
                IdUser = userId,
                Activated = true,
                IdBillingCredit = billingCreditId,
                IdAddOnPlan = addOnPlanId,
                ActivationDate = now,
            };
        }

        public async Task<ValidationResult> CanProceedToBuy(BuyAddOnPlan buyAddOnPlan, int userId, UserBillingInformation userBillingInformation, AccountTypeEnum accountType)
        {
            var userType = accountType == AccountTypeEnum.User ? "REG" : "CM";
            if (userBillingInformation == null)
            {
                var messageError = $"{userType} - Failed at buy a push notifiation plan, Invalid user";
                return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "Invalid user", MessageError = messageError } };
            }

            if (userBillingInformation.IsCancelated)
            {
                var messageError = $"{userType} - Failed at buy a push notifiation plan for user {userBillingInformation.Email}, Canceled user";
                return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "Canceled user", MessageError = messageError } };
            }

            if (userBillingInformation.IdBillingCountry == 0)
            {
                var messageError = $"{userType} - Failed at buy a push notifiation plan for user {userBillingInformation.Email}, Invalid country";
                return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "Invalid country", MessageError = messageError } };
            }

            if (!AllowedPaymentMethodsForBilling.Any(p => p == userBillingInformation.PaymentMethod))
            {
                var messageError = $"{userType} - Failed at buy a push notifiation plan for user {userBillingInformation.Email}, Invalid payment method {userBillingInformation.PaymentMethod}";
                return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "Invalid payment method", MessageError = messageError } };
            }

            if (userBillingInformation.PaymentMethod == PaymentMethodEnum.TRANSF && !AllowedCountriesForTransfer.Any(p => (int)p == userBillingInformation.IdBillingCountry))
            {
                var messageErrorTransference = $"{userType} - Failed at buy a push notifiation plan for user {userBillingInformation.Email}, payment method {userBillingInformation.PaymentMethod} it's only supported for {AllowedCountriesForTransfer.Select(p => p)}";
                return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "Invalid payment method", MessageError = messageErrorTransference } };
            }

            var currentBillingCredit = await billingRepository.GetCurrentBillingCredit(userId);
            if (currentBillingCredit == null)
            {
                var messageErrorTransference = $"{userType} - Failed at buy a push notifiation plan for user {userBillingInformation.Email}. The user has not an active marketing plan";
                return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "Invalid marketing plan", MessageError = messageErrorTransference } };
            }

            var newPushNotificationPlan = await pushNotificationPlanRepository.GetById(buyAddOnPlan.PlanId);
            if (newPushNotificationPlan == null)
            {
                var messageError = $"{userType} - Failed at buy a push notifiation plan for user {userBillingInformation.Email}. The plan {buyAddOnPlan.PlanId} not exist";
                return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "Invalid push notifiation plan", MessageError = messageError } };
            }

            CreditCard encryptedCreditCard;

            if (buyAddOnPlan.Total.GetValueOrDefault() > 0 &&
                (userBillingInformation.PaymentMethod == PaymentMethodEnum.CC || userBillingInformation.PaymentMethod == PaymentMethodEnum.MP))
            {
                if (accountType == AccountTypeEnum.User)
                {
                    encryptedCreditCard = await userRepository.GetEncryptedCreditCard(userBillingInformation.Email);
                }
                else
                {
                    encryptedCreditCard = await clientManagerRepository.GetEncryptedCreditCard(userBillingInformation.IdUser);
                }

                if (encryptedCreditCard == null)
                {
                    var messageError = $"Failed at buy a push notifiation plan for user {userBillingInformation.Email}, missing credit card information";
                    return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "User credit card missing", MessageError = messageError } };
                }
            }

            return new ValidationResult { IsValid = true };
        }

        public async Task ProceedToBuy(User user, BuyAddOnPlan buyAddOnPlan, UserBillingInformation userBillingInformation, Model.BillingCredit currentBillingCredit, CreditCardPayment payment, PlanAmountDetails amountDetails, UserBillingInformation userOrClientManagerBillingInformation, AccountTypeEnum accountType)
        {
            var currentPushNotificationPlan = await pushNotificationPlanUserRepository.GetCurrentPlan(user.Email);
            var pushNotificationPlan = await pushNotificationPlanRepository.GetById(buyAddOnPlan.PlanId);

            if (currentPushNotificationPlan == null || (currentPushNotificationPlan != null && currentPushNotificationPlan.IdPlan != pushNotificationPlan.IdPushNotificationPlan))
            {
                var billingCreditType = userOrClientManagerBillingInformation.PaymentMethod == PaymentMethodEnum.CC ? BillingCreditTypeEnum.PushNotification_Buyed_CC : BillingCreditTypeEnum.PushNotification_Request;
                if (currentPushNotificationPlan != null && currentPushNotificationPlan.Quantity > pushNotificationPlan.Quantity)
                {
                    billingCreditType = BillingCreditTypeEnum.Downgrade_Between_OnSite;
                }

                var total = pushNotificationPlan.Fee;
                var billingCreditAgreement = await billingCreditMapper.MapToBillingCreditAgreement(total, userBillingInformation, currentBillingCredit, payment, billingCreditType);
                var billingCreditId = await billingRepository.CreateBillingCreditAsync(billingCreditAgreement);

                var pushNotificationPlanUser = new PushNotificationPlanUser
                {
                    IdUser = user.IdUser,
                    Activated = true,
                    IdBillingCredit = billingCreditId,
                    IdPushNotificationPlan = pushNotificationPlan.IdPushNotificationPlan,
                    ActivationDate = DateTime.UtcNow,
                };

                await billingRepository.CreatePushNotificationPlanUserAsync(pushNotificationPlanUser);

                /* Save current billing credit in the UserAddOn table */
                await userAddOnRepository.SaveCurrentBillingCreditByUserIdAndAddOnTypeAsync(user.IdUser, (int)AddOnType.PushNotification, billingCreditId);
            }

            //Send notifications
            //var planDiscountInformation = await billingRepository.GetPlanDiscountInformation(currentBillingCredit.IdDiscountPlan ?? 0);
            //SendOnSiteNotifications(userOrClientManagerBillingInformation.Email, userOrClientManagerBillingInformation, onSitePlan, currentOnSitePlan, payment, planDiscountInformation, amountDetails, accountType);
        }

        public Task<SapBillingDto> MapAddOnBillingToSapAsync(SapSettings sapSettings, User user, BuyAddOnPlan buyAddOnPlan, string cardNumber, string holderName, Model.BillingCredit billingCredit, string authorizationNumber, int invoiceId, UserBillingInformation userOrClientManagerBillingInformation, AccountTypeEnum accountType)
        {
            throw new NotImplementedException();
        }
    }
}
