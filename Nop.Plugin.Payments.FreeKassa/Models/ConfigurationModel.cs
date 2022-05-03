using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.FreeKassa.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.FreeKassa.Fields.MerchantId")]
        public int MerchantId { get; set; }
        public bool MerchantId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.FreeKassa.Fields.Secret1")]
        public string Secret1 { get; set; }
        public bool Secret1_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.FreeKassa.Fields.Secret2")]
        public string Secret2 { get; set; }
        public bool Secret2_OverrideForStore { get; set; }
    }
}
