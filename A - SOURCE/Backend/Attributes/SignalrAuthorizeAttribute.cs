using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web.Http;
using Autofac;
using JWT;
using log4net;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Signalr.Backplane.Service.Interfaces.Providers;

namespace Signalr.Backplane.Service.Attributes
{
    public class SignalrAuthorizeAttribute : Attribute, IAuthorizeHubConnection, IAuthorizeHubMethodInvocation
    {
        #region Properties

        /// <summary>
        /// Life time scope of autofac.
        /// </summary>
        private ILifetimeScope _lifetimeScope;

        /// <summary>
        /// Instance of logging service.
        /// </summary>
        private ILog _log;

        /// <summary>
        ///     Autofac life time scope.
        /// </summary>
        public ILifetimeScope LifetimeScope
        {
            get
            {
                if (_lifetimeScope == null)
                    _lifetimeScope = GlobalHost.DependencyResolver.Resolve<ILifetimeScope>();
                return _lifetimeScope;
            }
            set { _lifetimeScope = value; }
        }

        /// <summary>
        /// Logging instance.
        /// </summary>
        public ILog Log
        {
            get
            {
                if (_log == null)
                    _log = LogManager.GetLogger(typeof(SignalrAuthorizeAttribute));

                return _log;
            }
            set { _log = value; }
        }

