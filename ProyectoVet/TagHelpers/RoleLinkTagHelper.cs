using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using ProyectoVet.Security;

namespace ProyectoVet.TagHelpers;

[HtmlTargetElement("a", Attributes = "asp-action")]
public class RoleLinkTagHelper : TagHelper
{
    public override int Order => 1000;
    [HtmlAttributeName("asp-controller")]
    public string? Controller { get; set; }
    [HtmlAttributeName("asp-action")]
    public string Action { get; set; } = "";
    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var controller = Controller ?? ViewContext.RouteData.Values["controller"]?.ToString() ?? "";
        if (!RoleAccess.CanAccess(ViewContext.HttpContext.User, controller, Action)) output.SuppressOutput();
    }
}
