using FreeKassa.NET;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.FreeKassa.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.FreeKassa.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class PaymentFreeKassaController : BasePaymentController
    {

        #region Fields

        private readonly IFreeKassaService _freeKassaService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IOrderService _orderService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public PaymentFreeKassaController(IFreeKassaService freeKassaService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            IOrderService orderService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext,
            IWebHelper webHelper)
        {
            _freeKassaService = freeKassaService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _orderService = orderService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
            _webHelper = webHelper;
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var freeKassaPaymentSettings = await _settingService.LoadSettingAsync<FreeKassaPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                MerchantId = freeKassaPaymentSettings.MerchantId,
                Secret1 = freeKassaPaymentSettings.Secret1,
                Secret2 = freeKassaPaymentSettings.Secret2,
                ActiveStoreScopeConfiguration = storeScope
            };

            var configureViewUrl = "~/Plugins/Payments.FreeKassa/Views/Configure.cshtml";

            if (storeScope <= 0)
                return View(configureViewUrl, model);

            model.MerchantId_OverrideForStore = await _settingService.SettingExistsAsync(freeKassaPaymentSettings, x => x.MerchantId, storeScope);
            model.Secret1_OverrideForStore = await _settingService.SettingExistsAsync(freeKassaPaymentSettings, x => x.Secret1, storeScope);
            model.Secret2_OverrideForStore = await _settingService.SettingExistsAsync(freeKassaPaymentSettings, x => x.Secret2, storeScope);
         
            return View(configureViewUrl, model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var freeKassaPaymentSettings = await _settingService.LoadSettingAsync<FreeKassaPaymentSettings>(storeScope);

            //save settings
            freeKassaPaymentSettings.MerchantId = model.MerchantId;
            freeKassaPaymentSettings.Secret1 = model.Secret1;
            freeKassaPaymentSettings.Secret2 = model.Secret2;
            
            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            await _settingService.SaveSettingOverridablePerStoreAsync(freeKassaPaymentSettings, x => x.MerchantId, model.MerchantId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(freeKassaPaymentSettings, x => x.Secret1, model.Secret1_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(freeKassaPaymentSettings, x => x.Secret2, model.Secret2_OverrideForStore, storeScope, false);
            
            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        [HttpGet]
        public async Task<IActionResult> Result()
        {
            var AmountStr = _webHelper.QueryString<string>("AMOUNT");
            var orderId = _webHelper.QueryString<string>("MERCHANT_ORDER_ID");
            var sign = _webHelper.QueryString<string>("SIGN");

            if (string.IsNullOrEmpty(AmountStr) || string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(sign))
                return Content("Error");

            var amount = Convert.ToDecimal(AmountStr);
            var notificationSign = _freeKassaService.GetNotificationSign(orderId, amount);

            if (notificationSign.ToUpper().Equals(sign.ToUpper()))
                return Content("Error");

            var order = await _orderService.GetOrderByIdAsync(Convert.ToInt32(orderId));
            
            if (order.OrderGuid != Guid.Empty)
            {
                if (order.OrderTotal <= amount)
                { //если оплачен полностью (или больше), то отметить как оплаченный
                    order.PaymentStatus = Core.Domain.Payments.PaymentStatus.Paid;
                    await _orderService.UpdateOrderAsync(order);
                }
            }
            return Content(string.Format("OK{0}", orderId));

        }

        [HttpGet]
        public async Task<IActionResult> Fail(string OutSum, string InvId)
        {
            var model = new Fail();
            if (!String.IsNullOrEmpty(OutSum) && !String.IsNullOrEmpty(InvId))
            {
                model.orderid = InvId;
                model.ordersum = OutSum;
            }
            else
            {
                model.orderid = "0";
                model.ordersum = "0";
            }
            return View("~/Plugins/Payments.FreeKassa/Views/PaymentFreeKassa/Fail.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> Success(string OutSum, string InvId, string SignatureValue, string Culture)
        {
            if (!String.IsNullOrEmpty(OutSum) && !String.IsNullOrEmpty(InvId) && !String.IsNullOrEmpty(SignatureValue))
            {
                //load settings for a chosen store scope
                var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
                var freeKassaPaymentSettings = await _settingService.LoadSettingAsync<FreeKassaPaymentSettings>(storeScope);

                var secret1 = freeKassaPaymentSettings.Secret1;

                //check md5:
                string computedmd5 = MD5Helper.GetMD5Hash(String.Format("{0}:{1}:{2}", OutSum, InvId, secret1));
                if (computedmd5.ToUpper() != SignatureValue.ToUpper())
                {
                    _log.InsertLog(logLevel: Core.Domain.Logging.LogLevel.Error, shortMessage: "Неправильный переход на success", fullMessage: String.Format("OutSum:{0}, InvId:{1}, SignatureValue:{2}", OutSum, InvId, SignatureValue));
                    return Content("Ошибка: От Робокассы получены неверные параметры.\nОшибка сохранена в лог сервера.");
                }

                return RedirectToRoute("CheckoutCompleted", new { orderId = InvId });
            }
            return Content("Ошибка: не указаны параметры при переходе.");
        }

        #endregion                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  
    }
}
