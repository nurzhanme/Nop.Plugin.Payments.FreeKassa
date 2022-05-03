using FreeKassa.NET;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Services.Configuration;

namespace Nop.Plugin.Payments.FreeKassa.Infrastructure
{
    /// <summary>
    /// Represents object for the configuring services on application startup
    /// </summary>
    public class NopStartup : INopStartup
    {
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;

        public NopStartup(IStoreContext storeContext)
        {
            _storeContext = storeContext;
        }

        /// <summary>
        /// Add and configure any of the middleware
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration of the application</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            //load settings for a chosen store scope
            var storeScope = _storeContext.GetActiveStoreScopeConfigurationAsync().GetAwaiter().GetResult();
            var freeKassaPaymentSettings = _settingService.LoadSettingAsync<FreeKassaPaymentSettings>(storeScope).GetAwaiter().GetResult();

            services.AddFreeKassa(freeKassaPaymentSettings.MerchantId, freeKassaPaymentSettings.Secret1, freeKassaPaymentSettings.Secret2);
        }

        /// <summary>
        /// Configure the using of added middleware
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        public void Configure(IApplicationBuilder application)
        {
        }

        /// <summary>
        /// Gets order of this startup configuration implementation
        /// </summary>
        public int Order => 3000;
    }
}
