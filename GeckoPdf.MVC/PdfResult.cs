using GeckoPdf.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace GeckoPdf.MVC
{
    public class PdfResult : ActionResult
    {
        private GeckoPdfConfig _config;
        private RouteValueDictionary routeValuesDict;
        private object routeValues;
        private string action;

        public PdfResult(GeckoPdfConfig config)
            : base()
        {
            _config = config;
        }

        public PdfResult(GeckoPdfConfig config, string action)
            : this(config)
        {
            this.action = action;
        }

        public PdfResult(GeckoPdfConfig config, string action, RouteValueDictionary routeValues)
            : this(config, action)
        {
            routeValuesDict = routeValues;
        }

        public PdfResult(GeckoPdfConfig config, string action, object routeValues)
            : this(config, action)
        {
            this.routeValues = routeValues;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var headers = context.HttpContext.Request.Headers;
            var cookies = context.HttpContext.Request.Cookies;

            var geckoCookies = new List<GeckoCookie>();
            foreach(HttpCookie c in cookies)
            {
                geckoCookies.Add(new GeckoCookie
                {
                    Name = c.Name,
                    Value = c.Value,
                    Path = c.Path,
                    ExpiresUnix = DateTimeToUnixTimestamp(c.Expires),
                    HttpOnly = c.HttpOnly,
                    Secure = c.Secure
                }); 
            }
            
            var ms = new GeckoPdf(_config).Convert(GetUrl(context), headers, geckoCookies);

            var bytes = ms.GetBuffer();
            ms.Dispose();

            var response = PrepareResponse(context.HttpContext.Response);
            response.OutputStream.Write(bytes, 0, bytes.Length);
        }

        private string GetUrl(ControllerContext context)
        {
            var urlHelper = new UrlHelper(context.RequestContext);

            string actionUrl = string.Empty;
            if (routeValues == null)
                actionUrl = urlHelper.Action(action, routeValuesDict);
            else if (routeValues != null)
                actionUrl = urlHelper.Action(action, routeValues);
            else
                actionUrl = urlHelper.Action(action);

            string url = string.Format("{0}://{1}{2}", context.HttpContext.Request.Url.Scheme, context.HttpContext.Request.Url.Authority, actionUrl);
            return url;
        }

        private HttpResponseBase PrepareResponse(HttpResponseBase response)
        {
            response.ContentType = "application/pdf";

            if (!string.IsNullOrEmpty(_config.FileName))
                response.AddHeader("Content-Disposition", string.Format("attachment; filename=\"{0}\"", SanitizeFileName(_config.FileName)));

            response.AddHeader("Content-Type", response.ContentType);

            return response;
        }

        #region Helpers

        private long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (long)(TimeZoneInfo.ConvertTimeToUtc(dateTime) -
                   new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        private static string SanitizeFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidPathChars()) + new string(Path.GetInvalidFileNameChars()));
            string invalidCharsPattern = string.Format(@"[{0}]+", invalidChars);

            string result = Regex.Replace(name, invalidCharsPattern, "_");
            return result;
        }

        #endregion
    }
}
