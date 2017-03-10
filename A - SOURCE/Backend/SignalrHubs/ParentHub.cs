using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Autofac;
using log4net;
using Microsoft.AspNet.SignalR;

namespace Signalr.Backplane.Service.SignalrHubs
{
    public class ParentHub : Hub
    {
        #region Properties

        /// <summary>
        ///     Autofac lifetime scope.
        /// </summary>
        private ILifetimeScope _lifetimeScope;

        /// <summary>
        /// Logging service.
        /// </summary>
        private ILog _log;

        /// <summary>
        ///     Autofac lifetime scope.
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
        /// Logging service.
        /// </summary>
        public ILog Log
        {
            get
            {
                if (_log == null)
                    _log = LogManager.GetLogger(typeof(ParentHub));
                return _log;
            }
            set { _log = value; }
        }

        #endregion
        
        #region Methods

        /// <summary>
        ///     Callback which is fired when a client connected
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnected()
        {
            // Find account from request.
            var httpContext = Context.Request.GetHttpContext();

            // Http context is invalid.
            if (httpContext == null)
                return;

            // Find account from request.
            var account = (Account) httpContext.Items[ClaimTypes.Actor];
            if (account == null)
                return;

            // Begin new life time scope.
            using (var lifeTimeScope = LifetimeScope.BeginLifetimeScope())
            {
                // Find unit of work of life time scope.
                var unitOfWork = lifeTimeScope.Resolve<IUnitOfWork>();
                var timeService = lifeTimeScope.Resolve<ITimeService>();

                try
                {
                    // Find and update connection to the account.
                    var signalrConnection = new SignalrConnection();
                    signalrConnection.OwnerIndex = account.Id;
                    signalrConnection.Index = Context.ConnectionId;
                    signalrConnection.Created = timeService.DateTimeUtcToUnix(DateTime.UtcNow);

                    unitOfWork.RepositorySignalrConnections.Initiate(signalrConnection);
                    await unitOfWork.CommitAsync();

                    Log.Info(
                        $"Connection (Id: {Context.ConnectionId}) has been established from account (Email: {account.Email})");
                }
                catch (Exception exception)
                {
                    Log.Error(exception.Message, exception);
                }
            }
        }

        /// <summary>
        ///     Callback which is fired when a client disconnected from
        /// </summary>
        /// <param name="stopCalled"></param>
        /// <returns></returns>
        public override async Task OnDisconnected(bool stopCalled)
        {
            using (var lifeTimeScope = LifetimeScope.BeginLifetimeScope())
            {
                // Find unit of work from life time scope.
                var unitOfWork = lifeTimeScope.Resolve<IUnitOfWork>();

                // Search for record whose index is the same as connection index.
                var condition = new FindSignalrConnectionViewModel();
                condition.Index = new TextSearch();
                condition.Index.Mode = TextComparision.EqualIgnoreCase;
                condition.Index.Value = Context.ConnectionId;

                unitOfWork.RepositorySignalrConnections.Delete(condition);
                await unitOfWork.CommitAsync();
            }
        }

        /// <summary>
        ///     Callback which is fired when a client reconnected to server.
        /// </summary>
        /// <returns></returns>
        public override async Task OnReconnected()
        {
            await OnConnected();
        }

        #endregion
    }
}