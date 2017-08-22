using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using DotNetOpenAuth.OAuth.ChannelElements;
using Moodys3.Common.ObjectBuilder;
using Moodys3.WSPlatform.BizCore.Authorization.Caching;
using Moodys3.WSPlatform.BizCore.Authorization.Interfaces.Domains;
using Moodys3.WSPlatform.BizCore.Authorization.Interfaces.Services;
using Moodys3.WSPlatform.Common;
using Moodys3.WSPlatform.Common.Exceptions;
using Moodys3.WSPlatform.Common.WebContext;
using Moodys3.WSPlatform.WebApi.Extension.Utils;

namespace Moodys3.WSPlatform.WebApi.Extension.Authentication
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class AuthenticateInternalAttribute : AuthenticateBase
	{
		private readonly AuthorizationCacheService authorizationCacheService;
		private const string UserNameQs = "user_name";

		private IUserAuthorizationService userAuthorizationService;

		public AuthenticateInternalAttribute()
		{
            ObjectFactory.TryGetInstance(out authorizationCacheService);
			ObjectFactory.TryGetInstance(out userAuthorizationService);
		}

		public override Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
		{
            var aOAuthPrincipal = context.Request.GetOAuthPrincipalFromGatewayRequest();
            string aUserName;
            context.Request.TryGetValue(UserNameQs, out aUserName);

            if (aOAuthPrincipal != null)
            {
                SetPrincipalByOAuthPrincipal(context, aOAuthPrincipal);
            }
            else if (!string.IsNullOrEmpty(aUserName))
            {
                SetPrincipalByUserName(context, aUserName);
            }
            else
            {
                SetAnonymousPrincipal(context);
            }

            var aOperationName = context.ActionContext.ActionDescriptor.ActionName;
            var aOperationTemplate = context.ActionContext.ActionDescriptor.GetUriTemplate();
            if (string.IsNullOrEmpty(aOperationTemplate))
            {
                var aApiTemplate = context.Request.GetRouteTemplate();
                aOperationTemplate = GetCurrentOperationTemplate(aApiTemplate);
            }

            WspContext.Current.SetValue(WsConstants.OPERATION_TEMPLATE, aOperationTemplate);
            WspContext.Current.SetValue(WsConstants.OPERATION_NAME, aOperationName);

            return Task.FromResult<object>(null);
        }

		public override Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
		{
			return Task.FromResult<object>(null);
		}

        private void SetPrincipalByOAuthPrincipal(HttpAuthenticationContext theContext, PrincipalWrapper theOAuthPrincipal)
        {
            var aUserName = !string.IsNullOrEmpty(theOAuthPrincipal.UserName) ? 
                            theOAuthPrincipal.UserName : 
                            string.Format("client:{0}", theOAuthPrincipal.ClientIdentifier);
            
            var aUserPrincipalWrapper = authorizationCacheService.Get(aUserName);
            if (aUserPrincipalWrapper == null)
            {
                var aUserPrincipal = userAuthorizationService.GetWebServiceUserPrincipal(aUserName);
                aUserPrincipalWrapper = UserPrincipalWrapper.NewInstance(aUserPrincipal);
                Task.Run(() => authorizationCacheService.Put(aUserName, aUserPrincipalWrapper));
            }

            var aPrincipal = new OAuthPrincipal(aUserName, new string[0]);
            SetCustomPrincipal(aPrincipal, theOAuthPrincipal, aUserPrincipalWrapper);

            var aUserAccessInfoPrincipal = new UserAccessInfoPrincipal(aPrincipal.Identity)
            {
                UserPrincipalWrapper = aUserPrincipalWrapper,
                PrincipalWarpper = theOAuthPrincipal
            };
            theContext.Principal = aUserAccessInfoPrincipal;
        }

        private void SetPrincipalByUserName(HttpAuthenticationContext theContext, string theUserName)
        {
            var aUserPrincipalWrapper = authorizationCacheService.Get(theUserName);
            if (aUserPrincipalWrapper == null)
            {
                var aUserPrincipal = userAuthorizationService.GetWebServiceUserPrincipal(theUserName);
                if (aUserPrincipal != null)
                {
                    aUserPrincipalWrapper = UserPrincipalWrapper.NewInstance(aUserPrincipal);
                    Task.Run(() => authorizationCacheService.Put(theUserName, aUserPrincipalWrapper));
                }
                else
                {
                    throw new WspException(WspExceptionCode.AccessDenied, "Invalid user.");
                }
            }

            var aPrincipal = new OAuthPrincipal(theUserName, new string[0]);
            SetCustomPrincipal(aPrincipal, null, aUserPrincipalWrapper);

            var aUserAccessInfoPrincipal = new UserAccessInfoPrincipal(aPrincipal.Identity)
            {
                UserPrincipalWrapper = aUserPrincipalWrapper,
            };
            theContext.Principal = aUserAccessInfoPrincipal;
        }

        private void SetAnonymousPrincipal(HttpAuthenticationContext theContext)
        {
            var aPrincipal = new OAuthPrincipal(WsConstants.ANONYMOUS_USER_NAME, null);
            var aUserPrincipalWrapper = GetAnonymousUserPricipal();

            SetCustomPrincipal(aPrincipal, null, aUserPrincipalWrapper);

            var aUserAccessInfoPrincipal = new UserAccessInfoPrincipal(aPrincipal.Identity)
            {
                UserPrincipalWrapper = aUserPrincipalWrapper,
            };
            theContext.Principal = aUserAccessInfoPrincipal;
        }

        private UserPrincipalWrapper GetAnonymousUserPricipal()
		{
			var aUserPrincipalWrapper = new UserPrincipalWrapper();
			aUserPrincipalWrapper.IsAdmin = false;
            aUserPrincipalWrapper.IsAnonymous = true;
            aUserPrincipalWrapper.UserId = Guid.NewGuid();
			aUserPrincipalWrapper.UserName = WsConstants.ANONYMOUS_USER_NAME;
			aUserPrincipalWrapper.ProductRights = new List<ProductRight>();
            return aUserPrincipalWrapper;
		}
    }
}