using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth.ChannelElements;
using DotNetOpenAuth.OAuth2.ChannelElements;
using log4net;
using Moodys3.BizCore.OAuth2;
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
    public class AuthenticateAttribute : AuthenticateBase
    {
        private readonly OAuthResourceService oAuthResourceService = new OAuthResourceService();
        private readonly AuthorizationCacheService authorizationCacheService;
        public const string ACCESS_TOKEN_QS = "access_token";
        private IUserAuthorizationService userAuthorizationService;
        private static readonly ILog logger = LogManager.GetLogger("WSLog");

        public AuthenticateAttribute()
        {
            ObjectFactory.TryGetInstance(out authorizationCacheService);
            ObjectFactory.TryGetInstance(out userAuthorizationService);
        }

        public override Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            IPrincipal aPrincipal = context.Principal;
            PrincipalWrapper aOAuthPrincipalWrapper = null;

            if (aPrincipal == null || aPrincipal.Identity.IsAuthenticated == false)
            {
                #region Verify access_token

                string aToken = string.Empty;
                var request = context.Request;
                if (request.TryGetValue(ACCESS_TOKEN_QS, out aToken))
                {
                    var aWrapper = authorizationCacheService.GetToken(aToken);
                    if (aWrapper == null)
                    {
                        IAuthorizationDescription aAuthorDescription = null;
                        try
                        {
                            aPrincipal = oAuthResourceService.VerifyOAuth2(
                                aToken,
                                request.Method.ToString(),
                                request.RequestUri,
                                GetHeaderCollection(request),
                                out aAuthorDescription);
                        }
                        catch (ProtocolFaultResponseException e)
                        {
                            string aMessage;
                            if (e.Message.Contains("The message expired at"))
                            {
                                logger.Warn(e.Message, e);
                                throw new WspException(WspExceptionCode.ERR_AUTH_003);
                            }
                            else
                            {
                                logger.Error(e.Message + " |t=" + aToken, e);
                                throw new WspException(WspExceptionCode.ERR_AUTH_002);
                            }
                        }

                        if (aPrincipal != null)
                        {
                            var aOAuthPrincipal = aPrincipal as OAuthPrincipal;
                            aOAuthPrincipalWrapper = new PrincipalWrapper
                            {
                                UserName = aOAuthPrincipal.Identity.Name,
                                Roles = new Collection<string>(aOAuthPrincipal.Roles)
                            };
                            if (aAuthorDescription != null)
                            {
                                aOAuthPrincipalWrapper.ClientIdentifier = aAuthorDescription.ClientIdentifier;
                                aOAuthPrincipalWrapper.Scope = aAuthorDescription.Scope;
                                aOAuthPrincipalWrapper.UtcIssued = aAuthorDescription.UtcIssued;
                            }
                            
                            Task.Run(() =>
                                {
                                    authorizationCacheService.PutToken(aToken, aOAuthPrincipalWrapper);
                                    logger.Info(aOAuthPrincipalWrapper.UserName + " |t=" + aToken);//for troubleshooting
                                });
                        }
                    }
                    else
                    {
                        aOAuthPrincipalWrapper = aWrapper;
                        aPrincipal = new OAuthPrincipal(aWrapper.UserName, aWrapper.Roles != null ? aWrapper.Roles.ToArray() : new string[0]);
                    }
                }
                else
                {
                    throw new WspException(WspExceptionCode.ERR_AUTH_001);
                }

                #endregion
            }

            if (aPrincipal == null)
            {
                throw new WspException(WspExceptionCode.ERR_AUTH_002);
            }

            #region Create UserPrincipal by user name

            UserPrincipalWrapper aUserPrincipalWrapper =
                authorizationCacheService.Get(aPrincipal.Identity.Name);
            if (aUserPrincipalWrapper == null)
            {
                var aUserPrincipal = userAuthorizationService.GetWebServiceUserPrincipal(aPrincipal.Identity.Name);
                aUserPrincipalWrapper = UserPrincipalWrapper.NewInstance(aUserPrincipal);
                Task.Run(() => authorizationCacheService.Put(aPrincipal.Identity.Name, aUserPrincipalWrapper));
            }

            SetCustomPrincipal(aPrincipal, aOAuthPrincipalWrapper, aUserPrincipalWrapper);

            var aUserAccessInfoPrincipal = new UserAccessInfoPrincipal(aPrincipal.Identity)
            {
                UserPrincipalWrapper = aUserPrincipalWrapper,
                PrincipalWarpper = aOAuthPrincipalWrapper
            };

            context.Principal = aUserAccessInfoPrincipal;

            #endregion

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

        private NameValueCollection GetHeaderCollection(HttpRequestMessage theRequest)
        {
            if (theRequest == null)
            {
                return null;
            }

            NameValueCollection headers = new NameValueCollection();
            foreach (var httpRequestHeader in theRequest.Headers)
            {
                headers.Add(httpRequestHeader.Key, httpRequestHeader.Value.FirstOrDefault());
            }

            return headers;
        }
    }
}
