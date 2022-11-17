using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.MercadoPagoApi;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Mappers;
using Doppler.BillingUser.Mappers.PaymentStatus;
using Doppler.BillingUser.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Controllers
{
    [ApiController]
    public class WebhooksController : ControllerBase
    {
        private readonly IBillingRepository _billingRepository;
        private readonly IPaymentAmountHelper _paymentAmountService;
        private readonly ILogger<WebhooksController> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly IPaymentStatusMapper _paymentStatusMapper;
        private readonly IEmailTemplatesService _emailTemplatesService;
        private readonly string PAYMENT_UPDATED = "payment.updated";

        public WebhooksController(
            IPaymentAmountHelper paymentAmountService,
            ILogger<WebhooksController> logger,
            IBillingRepository billingRepository,
            IUserRepository userRepository,
            IMercadoPagoService mercadoPagoService,
            IPaymentStatusMapper paymentStatusMapper,
            IEmailTemplatesService emailTemplatesService)
        {
            _billingRepository = billingRepository;
            _paymentAmountService = paymentAmountService;
            _logger = logger;
            _userRepository = userRepository;
            _mercadoPagoService = mercadoPagoService;
            _paymentStatusMapper = paymentStatusMapper;
            _emailTemplatesService = emailTemplatesService;
        }

        [HttpPost("/accounts/{accountname}/integration/mercadopagonotification")]
        public async Task<IActionResult> UpdateMercadoPagoPaymentStatusAsync([FromRoute] string accountname, [FromBody] MercadoPagoNotification notification)
        {
            if (notification.Action != PAYMENT_UPDATED)
            {
                return new OkObjectResult("Successful");
            }

            var user = await _userRepository.GetUserInformation(accountname);
            if (user is null)
            {
                return new NotFoundObjectResult("Account not found");
            }

            var invoice = await _billingRepository.GetInvoice(user.IdUser, notification.Data.Id.ToString());
            if (invoice is null)
            {
                _logger.LogError("Invoice with authorization number: {authorizationNumber} was not found.", notification.Data.Id);
                return new NotFoundObjectResult("Invoice not found");
            }

            var payment = await _mercadoPagoService.GetPaymentById(notification.Data.Id, accountname);

            var status = _paymentStatusMapper.MapToPaymentStatus(payment.Status);
            if (status == PaymentStatusEnum.Pending && invoice.Status == PaymentStatusEnum.Approved)
            {
                return new OkObjectResult("Successful");
            }

            if (status == PaymentStatusEnum.DeclinedPaymentTransaction && invoice.Status != PaymentStatusEnum.DeclinedPaymentTransaction)
            {
                if (invoice.Status == PaymentStatusEnum.Approved)
                {
                    _logger.LogError("The payment associated to the invoiceId {invoiceId} was rejected. Reason: {reason}", invoice.IdAccountingEntry, payment.StatusDetail);
                }

                await _billingRepository.UpdateInvoiceStatus(invoice.IdAccountingEntry, status, payment.StatusDetail, invoice.AuthorizationNumber);
                return new OkObjectResult("Successful");
            }

            if (status == PaymentStatusEnum.Approved && invoice.Status != PaymentStatusEnum.Approved)
            {
                var accountingEntryMapper = new AccountingEntryForMercadopagoMapper(_paymentAmountService);
                var encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);
                var paymentEntry = await accountingEntryMapper.MapToPaymentAccountingEntry(invoice, encryptedCreditCard);
                await _billingRepository.UpdateInvoiceStatus(invoice.IdAccountingEntry, PaymentStatusEnum.Approved, payment.StatusDetail, invoice.AuthorizationNumber);
                await _billingRepository.CreatePaymentEntryAsync(invoice.IdAccountingEntry, paymentEntry);

                await _emailTemplatesService.SendNotificationForMercadoPagoPaymentApproved(user.IdUser, user.Email);
            }

            return new OkObjectResult("Successful");
        }

    }
}
