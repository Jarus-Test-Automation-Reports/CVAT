using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.IO;
using System.Threading.Tasks;
using CAT.AID.Web.Helpers;


namespace CAT.AID.Web.Helpers
{
    public static class RenderViewExtensions
    {
        public static async Task<string> RenderViewAsync<TModel>(
            this Controller controller, string viewName, TModel model, bool partial = false)
        {
            controller.ViewData.Model = model;

            using var sw = new StringWriter();
            var engine = controller.HttpContext.RequestServices
                .GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;

            var viewResult = engine.FindView(controller.ControllerContext, viewName, !partial);

            var viewContext = new ViewContext(
                controller.ControllerContext,
                viewResult.View,
                controller.ViewData,
                controller.TempData,
                sw,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }
    }
}
