using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.AccountPlansApi;
using Doppler.BillingUser.ExternalServices.Clover;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.ExternalServices.Slack;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Mappers.BillingCredit.AddOns;
using Doppler.BillingUser.Mappers.OnSitePlan;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Services;
using Doppler.BillingUser.Utils;
using Doppler.BillingUser.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.AddOn.OnSite
{
    public class OnSiteMapper(
        IOnSitePlanRepository onSitePlanRepository,
        IBillingRepository billingRepository,
        IUserRepository userRepository,
        IClientManagerRepository clientManagerRepository,
        IUserAddOnRepository userAddOnRepository,
        IOnSitePlanUserRepository onSitePlanUserRepository,
        IEmailTemplatesService emailTemplatesService,
        IBillingCreditMapper billingCreditMapper) : IAddOnMapper
    {
        private readonly IOnSitePlanRepository onSitePlanRepository = onSitePlanRepository;
        private readonly IBillingRepository billingRepository = billingRepository;
        private readonly IUserRepository userRepository = userRepository;
        private readonly IClientManagerRepository clientManagerRepository = clientManagerRepository;
        private readonly IUserAddOnRepository userAddOnRepository = userAddOnRepository;
        private readonly IOnSitePlanUserRepository onSitePlanUserRepository = onSitePlanUserRepository;
        private readonly IBillingCreditMapper billingCreditMapper = billingCreditMapper;
        private readonly IEmailTemplatesService emailTemplatesService = emailTemplatesService;

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
            var onSitePlanUser = new OnSitePlanUser
            {
                Activated = addOnPlanUser.Activated,
                ActivationDate = addOnPlanUser.ActivationDate,
                ExperirationDate = addOnPlanUser.ExperirationDate,
                IdBillingCredit = addOnPlanUser.IdBillingCredit,
                IdOnSitePlan = addOnPlanUser.IdAddOnPlan,
                IdUser = addOnPlanUser.IdUser
            };

            return billingRepository.CreateOnSitePlanUserAsync(onSitePlanUser);
        }

        public async Task<AddOnPlan> GetAddOnFreePlanAsync()
        {
            var onSitePlan = await onSitePlanRepository.GetFreeOnSitePlan();
            return new AddOnPlan
            {
                Description = onSitePlan.Description,
                Fee = onSitePlan.Fee,
                FreeDays = onSitePlan.FreeDays,
                PlanId = onSitePlan.IdOnSitePlan,
                Quantity = onSitePlan.PrintQty,
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
                var messageError = $"{userType} - Failed at buy a OnSite plan, Invalid user";
                return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "Invalid user", MessageError = messageError } };
            }

            if (userBillingInformation.IsCancelated)
            {
                var messageError = $"{userType} - Failed at buy a OnSite plan for user {userBillingInformation.Email}, Canceled user";
                return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "Canceled user", MessageError = messageError } };
            }

            if (userBillingInformation.IdBillingCountry == 0)
            {
                var messageError = $"{userType} - Failed at buy a OnSite plan for user {userBillingInformation.Email}, Invalid country";
                return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "Invalid country", MessageError = messageError } };
            }

            if (!AllowedPaymentMethodsForBilling.Any(p => p == userBillingInformation.PaymentMethod))
            {
                var messageError = $"{userType} - Failed at buy a OnSite plan for user {userBillingInformation.Email}, Invalid payment method {userBillingInformation.PaymentMethod}";
                return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "Invalid payment method", MessageError = messageError } };
            }

            if (userBillingInformation.PaymentMethod == PaymentMethodEnum.TRANSF && !AllowedCountriesForTransfer.Any(p => (int)p == userBillingInformation.IdBillingCountry))
            {
                var messageErrorTransference = $"{userType} - Failed at buy a OnSite plan for user {userBillingInformation.Email}, payment method {userBillingInformation.PaymentMethod} it's only supported for {AllowedCountriesForTransfer.Select(p => p)}";
                return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "Invalid payment method", MessageError = messageErrorTransference } };
            }

            var currentBillingCredit = await billingRepository.GetCurrentBillingCredit(userId);
            if (currentBillingCredit == null)
            {
                var messageErrorTransference = $"{userType} - Failed at buy a OnSite plan for user {userBillingInformation.Email}. The user has not an active marketing plan";
                return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "Invalid marketing plan", MessageError = messageErrorTransference } };
            }

            var newOnSitePlan = await onSitePlanRepository.GetById(buyAddOnPlan.PlanId);
            if (newOnSitePlan == null)
            {
                var messageError = $"{userType} - Failed at buy a OnSite plan for user {userBillingInformation.Email}. The plan {buyAddOnPlan.PlanId} not exist";
                return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "Invalid OnSite plan", MessageError = messageError } };
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
                    var messageError = $"Failed at buy a OnSite plan for user {userBillingInformation.Email}, missing credit card information";
                    return new ValidationResult { IsValid = false, Error = new ValidationError { ErrorType = "User credit card missing", MessageError = messageError } };
                }
            }

            return new ValidationResult { IsValid = true };
        }

        public async Task ProceedToBuy(
            User user,
            BuyAddOnPlan buyAddOnPlan,
            UserBillingInformation userBillingInformation,
            Model.BillingCredit currentBillingCredit,
            CreditCardPayment payment,
            PlanAmountDetails amountDetails,
            UserBillingInformation userOrClientManagerBillingInformation,
            CurrentPlan currentAddOnPlan,
            AccountTypeEnum accountType)
        {
            var onSitePlan = await onSitePlanRepository.GetById(buyAddOnPlan.PlanId);

            if (currentAddOnPlan == null || (currentAddOnPlan != null && currentAddOnPlan.IdPlan != onSitePlan.IdOnSitePlan))
            {
                var billingCreditType = userOrClientManagerBillingInformation.PaymentMethod == PaymentMethodEnum.CC ? BillingCreditTypeEnum.OnSite_Buyed_CC : BillingCreditTypeEnum.OnSite_Request;
                if (currentAddOnPlan != null && currentAddOnPlan.PrintQty > onSitePlan.PrintQty)
                {
                    billingCreditType = BillingCreditTypeEnum.Downgrade_Between_OnSite;
                }

                var total = onSitePlan.Fee;
                var billingCreditAgreement = await billingCreditMapper.MapToBillingCreditAgreement(total, userBillingInformation, currentBillingCredit, payment, billingCreditType);
                var billingCreditId = await billingRepository.CreateBillingCreditAsync(billingCreditAgreement);

                var onSitePlanUserMapper = GetOnSitePlanUserMapper(userOrClientManagerBillingInformation.PaymentMethod);
                var onSitePlanUser = onSitePlanUserMapper.MapToOnSitePlanUser(user.IdUser, onSitePlan.IdOnSitePlan, billingCreditId);
                await billingRepository.CreateOnSitePlanUserAsync(onSitePlanUser);

                /* Save current billing credit in the UserAddOn table */
                await userAddOnRepository.SaveCurrentBillingCreditByUserIdAndAddOnTypeAsync(user.IdUser, (int)AddOnType.OnSite, billingCreditId);
            }

            //Send notifications
            var planDiscountInformation = await billingRepository.GetPlanDiscountInformation(currentBillingCredit.IdDiscountPlan ?? 0);
            SendOnSiteNotifications(userOrClientManagerBillingInformation.Email, userOrClientManagerBillingInformation, onSitePlan, currentAddOnPlan, payment, planDiscountInformation, amountDetails, accountType);
        }

        public async Task<SapBillingDto> MapAddOnBillingToSapAsync(
            SapSettings sapSettings,
            User user,
            BuyAddOnPlan buyAddOnPlan,
            string cardNumber,
            string holderName,
            Model.BillingCredit billingCredit,
            string authorizationNumber,
            int invoiceId,
            UserBillingInformation userOrClientManagerBillingInformation,
            CurrentPlan currentAddOnPlan,
            AccountTypeEnum accountType)
        {
            var onSitePlan = await onSitePlanRepository.GetById(buyAddOnPlan.PlanId);

            return BillingHelper.MapAddOnBillingToSapAsync(sapSettings,
                            cardNumber,
                            holderName,
                            billingCredit,
                            authorizationNumber,
                            invoiceId,
                            buyAddOnPlan.Total,
                            userOrClientManagerBillingInformation,
                            accountType,
                            onSitePlan.PrintQty,
                            onSitePlan.Fee,
                            currentAddOnPlan,
                            AdditionalServiceTypeEnum.OnSite);
        }


        public async Task<CurrentPlan> GetCurrentPlanAsync(string accountname)
        {
            var currentOnSitePlan = await onSitePlanUserRepository.GetCurrentPlan(accountname);

            return currentOnSitePlan;
        }

        private IOnSitePlanUserMapper GetOnSitePlanUserMapper(PaymentMethodEnum paymentMethod)
        {
            return paymentMethod switch
            {
                PaymentMethodEnum.CC or PaymentMethodEnum.MP or PaymentMethodEnum.TRANSF or PaymentMethodEnum.DA => new OnSitePlanUserMapper(),
                _ => throw new ArgumentException($"The paymentMethod '{paymentMethod}' does not have a mapper."),
            };
        }

        private async void SendOnSiteNotifications(
            string accountname,
            UserBillingInformation user,
            Model.OnSitePlan newPlan,
            CurrentPlan currentPlan,
            CreditCardPayment payment,
            PlanDiscountInformation planDiscountInformation,
            PlanAmountDetails amountDetails,
            AccountTypeEnum accountType)
        {
            User userInformation;
            if (accountType == AccountTypeEnum.User)
            {
                userInformation = await userRepository.GetUserInformation(accountname);
            }
            else
            {
                userInformation = await clientManagerRepository.GetUserInformation(accountname);
            }

            if (currentPlan == null)
            {
                bool isUpgradeApproved = (user.PaymentMethod == PaymentMethodEnum.CC || !BillingHelper.IsUpgradePending(user, null, payment));
                await emailTemplatesService.SendNotificationForUpgradeAddOnPlan(accountname, userInformation, newPlan, user, planDiscountInformation, !isUpgradeApproved, true, AddOnType.OnSite);
            }
            else
            {
                await emailTemplatesService.SendNotificationForUpdateAddOnPlan(accountname, userInformation, newPlan, user, planDiscountInformation, amountDetails, currentPlan, AddOnType.OnSite);
            }
        }
    }
}
