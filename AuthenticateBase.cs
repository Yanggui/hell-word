using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using Moodys3.BizCore.Authorization.Interfaces.Domains;
using Moodys3.Common.ObjectBuilder;
using Moodys3.Data.Authorization.Interfaces.Services;
using Moodys3.WSPlatform.BizCore.Authorization.Interfaces.Domains;
using Moodys3.WSPlatform.Common.WebContext;

namespace Moodys3.WSPlatform.WebApi.Extension.Authentication
{
	public abstract class AuthenticateBase : FilterAttribute, IAuthenticationFilter
	{
		private static IWebServiceDataService webServiceDataService;

		static AuthenticateBase()
		{
			ObjectFactory.TryGetInstance(out webServiceDataService);
		}

        private static ICollection<IWebServiceOperation> AllWebServiceApis;

		protected string GetCurrentOperationTemplate(string theWebAPiTemplate)
		{
			if (!string.IsNullOrEmpty(theWebAPiTemplate))
			{
				if (AllWebServiceApis == null)
				{
					lock (new object())
					{
						AllWebServiceApis = GetAllApiInfo();
					}
				}

				if (theWebAPiTemplate.StartsWith("rest") && theWebAPiTemplate.Length > 4)
				{
					theWebAPiTemplate = theWebAPiTemplate.Substring(4);
				}

				var aMatchList = AllWebServiceApis.Where(x => x.Uri.StartsWith(theWebAPiTemplate)).ToList();
				if (aMatchList.Count == 0)
				{
					return theWebAPiTemplate;
				}

                if (WspContext.Current != null && WspContext.Current.Request != null)
				{
                    Uri aFull = WspContext.Current.Request.Url;
					if (!aFull.AbsolutePath.Contains("rest"))
					{
						return theWebAPiTemplate;
					}

				    string aBaseString = string.Format("{0}rest", aFull.OriginalString.Split(new string[] { "rest" }, StringSplitOptions.None)[0]);
				    var aBaseUri = new Uri(aBaseString);
				    foreach (var apiInfo in aMatchList)
					{
						var aTemplate = new UriTemplate(apiInfo.Uri);
						if (aTemplate.Match(aBaseUri, aFull) != null)
						{
                            return apiInfo.Uri;
						}
					}

					return theWebAPiTemplate;
				}

				return aMatchList.FirstOrDefault().Uri;
			}

			return string.Empty;
		}

		private ICollection<IWebServiceOperation> GetAllApiInfo()
		{
		   return webServiceDataService.GetWebServiceOperationList(null);
		}

		protected void SetCustomPrincipal(IPrincipal aPrincipal, PrincipalWrapper theOAuthPrincipalWarpper, UserPrincipalWrapper theUserPrincipalWrapper)
		{
			if (aPrincipal != null)
			{
				var aIdentities = new List<IIdentity> { aPrincipal.Identity };
                WspContext.Current.SetValue("Identities", aIdentities);
			}

            WspContext.Current.SetValue("UserPrincipalWrapper", theUserPrincipalWrapper);

            WspContext.Current.SetValue("OAuthPrincipalWarpper", theOAuthPrincipalWarpper);
		}

		public abstract Task AuthenticateAsync(HttpAuthenticationContext context, System.Threading.CancellationToken cancellationToken);

		public abstract Task ChallengeAsync(HttpAuthenticationChallengeContext context, System.Threading.CancellationToken cancellationToken);
	}
}
