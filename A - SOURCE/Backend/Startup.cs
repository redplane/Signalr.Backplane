using System.Web.Http;
using System.Web.Routing;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;
using Signalr.Backplane.Service;
using Signalr.Backplane.Service.Configs;

[assembly: OwinStartup(typeof(Startup))]

namespace Signalr.Backplane.Service
{
    public class Startup
    {
        /// <summary>
        ///     Configuration function of OWIN Startup.
        /// </summary>
        /// <param name="app"></param>
        public void Configuration(IAppBuilder app)
        {
            // Register web api configuration.
            GlobalConfiguration.Configure(ApiRouteConfig.Register);
            
            // Dependency injection registration.
            InversionOfControlConfig.Register();

            // Use redis for being backplane.
            GlobalHost.DependencyResolver.UseRedis("redis-11722.c8.us-east-1-4.ec2.cloud.redislabs.com", 11722,
                "Fingerlickinggood", "iConfess");

            // Map signalr hubs.
            app.Map("/signalr", map =>
            {
                map.UseCors(CorsOptions.AllowAll);
                var hubConfiguration = new HubConfiguration();
                map.RunSignalR(hubConfiguration);
            });
            
            // Use cors.
            app.UseCors(CorsOptions.AllowAll);
        }
    }
}