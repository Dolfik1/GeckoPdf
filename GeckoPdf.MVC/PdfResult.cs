using GeckoPdf.Config;
using GeckoPdf.MVC.Extensions;
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

        private string _viewName;
        public string ViewName
        {
            get { return _viewName ?? string.Empty; }
            set { _viewName = value; }
        }

        private string _masterName;
        public string MasterName
        {
            get { return _masterName ?? string.Empty; }
            set { _masterName = value; }
        }

        public object Model { get; set; }

        public PdfResult(GeckoPdfConfig config)
        {
            MasterName = string.Empty;
            ViewName = string.Empty;
            Model = null;
            _config = config;
        }

        public PdfResult(string viewName, GeckoPdfConfig config)
            : this(config)
        {
            ViewName = viewName;
        }

        public PdfResult(object model, GeckoPdfConfig config)
            : this(config)
        {
            Model = model;
        }

        public PdfResult(string viewName, object model, GeckoPdfConfig config)
            : this(config)
        {
            ViewName = viewName;
            Model = model;
        }

        public PdfResult(string viewName, string masterName, object model, GeckoPdfConfig config)
            : this(viewName, model, config)
        {
            MasterName = masterName;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var headers = context.HttpContext.Request.Headers;
            var cookies = context.HttpContext.Request.Cookies;

            var geckoCookies = new List<GeckoCookie>();
            foreach(var key in cookies.AllKeys)
            {
                var c = cookies[key];
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

            var viewName = ViewName;
            if (string.IsNullOrEmpty(viewName))
                viewName = context.RouteData.GetRequiredString("action");

            var viewResult = GetView(context, viewName, MasterName);
            var html = context.GetHtmlFromView(viewResult, viewName, Model);

            var tempDir = context.HttpContext.Server.MapPath("~/App_Data/temp");
            var tempFile = Path.Combine(tempDir, Guid.NewGuid().ToString() + ".tmp");

            var bytes = new GeckoPdf(_config).ConvertHtml(context.HttpContext.Request.Url.AbsoluteUri, html, null, geckoCookies, tempFile);

            var response = PrepareResponse(context.HttpContext.Response);
            response.OutputStream.Write(bytes, 0, bytes.Length);
        }

        private HttpResponseBase PrepareResponse(HttpResponseBase response)
        {
            response.ContentType = "application/pdf";

            if (!string.IsNullOrEmpty(_config.FileName))
                response.AddHeader("Content-Disposition", string.Format("attachment; filename=\"{0}\"", SanitizeFileName(_config.FileName)));

            response.AddHeader("Content-Type", response.ContentType);

            return response;
        }

        protected virtual ViewEngineResult GetView(ControllerContext context, string viewName, string masterName)
        {
            return ViewEngines.Engines.FindView(context, viewName, MasterName);
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
