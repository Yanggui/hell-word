using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Moodys3.WSPlatform.Common.Exceptions;
using System.Net.Http;
using Microsoft.Security.Application;
using System.Text.RegularExpressions;
using System.Web;

namespace Moodys3.WSPlatform.WebApi.Extension.AntiXss
{
    public class AntiXssHttpMessageHandler : DelegatingHandler
    {
        private static Regex scriptExp = new Regex("<script[\\s\\S]+</script>", RegexOptions.IgnoreCase);
        private static Regex jsExp = new Regex("javascript:", RegexOptions.IgnoreCase);

        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage Request, System.Threading.CancellationToken cancellationToken)
        {
            var aQueryItems = Request.RequestUri.ParseQueryString();
            bool aQueryStringChanged = false;
            foreach (var key in aQueryItems.AllKeys)
            {
                var requestValue = Request.RequestUri.ParseQueryString()[key];

                if (scriptExp.IsMatch(requestValue.ToLower()) || jsExp.IsMatch(requestValue.ToLower()))
                {
                    throw new WspException(WspExceptionCode.InvalidRequest, "The request is potentially dangerous because it might include HTML markup or script.");
                }

                var item = Sanitizer.GetSafeHtmlFragment(requestValue);
                if (!item.Equals(requestValue))
                {
                    aQueryStringChanged = true;
                    //decode the item value for portfolio api : SearchPortfolios, portfolio name need to match the value; even though it would be a risk.  
                    aQueryItems.Set(key, HttpUtility.HtmlDecode(item));
                }
            }

            if (aQueryStringChanged)
            {
                var uriBuilder = new UriBuilder(Request.RequestUri);
                uriBuilder.Query = aQueryItems.ToString();
                Request.RequestUri = uriBuilder.Uri;
            }

            return base.SendAsync(Request, cancellationToken);
        }
    }
}
