using Dapper;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.Clover;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class BillingRepository : IBillingRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IEncryptionService _encryptionService;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ISapService _sapService;
        private readonly IOptions<CloverSettings> _cloverSettings;
        private readonly ICloverService _cloverService;

        private const int InvoiceBillingTypeQBL = 1;
        private const int UserAccountType = 1;
        private const string AccountingEntryStatusApproved = "Approved";
        private const string AccountingEntryTypeDescriptionInvoice = "Invoice";
        private const string AccountingEntryTypeDescriptionCCPayment = "CC Payment";
        private const string AccountEntryTypeInvoice = "I";
        private const string AccountEntryTypePayment = "P";
        private const string PaymentEntryTypePayment = "P";
        private const int CurrencyTypeUsd = 0;
        private const int BillingCreditTypeUpgradeRequest = 1;
        private const int MexicoIva = 16;
        private const int ArgentinaIva = 21;
        private const string FinalConsumer = "CF";

        public BillingRepository(IDatabaseConnectionFactory connectionFactory,
            IEncryptionService encryptionService,
            IPaymentGateway paymentGateway,
            ISapService sapService,
            IOptions<CloverSettings> cloverSettings,
            ICloverService cloverService)
        {
            _connectionFactory = connectionFactory;
            _encryptionService = encryptionService;
            _paymentGateway = paymentGateway;
            _sapService = sapService;
            _cloverSettings = cloverSettings;
            _cloverService = cloverService;
        }
        public async Task<BillingInformation> GetBillingInformation(string email)
        {
            using var connection = _connectionFactory.GetConnection();

            var results = await connection.QueryAsync<BillingInformation>(@"
SELECT
    U.BillingFirstName AS Firstname,
    U.BillingLastName AS Lastname,
    U.BillingAddress AS Address,
    U.BillingCity AS City,
    isnull(S.StateCode, '') AS Province,
    isnull(CO.Code, '') AS Country,
    U.BillingZip AS ZipCode,
    U.BillingPhone AS Phone
FROM
    [User] U
    LEFT JOIN [State] S ON U.IdBillingState = S.IdState
    LEFT JOIN [Country] CO ON S.IdCountry = CO.IdCountry
WHERE
    U.Email = @email",
                new { email });
            return results.FirstOrDefault();
        }

        public async Task UpdateBillingInformation(string accountName, BillingInformation billingInformation)
        {
            using var connection = _connectionFactory.GetConnection();

            await connection.ExecuteAsync(@"
UPDATE [User] SET
    [BillingFirstName] = @firstname,
    [BillingLastName] = @lastname,
    [BillingAddress] = @address,
    [BillingCity] = @city,
    [IdBillingState] = (SELECT IdState FROM [State] WHERE StateCode = @idBillingState),
    [BillingPhone] = @phoneNumber,
    [BillingZip] = @zipCode
WHERE
    Email = @email;",
                new
                {
                    @firstname = billingInformation.Firstname,
                    @lastname = billingInformation.Lastname,
                    @address = billingInformation.Address,
                    @city = billingInformation.City,
                    @idBillingState = billingInformation.Province,
                    @phoneNumber = billingInformation.Phone,
                    @zipCode = billingInformation.ZipCode,
                    @email = accountName
                });
        }

        public async Task<PaymentMethod> GetCurrentPaymentMethod(string username)
        {
            using var connection = _connectionFactory.GetConnection();

            var result = await connection.QueryFirstOrDefaultAsync<PaymentMethod>(@"

SELECT
    U.CCHolderFullName,
    U.CCNumber,
    U.CCExpMonth,
    U.CCExpYear,
    U.CCVerification,
    C.[Description] AS CCType,
    P.PaymentMethodName AS PaymentMethodName,
    U.RazonSocial,
    U.IdConsumerType,
    U.CUIT as IdentificationNumber,
    U.ResponsableIVA,
    U.IdCCType,
    U.CFDIUse AS UseCFDI,
    U.PaymentType,
    U.PaymentWay,
    U.BankAccount,
    U.BankName,
    U.TaxRegime,
    U.TaxCertificateUrl
FROM
    [User] U
LEFT JOIN
    [CreditCardTypes] C ON C.IdCCType = U.IdCCType
LEFT JOIN
    [PaymentMethods] P ON P.IdPaymentMethod = U.PaymentMethod
WHERE
    U.Email = @email;",
                new
                {
                    @email = username
                });

            result.IdConsumerType = ConsumerTypeHelper.GetConsumerType(result.IdConsumerType);

            if (result is not { PaymentMethodName: "CC" or "MP" })
                return result;

            result.CCHolderFullName = _encryptionService.DecryptAES256(result.CCHolderFullName);
            result.CCNumber = CreditCardHelper.ObfuscateNumber(_encryptionService.DecryptAES256(result.CCNumber));
            result.CCVerification = CreditCardHelper.ObfuscateVerificationCode(_encryptionService.DecryptAES256(result.CCVerification));

            return result;
        }

        public async Task<EmailRecipients> GetInvoiceRecipients(string accountName)
        {
            using var connection = _connectionFactory.GetConnection();

            var user = await connection.QueryFirstOrDefaultAsync<User>(@"
SELECT
    U.BillingEmails
FROM
    [User] U
WHERE
    U.Email = @email;",
                new
                {
                    @email = accountName
                });

            if (user is null) return null;

            return new EmailRecipients
            {
                Recipients = string.IsNullOrEmpty(user.BillingEmails) ? Array.Empty<string>() : user.BillingEmails.Replace(" ", string.Empty).Split(',')
            };
        }

        public async Task UpdateInvoiceRecipients(string accountName, string[] emailRecipients, int planId)
        {
            using var connection = _connectionFactory.GetConnection();

            await connection.ExecuteAsync(@"
UPDATE
    [User]
SET
    BillingEmails = @emailRecipients
WHERE
    Email = @email;",
                new
                {
                    @email = accountName,
                    @emailRecipients = string.Join(",", emailRecipients)
                });

            await SendUserDataToSap(accountName, planId);
        }

        public async Task<CurrentPlan> GetCurrentPlan(string accountName)
        {
            using var connection = _connectionFactory.GetConnection();

            var currentPlan = await connection.QueryFirstOrDefaultAsync<CurrentPlan>(@"
SELECT
    B.IdUserTypePlan as IdPlan,
    D.MonthPlan AS PlanSubscription,
    UT.Description AS PlanType,
    T.EmailQty,
    T.SubscribersQty,
    (CASE WHEN T.IdUserType != 4 THEN PartialBalance.Total ELSE T.SubscribersQty - ISNULL(Subscribers.Total, 0) END) AS RemainingCredits
FROM
    [BillingCredits] B
LEFT JOIN
    [UserTypesPlans] T ON T.[IdUserTypePlan] = B.[IdUserTypePlan]
LEFT JOIN
    [DiscountXPlan] D ON D.[IdDiscountPlan] = B.[IdDiscountPlan]
LEFT JOIN
    [UserTypes] UT ON UT.[IdUserType] = T.[IdUserType]
OUTER APPLY (SELECT TOP 1 MC.[PartialBalance] AS Total
    FROM [dbo].[MovementsCredits] MC
    WHERE MC.IdUser = (SELECT IdUser FROM [User] WHERE Email = @email)
    ORDER BY MC.[IdMovementCredit] DESC) PartialBalance
OUTER APPLY (
    SELECT SUM(VSBSXUA.Amount) AS Total
    FROM [dbo].[ViewSubscribersByStatusXUserAmount] VSBSXUA WITH (NOEXPAND)
    WHERE VSBSXUA.IdUser = (SELECT IdUser FROM [User] WHERE Email = @email)) Subscribers
WHERE
    B.[IdUser] = (SELECT IdUser FROM [User] WHERE Email = @email) ORDER BY B.[Date] DESC;",
                new
                {
                    @email = accountName
                });

            return currentPlan;
        }

        public async Task<bool> UpdateCurrentPaymentMethod(User user, PaymentMethod paymentMethod)
        {
            using var connection = _connectionFactory.GetConnection();

            if (paymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString())
            {
                var creditCard = new CreditCard
                {
                    Number = _encryptionService.EncryptAES256(paymentMethod.CCNumber.Replace(" ", "")),
                    HolderName = _encryptionService.EncryptAES256(paymentMethod.CCHolderFullName),
                    ExpirationMonth = int.Parse(paymentMethod.CCExpMonth),
                    ExpirationYear = int.Parse(paymentMethod.CCExpYear),
                    Code = _encryptionService.EncryptAES256(paymentMethod.CCVerification),
                    CardType = Enum.Parse<CardTypeEnum>(paymentMethod.CCType, true)
                };

                var cultureInfo = Thread.CurrentThread.CurrentCulture;
                var textInfo = cultureInfo.TextInfo;

                paymentMethod.CCType = textInfo.ToTitleCase(paymentMethod.CCType);

                //Validate CC
                var validCc = _cloverSettings.Value.UseCloverApi ? await _cloverService.IsValidCreditCard(user.Email, creditCard, user.IdUser, true) : await _paymentGateway.IsValidCreditCard(creditCard, user.IdUser, true);

                //var validCc = Enum.Parse<CardTypeEnum>(paymentMethod.CCType) != CardTypeEnum.Unknown && await ;
                if (!validCc)
                {
                    return false;
                }

                //Update user payment method in DB
                await UpdateUserPaymentMethod(user, paymentMethod);
            }
            else if (paymentMethod.PaymentMethodName == PaymentMethodEnum.MP.ToString())
            {
                var creditCard = new CreditCard
                {
                    Number = _encryptionService.EncryptAES256(paymentMethod.CCNumber.Replace(" ", "")),
                    HolderName = _encryptionService.EncryptAES256(paymentMethod.CCHolderFullName),
                    ExpirationMonth = int.Parse(paymentMethod.CCExpMonth),
                    ExpirationYear = int.Parse(paymentMethod.CCExpYear),
                    Code = _encryptionService.EncryptAES256(paymentMethod.CCVerification)
                };

                var cultureInfo = Thread.CurrentThread.CurrentCulture;
                var textInfo = cultureInfo.TextInfo;

                paymentMethod.CCType = textInfo.ToTitleCase(paymentMethod.CCType);

                //TODO: Integrate with the Mercadopago API: Create the customer in Mercadopago and then set the customerId in the user table
                //Update user payment method in DB
                await UpdateUserPaymentMethodByMercadopago(user, paymentMethod, creditCard);
            }
            else if (paymentMethod.PaymentMethodName == PaymentMethodEnum.TRANSF.ToString())
            {
                await UpdateUserPaymentMethodByTransfer(user, paymentMethod);
            }

            //Send BP to SAP
            if (paymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString() ||
                paymentMethod.PaymentMethodName == PaymentMethodEnum.MP.ToString() ||
                (paymentMethod.PaymentMethodName == PaymentMethodEnum.TRANSF.ToString() && user.IdBillingCountry == (int)CountryEnum.Argentina))
            {
                await SendUserDataToSap(user.Email, paymentMethod.IdSelectedPlan);
            }

            return true;
        }

        public async Task SetEmptyPaymentMethod(int idUser)
        {
            using var connection = _connectionFactory.GetConnection();

            await connection.ExecuteAsync(@"
UPDATE
    [USER]
SET
    CCHolderFullName = @ccHolderFullName,
    CCNumber = @ccNumber,
    CCExpMonth = @ccExpMonth,
    CCExpYear = @ccExpYear,
    CCVerification = @ccVerification,
    IdCCType = @idCCType,
    PaymentMethod = (SELECT IdPaymentMethod FROM [PaymentMethods] WHERE PaymentMethodName = @paymentMethodName),
    RazonSocial = @razonSocial,
    IdConsumerType = (SELECT IdConsumerType FROM [ConsumerTypes] WHERE Name = @idConsumerType),
    IdResponsabileBilling = @idResponsabileBilling,
    CUIT = @cuit,
    ResponsableIVA = @responsableIVA,
    CFDIUse = @useCFDI,
    PaymentType = @paymentType,
    PaymentWay = @paymentWay,
    BankAccount = @bankAccount,
    BankName = @bankName
WHERE
    IdUser = @IdUser;",
            new
            {
                idUser,
                @ccHolderFullName = string.Empty,
                @ccNumber = string.Empty,
                @ccExpMonth = (int?)null,
                @ccExpYear = (int?)null,
                @ccVerification = string.Empty,
                @idCCType = (string)null,
                @paymentMethodName = PaymentMethodEnum.NONE.ToString(),
                @razonSocial = string.Empty,
                @idConsumerType = string.Empty,
                @idResponsabileBilling = (int?)null,
                @cuit = string.Empty,
                @responsableIVA = (bool?)null,
                @useCFDI = string.Empty,
                @paymentType = string.Empty,
                @paymentWay = string.Empty,
                @bankAccount = string.Empty,
                @bankName = string.Empty
            });
        }


        private async Task UpdateUserPaymentMethodByTransfer(User user, PaymentMethod paymentMethod)
        {
            using var connection = _connectionFactory.GetConnection();

            var test = user.IdBillingCountry == (int)CountryEnum.Mexico && paymentMethod.PaymentWay == PaymentWayEnum.TRANSFER.ToString() ? paymentMethod.TaxRegime : 0;

            await connection.ExecuteAsync(@"
UPDATE
    [USER]
SET
    PaymentMethod = (SELECT IdPaymentMethod FROM [PaymentMethods] WHERE PaymentMethodName = @paymentMethodName),
    RazonSocial = @razonSocial,
    IdConsumerType = (SELECT IdConsumerType FROM [ConsumerTypes] WHERE Name = @idConsumerType),
    IdResponsabileBilling = @idResponsabileBilling,
    CUIT = @cuit,
    ResponsableIVA = @responsableIVA,
    CFDIUse = @useCFDI,
    PaymentType = @paymentType,
    PaymentWay = @paymentWay,
    BankAccount = @bankAccount,
    BankName = @bankName,
    TaxRegime = @taxRegime,
    TaxCertificateUrl = @taxCertificateUrl
WHERE
    IdUser = @IdUser;",
                new
                {
                    user.IdUser,
                    @paymentMethodName = paymentMethod.PaymentMethodName,
                    @razonSocial = paymentMethod.RazonSocial,
                    @idConsumerType = paymentMethod.IdConsumerType,
                    @idResponsabileBilling = CalculateBillingSystemByTransfer(user.IdBillingCountry),
                    @cuit = paymentMethod.IdentificationNumber,
                    @responsableIVA = paymentMethod.ResponsableIVA,
                    @useCFDI = user.IdBillingCountry == (int)CountryEnum.Mexico ? paymentMethod.UseCFDI : null,
                    @paymentType = user.IdBillingCountry == (int)CountryEnum.Mexico ? paymentMethod.PaymentType : null,
                    @paymentWay = user.IdBillingCountry == (int)CountryEnum.Mexico ? paymentMethod.PaymentWay.ToString() : null,
                    @bankAccount = user.IdBillingCountry == (int)CountryEnum.Mexico && paymentMethod.PaymentWay == PaymentWayEnum.TRANSFER.ToString() ? paymentMethod.BankAccount : null,
                    @bankName = user.IdBillingCountry == (int)CountryEnum.Mexico && paymentMethod.PaymentWay == PaymentWayEnum.TRANSFER.ToString() ? paymentMethod.BankName : null,
                    @taxRegime = user.IdBillingCountry == (int)CountryEnum.Mexico ? paymentMethod.TaxRegime : 0,
                    @taxCertificateUrl = paymentMethod.TaxCertificateUrl,
                });
        }

        private async Task UpdateUserPaymentMethod(User user, PaymentMethod paymentMethod)
        {
            using var connection = _connectionFactory.GetConnection();

            await connection.ExecuteAsync(@"
UPDATE
    [USER]
SET
    CCHolderFullName = @ccHolderFullName,
    CCNumber = @ccNumber,
    CCExpMonth = @ccExpMonth,
    CCExpYear = @ccExpYear,
    CCVerification = @ccVerification,
    IdCCType = @idCCType,
    PaymentMethod = (SELECT IdPaymentMethod FROM [PaymentMethods] WHERE PaymentMethodName = @paymentMethodName),
    RazonSocial = @razonSocial,
    IdConsumerType = (SELECT IdConsumerType FROM [ConsumerTypes] WHERE Name = @idConsumerType),
    IdResponsabileBilling = @idResponsabileBilling
WHERE
    IdUser = @IdUser;",
            new
            {
                user.IdUser,
                @ccHolderFullName = _encryptionService.EncryptAES256(paymentMethod.CCHolderFullName),
                @ccNumber = _encryptionService.EncryptAES256(paymentMethod.CCNumber.Replace(" ", "")),
                @ccExpMonth = paymentMethod.CCExpMonth,
                @ccExpYear = paymentMethod.CCExpYear,
                @ccVerification = _encryptionService.EncryptAES256(paymentMethod.CCVerification),
                @idCCType = Enum.Parse<CardTypeEnum>(paymentMethod.CCType, true),
                @paymentMethodName = paymentMethod.PaymentMethodName,
                @razonSocial = paymentMethod.RazonSocial,
                @idConsumerType = paymentMethod.IdConsumerType,
                @idResponsabileBilling = (int)ResponsabileBillingEnum.QBL
            });
        }

        private async Task UpdateUserPaymentMethodByMercadopago(User user, PaymentMethod paymentMethod, CreditCard creditCard)
        {
            using var connection = _connectionFactory.GetConnection();

            await connection.ExecuteAsync(@"
UPDATE
    [USER]
SET
    CCHolderFullName = @ccHolderFullName,
    CCNumber = @ccNumber,
    CCExpMonth = @ccExpMonth,
    CCExpYear = @ccExpYear,
    CCVerification = @ccVerification,
    IdCCType = @idCCType,
    PaymentMethod = (SELECT IdPaymentMethod FROM [PaymentMethods] WHERE PaymentMethodName = @paymentMethodName),
    RazonSocial = @razonSocial,
    IdConsumerType = (SELECT IdConsumerType FROM [ConsumerTypes] WHERE Name = @idConsumerType),
    IdResponsabileBilling = @idResponsabileBilling,
    CUIT = @cuit
WHERE
    IdUser = @IdUser;",
            new
            {
                user.IdUser,
                @ccHolderFullName = creditCard.HolderName,
                @ccNumber = creditCard.Number,
                @ccExpMonth = creditCard.ExpirationMonth,
                @ccExpYear = creditCard.ExpirationYear,
                @ccVerification = creditCard.Code,
                @idCCType = Enum.Parse<CardTypeEnum>(paymentMethod.CCType, true),
                @paymentMethodName = paymentMethod.PaymentMethodName,
                @razonSocial = paymentMethod.RazonSocial,
                @idConsumerType = paymentMethod.IdConsumerType ?? FinalConsumer,
                @idResponsabileBilling = (int)ResponsabileBillingEnum.Mercadopago,
                @cuit = paymentMethod.IdentificationNumber,
            });
        }

        private async Task SendUserDataToSap(string accountName, int planId)
        {
            using var connection = _connectionFactory.GetConnection();

            var user = await connection.QueryFirstOrDefaultAsync<User>(@"
SELECT
    U.FirstName,
    U.IdUser,
    U.BillingEmails,
    U.RazonSocial,
    U.BillingFirstName,
    U.BillingLastName,
    U.BillingAddress,
    U.CityName,
    U.IdState,
    S.CountryCode as StateCountryCode,
    U.Address,
    U.ZipCode,
    U.BillingZip,
    U.Email,
    U.PhoneNumber,
    U.IdConsumerType,
    U.CUIT,
    U.IsCancelated,
    U.SapProperties,
    U.BlockedAccountNotPayed,
    V.IsInbound as IsInbound,
    BS.CountryCode as BillingStateCountryCode,
    U.PaymentMethod,
    (SELECT IdUserType FROM [UserTypesPlans] WHERE IdUserTypePlan = @idUserTypePlan) as IdUserType,
    U.IdResponsabileBilling,
    U.IdBillingState,
    BS.Name as BillingStateName,
    U.BillingCity
FROM
    [User] U
LEFT JOIN
    [State] S ON S.IdState = U.IdState
LEFT JOIN
    [Vendor] V ON V.IdVendor = U.IdVendor
LEFT JOIN
    [State] BS ON BS.IdState = U.IdBillingState
WHERE
    U.Email = @accountName;",
                new
                {
                    accountName,
                    @idUserTypePlan = planId
                });

            if (user.IdResponsabileBilling is (int)ResponsabileBillingEnum.QBL or (int)ResponsabileBillingEnum.GBBISIDE or (int)ResponsabileBillingEnum.Mercadopago)
            {
                var sapDto = new SapBusinessPartner
                {
                    Id = user.IdUser,
                    IsClientManager = false,
                    BillingEmails = (user.BillingEmails ?? string.Empty).Replace(" ", string.Empty).Split(','),
                    FirstName = SapHelper.GetFirstName(user),
                    LastName = string.IsNullOrEmpty(user.RazonSocial) ? user.BillingLastName ?? "" : "",
                    BillingAddress = user.BillingAddress ?? "",
                    CityName = user.CityName ?? "",
                    StateId = user.IdState,
                    CountryCode = user.StateCountryCode ?? "",
                    Address = user.Address ?? "",
                    ZipCode = user.ZipCode ?? "",
                    BillingZip = user.BillingZip ?? "",
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? "",
                    FederalTaxId = user.CUIT,
                    IdConsumerType = user.IdConsumerType,
                    Cancelated = user.IsCancelated,
                    SapProperties = JsonConvert.DeserializeObject(user.SapProperties),
                    Blocked = user.BlockedAccountNotPayed,
                    IsInbound = user.IsInbound,
                    BillingCountryCode = user.BillingStateCountryCode ?? "",
                    PaymentMethod = user.PaymentMethod,
                    PlanType = user.IdUserType,
                    BillingSystemId = user.IdResponsabileBilling
                };

                sapDto.BillingStateId = ((sapDto.BillingSystemId == (int)ResponsabileBillingEnum.QBL || sapDto.BillingSystemId == (int)ResponsabileBillingEnum.QuickBookUSA) && sapDto.BillingCountryCode != "US") ? string.Empty
                    : (sapDto.BillingCountryCode == "US") ? (SapDictionary.StatesDictionary.TryGetValue(user.IdBillingState, out string stateIdUs) ? stateIdUs : string.Empty)
                    : (SapDictionary.StatesDictionary.TryGetValue(user.IdBillingState, out string stateId) ? stateId : "99");
                sapDto.County = user.BillingStateName ?? "";
                sapDto.BillingCity = user.BillingCity ?? "";

                await _sapService.SendUserDataToSap(sapDto);
            }
        }

        public async Task<PaymentMethod> GetPaymentMethodByUserName(string username)
        {
            using var connection = _connectionFactory.GetConnection();

            var result = await connection.QueryFirstOrDefaultAsync<PaymentMethod>(@"

SELECT
    U.CCHolderFullName,
    U.CCNumber,
    U.CCExpMonth,
    U.CCExpYear,
    U.CCVerification,
    C.[Description] AS CCType,
    P.PaymentMethodName AS PaymentMethodName,
    U.RazonSocial,
    U.IdConsumerType,
    U.CUIT as IdentificationNumber,
    U.ResponsableIVA,
    U.IdCCType
FROM
    [User] U
LEFT JOIN
    [CreditCardTypes] C ON C.IdCCType = U.IdCCType
LEFT JOIN
    [PaymentMethods] P ON P.IdPaymentMethod = U.PaymentMethod
WHERE
    U.Email = @email;",
                new
                {
                    @email = username
                });

            return result;
        }

        public async Task UpdateBillingCreditAsync(int billingCreditId, BillingCreditPaymentInfo billingCreditPaymentInfo)
        {
            using var connection = _connectionFactory.GetConnection();
            await connection.QueryFirstOrDefaultAsync("""
UPDATE [dbo].[BillingCredits]
SET CCNumber = @CCNumber,
    CCExpMonth = @CCExpMonth,
    CCExpYear = @CCExpYear,
    CCVerification = @CCVerification
WHERE
    IdBillingCredit = @billingCreditId
""",
                new
                {
                    @billingCreditId = billingCreditId,
                    @CCNumber = billingCreditPaymentInfo.CCNumber,
                    @CCExpMonth = billingCreditPaymentInfo.CCExpMonth,
                    @CCExpYear = billingCreditPaymentInfo.CCExpYear,
                    @CCVerification = billingCreditPaymentInfo.CCVerification
                });
        }

        public async Task<int> CreateBillingCreditAsync(BillingCreditAgreement buyCreditAgreement)
        {
            using var connection = _connectionFactory.GetConnection();
            var result = await connection.QueryFirstOrDefaultAsync<int>(@"
INSERT INTO [dbo].[BillingCredits]
    ([Date],
    [IdUser],
    [IdPaymentMethod],
    [PlanFee],
    [PaymentDate],
    [Taxes],
    [IdCurrencyType],
    [CreditsQty],
    [ActivationDate],
    [ExtraEmailFee],
    [TotalCreditsQty],
    [IdBillingCreditType],
    [CCNumber],
    [CCExpMonth],
    [CCExpYear],
    [CCVerification],
    [IdCCType],
    [IdConsumerType],
    [RazonSocial],
    [CUIT],
    [ExclusiveMessage],
    [IdUserTypePlan],
    [DiscountPlanFeePromotion],
    [ExtraCreditsPromotion],
    [SubscribersQty],
    [CCHolderFullName],
    [NroFacturacion],
    [IdDiscountPlan],
    [TotalMonthPlan],
    [CurrentMonthPlan],
    [PaymentType],
    [CFDIUse],
    [PaymentWay],
    [BankName],
    [BankAccount],
    [IdResponsabileBilling],
    [CCIdentificationType],
    [CCIdentificationNumber],
    [ResponsableIVA],
    [IdPromotion],
    [PromotionDuration],
    [DiscountPlanFeeAdmin])
VALUES (
    @date,
    @idUser,
    @idPaymentMethod,
    @planFee,
    @paymentDate,
    @taxes,
    @idCurrencyType,
    @creditsQty,
    @activationDate,
    @extraEmailFee,
    @totalCreditsQty,
    @idBillingCreditType,
    @ccNumber,
    @ccExpMonth,
    @ccExpYear,
    @ccVerification,
    @idCCType,
    @idConsumerType,
    @razonSocial,
    @cuit,
    @exclusiveMessage,
    @idUserTypePlan,
    @discountPlanFeePromotion,
    @extraCreditsPromotion,
    @subscribersQty,
    @ccHolderFullName,
    @nroFacturacion,
    @idDiscountPlan,
    @totalMonthPlan,
    @currentMonthPlan,
    @paymentType,
    @cfdiUse,
    @paymentWay,
    @bankName,
    @bankAccount,
    @idResponsabileBilling,
    @ccIdentificationType,
    @ccIdentificationNumber,
    @responsableIVA,
    @idPromotion,
    @promotionDuration,
    @discountPlanFeeAdmin);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new
            {
                @date = DateTime.UtcNow,
                @idUser = buyCreditAgreement.IdUser,
                @idPaymentMethod = buyCreditAgreement.IdPaymentMethod,
                @planFee = buyCreditAgreement.BillingCredit.PlanFee,
                @paymentDate = buyCreditAgreement.BillingCredit.PaymentDate,
                @taxes = buyCreditAgreement.BillingCredit.Taxes,
                @idCurrencyType = CurrencyTypeUsd,
                @creditsQty = buyCreditAgreement.BillingCredit.CreditsQty,
                @activationDate = buyCreditAgreement.BillingCredit.ActivationDate,
                @extraEmailFee = buyCreditAgreement.BillingCredit.ExtraEmailFee,
                @totalCreditsQty = buyCreditAgreement.BillingCredit.CreditsQty + (buyCreditAgreement.BillingCredit.ExtraCreditsPromotion ?? 0),
                @idBillingCreditType = buyCreditAgreement.BillingCredit.IdBillingCreditType,
                @ccNumber = buyCreditAgreement.CCNumber,
                @ccExpMonth = buyCreditAgreement.CCExpMonth,
                @ccExpYear = buyCreditAgreement.CCExpYear,
                @ccVerification = buyCreditAgreement.CCVerification,
                @idCCType = buyCreditAgreement.IdCCType,
                @idConsumerType = buyCreditAgreement.IdConsumerType,
                @razonSocial = buyCreditAgreement.RazonSocial,
                @cuit = buyCreditAgreement.Cuit ?? buyCreditAgreement.Rfc,
                @exclusiveMessage = buyCreditAgreement.ExclusiveMessage,
                @idUserTypePlan = buyCreditAgreement.BillingCredit.IdUserTypePlan,
                @discountPlanFeePromotion = buyCreditAgreement.BillingCredit.DiscountPlanFeePromotion,
                @extraCreditsPromotion = buyCreditAgreement.BillingCredit.ExtraCreditsPromotion,
                @subscribersQty = buyCreditAgreement.BillingCredit.SubscribersQty,
                @ccHolderFullName = buyCreditAgreement.CCHolderFullName,
                @nroFacturacion = 0,
                @idDiscountPlan = buyCreditAgreement.BillingCredit.IdDiscountPlan,
                @totalMonthPlan = buyCreditAgreement.BillingCredit.TotalMonthPlan,
                @currentMonthPlan = buyCreditAgreement.BillingCredit.CurrentMonthPlan,
                @paymentType = buyCreditAgreement.PaymentType,
                @cfdiUse = buyCreditAgreement.CFDIUse,
                @paymentWay = buyCreditAgreement.PaymentWay,
                @bankName = buyCreditAgreement.BankName,
                @bankAccount = buyCreditAgreement.BankAccount,
                @idResponsabileBilling = buyCreditAgreement.IdResponsabileBilling,
                @ccIdentificationType = buyCreditAgreement.CCIdentificationType,
                @ccIdentificationNumber = buyCreditAgreement.CCIdentificationNumber,
                @responsableIVA = buyCreditAgreement.ResponsableIVA,
                @idPromotion = buyCreditAgreement.IdPromotion,
                @promotionDuration = buyCreditAgreement.PromotionDuration,
                @discountPlanFeeAdmin = buyCreditAgreement.BillingCredit.DiscountPlanFeeAdmin
            });

            return result;
        }

        public async Task<int> CreateMovementCreditAsync(int idBillingCredit, int partialBalance, UserBillingInformation user, UserTypePlanInformation newUserTypePlan, int? currentMonthlyAddedEmailsWithBilling = null)
        {
            BillingCredit billingCredit = await GetBillingCredit(idBillingCredit);
            string conceptEnglish;
            string conceptSpanish;

            if (newUserTypePlan.IdUserType == UserTypeEnum.INDIVIDUAL)
            {
                conceptEnglish = "Credits Accreditation";
                conceptSpanish = "Acreditación de Créditos";
            }
            else
            {
                TextInfo textInfo = new CultureInfo("es", false).TextInfo;
                var date = billingCredit.ActivationDate ?? DateTime.UtcNow;
                conceptSpanish = "Acreditación de Emails Mes: " + textInfo.ToTitleCase(date.ToString("MMMM", CultureInfo.CreateSpecificCulture("es")));
                conceptEnglish = "Monthly Emails Accreditation: " + date.ToString("MMMM", CultureInfo.CreateSpecificCulture("en"));
            }

            using var connection = _connectionFactory.GetConnection();
            var result = await connection.QueryAsync<int>(@"
INSERT INTO [dbo].[MovementsCredits]
    ([IdUser],
    [Date],
    [CreditsQty],
    [IdBillingCredit],
    [PartialBalance],
    [ConceptEnglish],
    [ConceptSpanish],
    [IdUserType])
VALUES
    (@idUser,
    @date,
    @creditsQty,
    @idBillingCredit,
    @partialBalance,
    @conceptEnglish,
    @conceptSpanish,
    @idUserType);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new
            {
                @idUser = billingCredit.IdUser,
                @date = billingCredit.ActivationDate.HasValue ? billingCredit.ActivationDate.Value : DateTime.UtcNow,
                @idUserType = newUserTypePlan.IdUserType,
                @creditsQty = currentMonthlyAddedEmailsWithBilling == null ?
                    billingCredit.TotalCreditsQty.Value :
                    billingCredit.TotalCreditsQty.Value - currentMonthlyAddedEmailsWithBilling,
                @idBillingCredit = billingCredit.IdBillingCredit,
                @partialBalance = currentMonthlyAddedEmailsWithBilling == null ?
                    partialBalance + billingCredit.TotalCreditsQty.Value :
                    (partialBalance + billingCredit.TotalCreditsQty.Value) - currentMonthlyAddedEmailsWithBilling,
                @conceptEnglish = conceptEnglish,
                @conceptSpanish = conceptSpanish,
            });

            return result.FirstOrDefault();
        }

        public async Task<BillingCredit> GetBillingCredit(int billingCreditId)
        {
            using var connection = _connectionFactory.GetConnection();
            var billingCredit = await connection.QueryFirstOrDefaultAsync<BillingCredit>(@"
SELECT
    BC.[IdBillingCredit],
    BC.[Date],
    BC.[IdUser],
    BC.[PlanFee],
    BC.[CreditsQty],
    BC.[ActivationDate],
    BC.[TotalCreditsQty],
    BC.[IdUserTypePlan],
    DP.[DiscountPlanFee],
    BC.[IdResponsabileBilling],
    BC.[CCIdentificationType],
    BC.TotalMonthPlan,
    BC.CUIT As Cuit,
    BC.DiscountPlanFeeAdmin,
    BC.DiscountPlanFeePromotion,
    BC.IdPromotion,
    BC.[SubscribersQty],
    BC.[PaymentDate],
    BC.[IdDiscountPlan],
    BC.[TotalMonthPlan],
    BC.[CurrentMonthPlan]
FROM
    [dbo].[BillingCredits] BC
        LEFT JOIN [dbo].[DiscountXPlan] DP
        ON BC.IdDiscountPlan = DP.IdDiscountPlan
WHERE
    IdBillingCredit = @billingCreditId",
                new
                {
                    @billingCreditId = billingCreditId
                });

            return billingCredit;
        }

        public async Task<AccountingEntry> GetInvoice(int idClient, string authorizationNumber)
        {
            using var connection = _connectionFactory.GetConnection();
            var invoice = await connection.QueryFirstOrDefaultAsync<AccountingEntry>(@"
SELECT
    AE.[IdAccountingEntry],
    AE.[IdInvoice],
    AE.[Date],
    AE.[Amount],
    AE.[Status],
    AE.[Source],
    AE.[AuthorizationNumber],
    AE.[InvoiceNumber],
    AE.[AccountEntryType],
    AE.[AccountingTypeDescription],
    AE.[IdClient],
    AE.[IdAccountType],
    AE.[IdInvoiceBillingType],
    AE.[IdCurrencyType],
    AE.[CurrencyRate],
    AE.[Taxes]
FROM
    [dbo].[AccountingEntry] AE
WHERE
    idClient = @idClient AND authorizationNumber = @authorizationNumber",
                new
                {
                    @idClient = idClient,
                    @authorizationNumber = authorizationNumber
                });
            return invoice;
        }

        public async Task<List<AccountingEntry>> GetInvoices(int idClient, params PaymentStatusEnum[] status)
        {
            if (status.Length == 0)
            {
                status = new PaymentStatusEnum[] { PaymentStatusEnum.DeclinedPaymentTransaction, PaymentStatusEnum.Pending, PaymentStatusEnum.Approved };
            }
            using var connection = _connectionFactory.GetConnection();
            var invoices = (await connection.QueryAsync<AccountingEntry>(@"
SELECT
    AE.[IdAccountingEntry],
    AE.[Date],
    AE.[Amount],
    AE.[Status],
    AE.[Source],
    AE.[AuthorizationNumber],
    AE.[InvoiceNumber],
    AE.[AccountEntryType],
    AE.[AccountingTypeDescription],
    AE.[IdClient],
    AE.[IdAccountType],
    AE.[IdInvoiceBillingType],
    AE.[IdCurrencyType],
    AE.[CurrencyRate],
    AE.[Taxes],
    AE.[ErrorMessage]
FROM
    [dbo].[AccountingEntry] AE
WHERE
    idClient = @idClient AND [Status] IN @statusCondition",
                new
                {
                    @idClient = idClient,
                    @statusCondition = status.Select(x => x.ToString())
                })).ToList();
            return invoices;
        }

        public async Task<PlanDiscountInformation> GetPlanDiscountInformation(int discountId)
        {
            using var connection = _connectionFactory.GetConnection();
            var discountInformation = await connection.QueryFirstOrDefaultAsync<PlanDiscountInformation>(@"
SELECT
    DP.[IdDiscountPlan],
    DP.[DiscountPlanFee],
    DP.[MonthPlan],
    DP.[ApplyPromo]
FROM
    [DiscountXPlan] DP
WHERE
    DP.[IdDiscountPlan] = @discountId AND DP.[Active] = 1",
    new { discountId });

            return discountInformation;
        }

        public async Task UpdateUserSubscriberLimitsAsync(int idUser)
        {
            using var connection = _connectionFactory.GetConnection();
            using var dtUserCheckLimits = new DataTable();
            dtUserCheckLimits.Columns.Add(new DataColumn("IdUser", typeof(int)));

            var dataRow = dtUserCheckLimits.NewRow();
            dataRow["IdUser"] = idUser;
            dtUserCheckLimits.Rows.Add(dataRow);

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@Table", dtUserCheckLimits.AsTableValuedParameter("TYPEUSERTOCHECKLIMITS"));

            await connection.ExecuteAsync("User_UpdateLimits", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> ActivateStandBySubscribers(int idUser)
        {
            using var connection = _connectionFactory.GetConnection();
            var result = await connection.ExecuteScalarAsync<int>("UserReactivateStandBySubscribers", new { IdUser = idUser }, commandType: CommandType.StoredProcedure);
            return result;
        }

        public async Task<int> CreateAccountingEntriesAsync(AccountingEntry invoiceEntry, AccountingEntry paymentEntry)
        {
            using var connection = _connectionFactory.GetConnection();
            var invoiceId = await connection.QueryFirstOrDefaultAsync<int>(@"
INSERT INTO [dbo].[AccountingEntry]
    ([Date],
    [Amount],
    [Status],
    [Source],
    [AuthorizationNumber],
    [InvoiceNumber],
    [AccountEntryType],
    [AccountingTypeDescription],
    [IdClient],
    [IdAccountType],
    [IdInvoiceBillingType],
    [IdCurrencyType],
    [CurrencyRate],
    [Taxes]
    )
VALUES
    (@date,
    @amount,
    @status,
    @source,
    @authorizationNumber,
    @invoiceNumber,
    @accountEntryType,
    @accountingTypeDescription,
    @idClient,
    @idAccountType,
    @idInvoiceBillingType,
    @idCurrencyType,
    @currencyRate,
    @taxes);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new
            {
                @idClient = invoiceEntry.IdClient,
                @amount = invoiceEntry.Amount,
                @date = invoiceEntry.Date,
                @status = invoiceEntry.Status.ToString(),
                @source = invoiceEntry.Source,
                @accountingTypeDescription = invoiceEntry.AccountingTypeDescription,
                @invoiceNumber = 0,
                @idAccountType = invoiceEntry.IdAccountType,
                @idInvoiceBillingType = invoiceEntry.IdInvoiceBillingType,
                @authorizationNumber = invoiceEntry.AuthorizationNumber,
                @accountEntryType = invoiceEntry.AccountEntryType,
                @idCurrencyType = invoiceEntry.IdCurrencyType,
                @currencyRate = invoiceEntry.CurrencyRate,
                @taxes = invoiceEntry.Taxes
            });

            if (paymentEntry != null)
            {
                await connection.QueryFirstOrDefaultAsync<int>(@"
INSERT INTO [dbo].[AccountingEntry]
    ([IdClient],
    [IdInvoice],
    [Amount],
    [CCNumber],
    [CCExpMonth],
    [CCExpYear],
    [CCHolderName],
    [Date],
    [Source],
    [AccountingTypeDescription],
    [IdAccountType],
    [IdInvoiceBillingType],
    [AccountEntryType],
    [AuthorizationNumber],
    [PaymentEntryType],
    [IdCurrencyType],
    [CurrencyRate],
    [Taxes])
VALUES
    (@idClient,
    @idInvoice,
    @amount,
    @ccCNumber,
    @ccExpMonth,
    @ccExpYear,
    @ccHolderName,
    @date,
    @source,
    @accountingTypeDescription,
    @idAccountType,
    @idInvoiceBillingType,
    @accountEntryType,
    @authorizationNumber,
    @paymentEntryType,
    @idCurrencyType,
    @currencyRate,
    @taxes);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
                new
                {
                    @idClient = paymentEntry.IdClient,
                    @idInvoice = invoiceId,
                    @amount = paymentEntry.Amount,
                    @ccCNumber = paymentEntry.CcCNumber,
                    @ccExpMonth = paymentEntry.CcExpMonth,
                    @ccExpYear = paymentEntry.CcExpYear,
                    @ccHolderName = paymentEntry.CcHolderName,
                    @date = paymentEntry.Date,
                    @source = paymentEntry.Source,
                    @accountingTypeDescription = paymentEntry.AccountingTypeDescription,
                    @idAccountType = paymentEntry.IdAccountType,
                    @idInvoiceBillingType = paymentEntry.IdInvoiceBillingType,
                    @accountEntryType = paymentEntry.AccountEntryType,
                    @authorizationNumber = paymentEntry.AuthorizationNumber,
                    @paymentEntryType = paymentEntry.PaymentEntryType,
                    @idCurrencyType = paymentEntry.IdCurrencyType,
                    @currencyRate = paymentEntry.CurrencyRate,
                    @taxes = paymentEntry.Taxes
                });
            }

            return invoiceId;
        }

        public async Task<int> CreatePaymentEntryAsync(int invoiceId, AccountingEntry paymentEntry)
        {
            using var connection = _connectionFactory.GetConnection();
            var IdAccountingEntry = await connection.QueryFirstOrDefaultAsync<int>(@"
INSERT INTO [dbo].[AccountingEntry]
    ([IdClient],
    [IdInvoice],
    [Amount],
    [CCNumber],
    [CCExpMonth],
    [CCExpYear],
    [CCHolderName],
    [Date],
    [Source],
    [AccountingTypeDescription],
    [IdAccountType],
    [IdInvoiceBillingType],
    [AccountEntryType],
    [AuthorizationNumber],
    [PaymentEntryType],
    [IdCurrencyType],
    [CurrencyRate],
    [Taxes])
VALUES
    (@idClient,
    @idInvoice,
    @amount,
    @ccCNumber,
    @ccExpMonth,
    @ccExpYear,
    @ccHolderName,
    @date,
    @source,
    @accountingTypeDescription,
    @idAccountType,
    @idInvoiceBillingType,
    @accountEntryType,
    @authorizationNumber,
    @paymentEntryType,
    @idCurrencyType,
    @currencyRate,
    @taxes);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new
            {
                @idClient = paymentEntry.IdClient,
                @idInvoice = invoiceId,
                @amount = paymentEntry.Amount,
                @ccCNumber = paymentEntry.CcCNumber,
                @ccExpMonth = paymentEntry.CcExpMonth,
                @ccExpYear = paymentEntry.CcExpYear,
                @ccHolderName = paymentEntry.CcHolderName,
                @date = paymentEntry.Date,
                @source = paymentEntry.Source,
                @accountingTypeDescription = paymentEntry.AccountingTypeDescription,
                @idAccountType = paymentEntry.IdAccountType,
                @idInvoiceBillingType = paymentEntry.IdInvoiceBillingType,
                @accountEntryType = paymentEntry.AccountEntryType,
                @authorizationNumber = paymentEntry.AuthorizationNumber,
                @paymentEntryType = paymentEntry.PaymentEntryType,
                @idCurrencyType = paymentEntry.IdCurrencyType,
                @currencyRate = paymentEntry.CurrencyRate,
                @taxes = paymentEntry.Taxes
            });

            return IdAccountingEntry;
        }

        public async Task<int> CreateCreditNoteEntryAsync(AccountingEntry creditNoteEntry)
        {
            using var connection = _connectionFactory.GetConnection();
            var IdAccountingEntry = await connection.QueryFirstOrDefaultAsync<int>(@"
INSERT INTO [dbo].[AccountingEntry]
    ([IdClient],
    [IdInvoice],
    [Amount],
    [Date],
    [Source],
    [AccountingTypeDescription],
    [IdAccountType],
    [IdBillingSource],
    [IdInvoiceBillingType],
    [IdCurrencyType],
    [CurrencyRate],
    [Taxes],
    [AccountEntryType],
    [PaymentEntryType])
VALUES
    (@idClient,
    @idInvoice,
    @amount,
    @date,
    @source,
    @accountingTypeDescription,
    @idAccountType,
    @idBillingSource,
    @idInvoiceBillingType,
    @idCurrencyType,
    @currencyRate,
    @taxes,
    @accountEntryType,
    @paymentEntryType);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new
            {
                @idClient = creditNoteEntry.IdClient,
                @idInvoice = creditNoteEntry.IdInvoice,
                @amount = creditNoteEntry.Amount,
                @date = creditNoteEntry.Date,
                @source = creditNoteEntry.Source,
                @accountingTypeDescription = creditNoteEntry.AccountingTypeDescription,
                @idAccountType = creditNoteEntry.IdAccountType,
                @idBillingSource = creditNoteEntry.IdBillingSource,
                @idInvoiceBillingType = creditNoteEntry.IdInvoiceBillingType,
                @idCurrencyType = creditNoteEntry.IdCurrencyType,
                @currencyRate = creditNoteEntry.CurrencyRate,
                @taxes = creditNoteEntry.Taxes,
                @accountEntryType = creditNoteEntry.AccountEntryType,
                @paymentEntryType = creditNoteEntry.PaymentEntryType
            });

            return IdAccountingEntry;
        }

        public async Task UpdateInvoiceStatus(int id, PaymentStatusEnum status, string statusDetail, string authorizationNumber)
        {
            using var connection = _connectionFactory.GetConnection();
            await connection.ExecuteAsync(@"
UPDATE
    [dbo].[AccountingEntry]
SET
    Status = @Status,
    ErrorMessage = @StatusDetail,
    AuthorizationNumber = @authorizationNumber
WHERE
    IdAccountingEntry = @Id;",
            new
            {
                @Id = id,
                @Status = status.ToString(),
                @StatusDetail = status == PaymentStatusEnum.DeclinedPaymentTransaction ? statusDetail : string.Empty,
                @authorizationNumber = authorizationNumber
            });
        }

        public async Task<int> CreateMovementBalanceAdjustmentAsync(int userId, int creditsQty, UserTypeEnum currentUserType, UserTypeEnum newUserType)
        {
            string conceptEnglish = string.Empty;
            string conceptSpanish = string.Empty;

            if (newUserType == UserTypeEnum.MONTHLY)
            {
                if (currentUserType == UserTypeEnum.MONTHLY)
                {
                    conceptEnglish = "Changed between Monthlies plans";
                    conceptSpanish = "Cambio entre planes Mensuales";
                }
                else
                {
                    conceptEnglish = "Changed to Monthly";
                    conceptSpanish = "Cambio a Mensual";
                }
            }
            else
            {
                if (newUserType == UserTypeEnum.INDIVIDUAL)
                {
                    conceptEnglish = "Changed to Prepaid";
                    conceptSpanish = "Cambio a Prepago";
                }
            }

            using var connection = _connectionFactory.GetConnection();
            var result = await connection.QueryAsync<int>(@"
INSERT INTO [dbo].[MovementsCredits]
    ([IdUser],
    [Date],
    [CreditsQty],
    [ConceptEnglish],
    [ConceptSpanish],
    [IdUserType],
    [Visible])
VALUES
    (@idUser,
    @date,
    @creditsQty,
    @conceptEnglish,
    @conceptSpanish,
    @idUserType,
    @visible);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new
            {
                @idUser = userId,
                @date = DateTime.UtcNow,
                @idUserType = newUserType,
                @creditsQty = creditsQty * -1,
                @conceptEnglish = conceptEnglish,
                @conceptSpanish = conceptSpanish,
                @visible = false
            });

            return result.FirstOrDefault();
        }

        public async Task CreateMovementCreditsLeftAsync(int idUser, int creditsQty, int partialBalance)
        {
            string conceptEnglish = "Not Spent Credits";
            string conceptSpanish = "Creditos No Gastados";

            using var connection = _connectionFactory.GetConnection();
            var result = await connection.QueryAsync<int>(@"
INSERT INTO [dbo].[MovementsCredits]
    ([IdUser],
    [Date],
    [CreditsQty],
    [ConceptEnglish],
    [ConceptSpanish],
    [IdUserType],
    [Visible])
VALUES
    (@idUser,
    @date,
    @creditsQty,
    @conceptEnglish,
    @conceptSpanish,
    @idUserType,
    @visible);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new
            {
                @idUser = idUser,
                @date = DateTime.UtcNow,
                @idUserType = (int)UserTypeEnum.MONTHLY,
                @creditsQty = creditsQty + partialBalance,
                @conceptEnglish = conceptEnglish,
                @conceptSpanish = conceptSpanish,
                @visible = false
            });
        }

        public async Task<List<BillingCredit>> GetPendingBillingCreditsAsync(int userId, PaymentMethodEnum paymentMethod)
        {
            using var connection = _connectionFactory.GetConnection();
            var billingCredits = await connection.QueryAsync<BillingCredit>(@"
SELECT BC.[IdBillingCredit],
    BC.[Date],
    BC.[IdUser],
    BC.[PlanFee],
    BC.[CreditsQty],
    BC.[ActivationDate],
    BC.[TotalCreditsQty],
    BC.[IdUserTypePlan],
    BC.[IdResponsabileBilling],
    BC.[CCIdentificationType],
    BC.TotalMonthPlan,
    BC.CUIT As Cuit,
    BC.DiscountPlanFeeAdmin,
    BC.DiscountPlanFeePromotion,
    BC.IdPromotion,
    BC.[SubscribersQty],
    BC.[PaymentDate],
    BC.[IdDiscountPlan],
    BC.[TotalMonthPlan],
    BC.[CurrentMonthPlan],
    BC.[IdBillingCreditType],
    BC.[ExtraCreditsPromotion],
    P.[IdUserType],
    BC.[CCNumber],
    BC.[CCHolderFullName] AS CCHolderName
FROM [BillingCredits] BC
INNER JOIN [UserTypesPlans] P ON P.IdUserTypePlan = BC.IdUserTypePlan
WHERE IdPaymentMethod = @idPaymentMethod AND
    PaymentDate IS NULL AND
    IdBillingCreditType IN (1, 2, 13, 14) AND
    BC.IdUser = @userId
ORDER BY Date DESC;",
            new
            {
                userId,
                @idPaymentMethod = (int)paymentMethod
            });

            return billingCredits.ToList();
        }

        public async Task ApproveBillingCreditAsync(BillingCredit billingCredit)
        {
            using var connection = _connectionFactory.GetConnection();
            await connection.QueryFirstOrDefaultAsync(@"
UPDATE [dbo].[BillingCredits]
SET ActivationDate = @ActivationDate,
    PaymentDate = @PaymentDate
WHERE
    IdBillingCredit = @billingCreditId
",
                new
                {
                    @billingCreditId = billingCredit.IdBillingCredit,
                    @ActivationDate = billingCredit.ActivationDate,
                    @PaymentDate = billingCredit.PaymentDate
                });
        }

        public async Task<BillingCredit> GetPreviousBillingCreditNotCancelledByIdUserAsync(int idUser, int currentBillingCredit)
        {
            using var connection = _connectionFactory.GetConnection();
            var billingCredits = await connection.QueryAsync<BillingCredit>(@"
SELECT BC.[IdBillingCredit],
    BC.[Date],
    BC.[IdUser],
    BC.[PlanFee],
    BC.[CreditsQty],
    BC.[ActivationDate],
    BC.[TotalCreditsQty],
    BC.[IdUserTypePlan],
    BC.[IdResponsabileBilling],
    BC.[CCIdentificationType],
    BC.TotalMonthPlan,
    BC.CUIT As Cuit,
    BC.DiscountPlanFeeAdmin,
    BC.DiscountPlanFeePromotion,
    BC.IdPromotion,
    BC.[SubscribersQty],
    BC.[PaymentDate],
    BC.[IdDiscountPlan],
    BC.[TotalMonthPlan],
    BC.[CurrentMonthPlan],
    BC.[IdBillingCreditType],
    BC.[ExtraCreditsPromotion],
    P.[IdUserType],
    BC.[CCNumber],
    BC.[CCHolderFullName] AS CCHolderName
FROM [BillingCredits] BC
INNER JOIN [UserTypesPlans] P ON P.IdUserTypePlan = BC.IdUserTypePlan
WHERE IdBillingCreditType NOT IN (17, 22, 20, 21) AND BC.IdUser = @idUser AND BC.[IdBillingCredit] !=  @currentBillingCredit
ORDER BY BC.[ActivationDate] DESC;",
            new
            {
                idUser,
                currentBillingCredit
            });

            return billingCredits.FirstOrDefault();
        }

        public async Task CancelBillingCreditAsync(BillingCredit billingCredit)
        {
            using var connection = _connectionFactory.GetConnection();
            await connection.QueryFirstOrDefaultAsync(@"
UPDATE [dbo].[BillingCredits]
SET IdBillingCreditType = @idIdBillingCreditType,
    IdPaymentMethod = @idPaymentMethod
WHERE
    IdBillingCredit = @billingCreditId
",
                new
                {
                    @billingCreditId = billingCredit.IdBillingCredit,
                    @idIdBillingCreditType = (int)BillingCreditTypeEnum.Canceled,
                    @idPaymentMethod = (int)PaymentMethodEnum.NONE
                });
        }

        private int CalculateBillingSystemByTransfer(int idBillingCountry)
        {
            return idBillingCountry switch
            {
                (int)CountryEnum.Colombia => (int)ResponsabileBillingEnum.BorisMarketing,
                (int)CountryEnum.Mexico => (int)ResponsabileBillingEnum.RC,
                (int)CountryEnum.Argentina => (int)ResponsabileBillingEnum.GBBISIDE,
                _ => (int)ResponsabileBillingEnum.GBBISIDE,
            };
        }
    }
}
