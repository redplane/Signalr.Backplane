using System.Web.Http;
using Autofac;
using Autofac.Integration.SignalR;
using Autofac.Integration.WebApi;
using log4net.Config;
using Microsoft.AspNet.SignalR;
using Signalr.Backplane.Service.Modules;

namespace Signalr.Backplane.Service.Configs
{
    public class InversionOfControlConfig
    {
        #region Methods

        /// <summary>
        ///     Register list of inversion of controls.
        /// </summary>
        public static void Register()
        {
            // Initiate container builder to register dependency injection.
            var containerBuilder = new ContainerBuilder();

            #region Controllers & hubs

            // Controllers & hubs
            containerBuilder.RegisterApiControllers(typeof(Startup).Assembly);
            
            #endregion

            #region Unit of work & Database context

            // TODO:

            #endregion

            #region Services

            // TODO:
            //containerBuilder.RegisterType<SignalrAuthorizeAttribute>().InstancePerLifetimeScope();

            #endregion

            #region Modules

            // Log4net module registration (this is for logging)
            XmlConfigurator.Configure();
            containerBuilder.RegisterModule<LogModule>();

            #endregion

            #region Providers

            #endregion

            #region IoC build

            // Container build.
            var container = containerBuilder.Build();

            // Attach dependency injection into configuration.
            GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
            GlobalHost.DependencyResolver = new AutofacDependencyResolver(container);

            #endregion
        }

        #endregion
    }
}