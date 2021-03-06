﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.MonetaDirect.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework;

namespace Nop.Plugin.Payments.MonetaDirect
{

    public class MonetaDirectPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly MonetaDirectPaymentSettings _monetaDirectPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly CurrencySettings _currencySettings;
        private readonly IWebHelper _webHelper;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        #endregion

        #region Ctor

        public MonetaDirectPaymentProcessor(MonetaDirectPaymentSettings monetaDirectPaymentSettings,
            ISettingService settingService,
            ICurrencyService currencyService, ICustomerService customerService,
            CurrencySettings currencySettings, IWebHelper webHelper,
            IOrderTotalCalculationService orderTotalCalculationService)
        {
            this._monetaDirectPaymentSettings = monetaDirectPaymentSettings;
            this._settingService = settingService;
            this._currencyService = currencyService;
            this._customerService = customerService;
            this._currencySettings = currencySettings;
            this._webHelper = webHelper;
            this._orderTotalCalculationService = orderTotalCalculationService;
        }

        #endregion

        #region Methods

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult {NewPaymentStatus = PaymentStatus.Pending};
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var customerId = postProcessPaymentRequest.Order.CustomerId;
            var orderGuid = postProcessPaymentRequest.Order.OrderGuid;
            var orderTotal = postProcessPaymentRequest.Order.OrderTotal;

            var model = _monetaDirectPaymentSettings.CreatePaymentInfoModel(customerId, orderGuid, orderTotal);
           
            var post = new RemotePost
            {
                FormName = "PayPoint",
                Url = model.MonetaAssistantUrl
            };
            post.Add("MNT_ID", model.MntId);
            post.Add("MNT_TRANSACTION_ID", model.MntTransactionId);
            post.Add("MNT_CURRENCY_CODE", model.MntCurrencyCode);
            post.Add("MNT_AMOUNT", model.MntAmount);
            post.Add("MNT_TEST_MODE", model.MntTestMode.ToString());
            post.Add("MNT_SUBSCRIBER_ID", model.MntSubscriberId.ToString());
            post.Add("MNT_SIGNATURE", model.MntSignature);
            post.Post();
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return false;
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
                _monetaDirectPaymentSettings.AdditionalFee, _monetaDirectPaymentSettings.AdditionalFeePercentage);
            return result;
        }

        #region Not implemented methods
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }
        #endregion

        public bool CanRePostProcessPayment(Order order)
        {
            return false;
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentMonetaDirect";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.MonetaDirect.Controllers" }, { "area", null } };
        }

        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentMonetaDirect";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.MonetaDirect.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentMonetaDirectController);
        }
 
        public override void Install()
        {
            //settings
            var settings = new MonetaDirectPaymentSettings
            {
                MntTestMode = true,
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.Amount", "Amount");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.MntId", "Store identifier");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.MntTestMode", "Is made in test mode");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.HeshCode", "Hesh-code");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.MntCurrencyCode", "ISO currency code");
           
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");

            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.RedirectionTip",
                "For payment you will be redirected to the website MONETA.RU");

            base.Install();
        }

        public override void Uninstall()
        {
            _settingService.DeleteSetting<MonetaDirectPaymentSettings>();

            this.DeletePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.Amount");
            this.DeletePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.MntId");
            this.DeletePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.MntTestMode");
            this.DeletePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.HeshCode");
            this.DeletePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.MntCurrencyCode");
    
            this.DeletePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.AdditionalFeePercentage");
            this.DeletePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.AdditionalFeePercentage.Hint");

            this.DeletePluginLocaleResource("Plugins.Payments.MonetaDirect.Fields.RedirectionTip");

            base.Uninstall();
        }

        #endregion

        #region Properties
        public bool SupportCapture => false;

        public bool SupportPartiallyRefund => false;

        public bool SupportRefund => false;

        public bool SupportVoid => false;

        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        public bool SkipPaymentInfo => false;

        #endregion
    }
}