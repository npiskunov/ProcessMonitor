using System;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProcessMonitorWeb.Pages
{
    public class DocsModel : PageModel
    {
        private IHtmlContent GetEnumDescription(Type t)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<div><h3>{t.Name}</h3><ul>");
            foreach (var item in Enum.GetValues(t))
            {
                sb.AppendLine($"<li>{item} = {(int)item}</li>");
            }
            sb.AppendLine("</ul></div>");
            return new HtmlString(sb.ToString());
        }

        public IHtmlContent GetTypeDescription(Type t)
        {
            var sb = new StringBuilder();
            if (t.IsEnum)
            {
                return GetEnumDescription(t);
            }

            sb.AppendLine($"<div><h3>{t.Name}</h3><ul>");
            foreach (var prop in t.GetProperties())
            {
                var description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description;
                sb.AppendLine($"<li>[{prop.PropertyType.Name}] <b>{prop.Name}</b> : <i>{description}</i></li>");
            }
            sb.AppendLine("</ul></div>");
            return new HtmlString(sb.ToString());
        }
        public void OnGet()
        {

        }
    }
}