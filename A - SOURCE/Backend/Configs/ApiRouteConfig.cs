using System.Web.Http;
using Newtonsoft.Json.Serialization;

namespace Signalr.Backplane.Service.Configs
{
    public static class ApiRouteConfig
    {
        /// <summary>
        ///     Config routes of web api.
        /// </summary>
        /// <param name="config"></param>
        public static void Register(HttpConfiguration config)
        {
            // Make json returned in camelcase.
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver =
                new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.UseDataContractJsonSerializer = false;

            // Enable CORS
            config.EnableCors();
            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{id}", new {id = RouteParameter.Optional}
            );


            config.Routes.MapHttpRoute("ApiRequireAction", "api/{controller}/{action}/{id}",
                new {id = RouteParameter.Optional}
            );
        }
    }
}