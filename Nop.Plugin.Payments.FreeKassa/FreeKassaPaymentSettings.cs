using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.FreeKassa
{
    /// <summary>
    /// Represents settings of the FreeKassa payment plugin
    /// </summary>
    public class FreeKassaPaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a MerchantId in FreeKassa Service
        /// </summary>
        public int MerchantId { get; set; }

        /// <summary>
        /// Gets or sets first secret word. Used for payment signature
        /// </summary>
        public string Secret1 { get; set; }

        /// <summary>
        /// Gets or sets second secret word. Used for generating a control signature
        /// </summary>
        public string Secret2 { get; set; }
    }
}
