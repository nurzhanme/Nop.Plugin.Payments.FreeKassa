using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.FreeKassa.Components
{
    public class PaymentFreeKassaViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.FreeKassa/Views/PaymentInfo.cshtml");
        }
    }
}
