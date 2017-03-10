using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Autofac;

namespace Signalr.Backplane.Service.Attributes
{
    public class ApiAuthorizeAttribute : AuthorizationFilterAttribute
    {
        #region Properties
        
        /// <summary>
        /// Autofac lifetime scope.
        /// </summary>
        public ILifetimeScope LifetimeScope { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///     Override this function for checking whether user is allowed to access function.
        /// </summary>
        /// <param name="httpActionContext"></param>
        /// <returns></returns>
        public override void OnAuthorization(HttpActionContext httpActionContext)
        {
            try
            {
                using (var lifetimeScope = LifetimeScope.BeginLifetimeScope())
                {
                    // Find the instance of unit of work.
                    var unitOfWork = lifetimeScope.Resolve<IUnitOfWork>();

                    #region Principle validation

                    // Find the principle of request.
                    var principle = httpActionContext.RequestContext.Principal;

                    // Principal is invalid.
                    if (principle == null)
                    {
                        // Anonymous request is allowed.
                        if (IsAllowAnonymousRequest(httpActionContext))
                            return;

                        httpActionContext.Response =
                            httpActionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized,
                                HttpMessages.InvalidAuthenticationToken);
                        return;
                    }
                    // Find the identity set in principle.
                    var identity = principle.Identity;
                    if (identity == null)
                    {
                        // Anonymous request is allowed.
                        if (IsAllowAnonymousRequest(httpActionContext))
                            return;

                        httpActionContext.Response =
                            httpActionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized,
                                HttpMessages.InvalidAuthenticationToken);
                        return;
                    }

                    #endregion

                    #region Claim identity

                    // Find the claim identity.
                    var claimIdentity = (ClaimsIdentity)identity;

                    // Claim doesn't contain email.
                    var claimEmail = claimIdentity.FindFirst(ClaimTypes.Email);
                    if ((claimEmail == null) || string.IsNullOrEmpty(claimEmail.Value))
                    {
                        // Anonymous request is allowed.
                        if (IsAllowAnonymousRequest(httpActionContext))
                            return;

                        httpActionContext.Response =
                            httpActionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized,
                                HttpMessages.InvalidAuthenticationToken);
                        return;
                    }

                    // Find email in the database.
                    var account = unitOfWork.Context.Accounts
                        .FirstOrDefault(x => x.Email.Equals(claimEmail.Value, StringComparison.InvariantCultureIgnoreCase));

                    // Account is not found.
                    if (account == null)
                    {
                        // Anonymous request is allowed.
                        if (IsAllowAnonymousRequest(httpActionContext))
                            return;

                        httpActionContext.Response =
                            httpActionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized,
                                HttpMessages.InvalidAuthenticationInfo);
                        return;
                    }

                    // TODO: Implement claim of password.

                    #endregion

                    #region Account status validation

                    // Account is waiting for confirmation.
                    if (account.Status == AccountStatus.Pending)
                    {
                        // Anonymous request is allowed.
                        if (IsAllowAnonymousRequest(httpActionContext))
                            return;

                        httpActionContext.Response = httpActionContext.Request.CreateErrorResponse(
                            HttpStatusCode.Forbidden, HttpMessages.AccountIsPending);
                        return;
                    }

                    // Account is forbidden to access function.
                    if (account.Status == AccountStatus.Disabled)
                    {
                        // Anonymous request is allowed.
                        if (IsAllowAnonymousRequest(httpActionContext))
                            return;

                        httpActionContext.Response = httpActionContext.Request.CreateErrorResponse(
                            HttpStatusCode.Forbidden, HttpMessages.AccountIsDisabled);
                        return;
                    }

                    #endregion

                    // Insert account information into HttpItem for later use.
                    var properties = httpActionContext.Request.Properties;
                    if (properties.ContainsKey(ClaimTypes.Actor))
                        properties[ClaimTypes.Actor] = account;
                    else
                        properties.Add(ClaimTypes.Actor, account);

                    
                }
            }
            catch
            {
                // Anonymous request is allowed.
                if (IsAllowAnonymousRequest(httpActionContext))
                    return;

                httpActionContext.Response = httpActionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized,
                    HttpMessages.InvalidAuthenticationInfo);
            }
        }

        /// <summary>
        ///     Whether method or controller allows anonymous requests or not.
        /// </summary>
        /// <param name="httpActionContext"></param>
        /// <returns></returns>
        private bool IsAllowAnonymousRequest(HttpActionContext httpActionContext)
        {
#if UNAUTHENTICATION_ALLOW
            return true;
#endif
            return httpActionContext.ActionDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().Any()
                   ||
                   httpActionContext.ControllerContext.ControllerDescriptor.GetCustomAttributes<AllowAnonymousAttribute>
                       ().Any();
        }

        #endregion
    }
}