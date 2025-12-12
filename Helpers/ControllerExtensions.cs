using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CAT.AID.Web.Helpers
{
    public static class ControllerExtensions
    {
        public static async Task<string> RenderViewToStringAsync(
            this Controller controller,
            string viewName,
            object model,
            bool isPartial = false)
        {
            if (string.IsNullOrWhiteSpace(viewName))
                throw new ArgumentNullException(nameof(viewName));

            controller.ViewData.Model = model;

            await using var sw = new StringWriter();

            var viewEngine = controller.HttpContext.RequestServices
                .GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;

            if (viewEngine == null)
                throw new Exception("❌ ICompositeViewEngine not found. Ensure MVC + Razor services are registered.");

            var viewResult = viewEngine.FindView(controller.ControllerContext, viewName, false);

            if (!viewResult.Success)
                throw new Exception($"❌ View '{viewName}' not found.\nSearched:\n{string.Join("\n", viewResult.SearchedLocations)}");

            var view = viewResult.View;

            var viewContext = new ViewContext(
                controller.ControllerContext,
                view,
                controller.ViewData,
                controller.TempData,
                sw,
                new HtmlHelperOptions()
            );

            await view.RenderAsync(viewContext);
            return sw.ToString();
        }
    }
}