        /// <summary>
        ///     Roles which can access to controller/method.
        /// </summary>
        public AccountRole[] Roles { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initiate attribute without roles.
        /// </summary>
        public SignalrAuthorizeAttribute()
        {
        }

        /// <summary>
        /// Initiate attribute with a specific role.
        /// </summary>
        /// <param name="role"></param>
        public SignalrAuthorizeAttribute(AccountRole role)
        {
            Roles = new[] {role};
        }

        /// <summary>
        /// Initiate attribute with specific roles.
        /// </summary>
        /// <param name="roles"></param>
        public SignalrAuthorizeAttribute(AccountRole[] roles)
        {
            Roles = roles;
        }
        #endregion

        #region Methods

        /// <summary>
        ///     Callback which is fired when connection has been made to controller/method.
        /// </summary>
        /// <param name="hubDescriptor"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public bool AuthorizeHubConnection(HubDescriptor hubDescriptor, IRequest request)
        {
            // Whether anonymous access is allowed or not.
            if (IsAnonymousAllowed(hubDescriptor))
                return true;

            using (var lifeTimeScope = LifetimeScope.BeginLifetimeScope())
            {
                return IsAccessible(lifeTimeScope, request);
            }
        }

        /// <summary>
        ///     Callback which is fired when user is invoking controller/method.
        /// </summary>
        /// <param name="hubIncomingInvokerContext"></param>
        /// <param name="appliesToMethod"></param>
        /// <returns></returns>
        public bool AuthorizeHubMethodInvocation(IHubIncomingInvokerContext hubIncomingInvokerContext,
            bool appliesToMethod)
        {
            var hub = hubIncomingInvokerContext.Hub;
            var request = hub.Context.Request;
            var hubDescriptor = hubIncomingInvokerContext.MethodDescriptor.Hub;

            // Whether anonymous access is allowed or not.
            if (IsAnonymousAllowed(hubDescriptor))
                return true;

            using (var lifeTimeScope = LifetimeScope.BeginLifetimeScope())
            {
                // Find logging service.
                var log = lifeTimeScope.Resolve<ILog>();

                #region Request validation

                if (request.User == null)
                {
                    InitiateErrorMessage(log, "No user identity has been attached to the request.");
                    return false;
                }

                if (request.User.Identity == null)
                {
                    InitiateErrorMessage(log, "No user identity has been attached to the request.");
                    return false;
                }

                #endregion

                return IsAccessible(lifeTimeScope, request);
            }
        }

        /// <summary>
        ///     Check whether hub is accessible or not.
        /// </summary>
        /// <param name="lifeTimeScope"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private bool IsAccessible(ILifetimeScope lifeTimeScope, IRequest request)
        {
            // Find logging service.
            var unitOfWork = lifeTimeScope.Resolve<IUnitOfWork>();
            var httpContext = request.GetHttpContext();

            #region Identity check.

            // Find request principle.
            var principle = request.User;

            // Request has been authenticated before.
            if ((principle != null) && (principle.Identity != null) && principle.Identity.IsAuthenticated)
                return true;

            #endregion

            #region Request url token search

            // Find url of signalr request.
            var url = request.QueryString;

            // Find authentication token from request url.
            var authenticationToken = url.Get(nameof(Authorization));
            if (string.IsNullOrWhiteSpace(authenticationToken))
                return false;

            // Find bearer authentication provider from services.
            var bearerAuthenticationProvider = lifeTimeScope.Resolve<IBearerAuthenticationProvider>();
            if (bearerAuthenticationProvider == null)
            {
                InitiateErrorMessage(Log, "No bearer provider has been installed into signalr hub.");
                return false;
            }

            #endregion

            #region Authentication token validation

            // Decode the token and set to claim. The object should be in dictionary.
            var claimPairs = JsonWebToken.DecodeToObject<Dictionary<string, string>>(authenticationToken,
                bearerAuthenticationProvider.Key);

            var claimIdentity = new ClaimsIdentity(null, bearerAuthenticationProvider.IdentityName);
            foreach (var key in claimPairs.Keys)
                claimIdentity.AddClaim(new Claim(key, claimPairs[key]));

            #endregion

            #region Claim identity

            // Claim doesn't contain email.
            var claimEmail = claimIdentity.FindFirst(ClaimTypes.Email);
            if ((claimEmail == null) || string.IsNullOrEmpty(claimEmail.Value))
                return false;

            // Find email in the database.
            var account = unitOfWork.Context.Accounts
                .FirstOrDefault(x => x.Email.Equals(claimEmail.Value, StringComparison.InvariantCultureIgnoreCase));

            // Account is not found.
            if (account == null)
                return false;

            // TODO: Implement claim of password.

            #endregion

            #region Account status validation

            // Account is waiting for confirmation.
            if (account.Status == AccountStatus.Pending)
            {
                InitiateErrorMessage(Log, "(SignalR) Account is pending");
                return false;
            }

            // Account is forbidden to access function.
            if (account.Status == AccountStatus.Disabled)
            {
                InitiateErrorMessage(Log, "(SignalR) Account is disabled");
                return false;
            }

            #endregion

            #region Roles validation

            if (Roles != null)
            {
                if (!Roles.Any(x => x == account.Role))
                {
                    InitiateErrorMessage(Log, "(SignalR) Role is invalid");
                    return false;
                }
            }

            #endregion
            
            // Insert account information into HttpItem for later use.
            var properties = httpContext.Items;
            if (properties.Contains(ClaimTypes.Actor))
                properties[ClaimTypes.Actor] = account;
            else
                properties.Add(ClaimTypes.Actor, account);


            // Authenticate the request.
            httpContext.User = new ClaimsPrincipal(claimIdentity);

            return true;
        }

        /// <summary>
        ///     Whether hub allows anonymous access or not.
        /// </summary>
        /// <param name="hubDescriptor"></param>
        /// <returns></returns>
        private bool IsAnonymousAllowed(HubDescriptor hubDescriptor)
        {
            return hubDescriptor.HubType.IsDefined(typeof(AllowAnonymousAttribute), true);
        }

        /// <summary>
        ///     Initiate error message into log file.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        private void InitiateErrorMessage(ILog log, object message, Exception exception)
        {
            if (log == null)
                return;

            log.Error(message, exception);
        }

        /// <summary>
        ///     Initiate error message into log file.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="message"></param>
        private void InitiateErrorMessage(ILog log, object message)
        {
            if (log == null)
                return;

            log.Error(message);
        }

        #endregion
    }
}