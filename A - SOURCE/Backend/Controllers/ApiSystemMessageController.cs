using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.AspNet.SignalR;
using Signalr.Backplane.Service.SignalrHubs;
using Signalr.Backplane.Shared.Models;

namespace Signalr.Backplane.Service.Controllers
{
    [RoutePrefix("api/system-message")]
    public class ApiSystemMessageController : ApiController
    {
        /// <summary>
        /// Publish a message to all connectors.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [Route("")]
        [HttpPost]
        public HttpResponseMessage Publish([FromBody] SendMessage parameters)
        {
            if (parameters == null)
            {
                parameters = new SendMessage();
                Validate(parameters);
            }

            if (!ModelState.IsValid)
                return Request.CreateResponse(HttpStatusCode.BadRequest);

            var message = new PublishMessage();
            message.Id = Guid.NewGuid().ToString();
            message.Message = parameters;

            // Find system message hub.
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<SignalrMessageHub>();
            hubContext.Clients.All.obtainSystemMessage(message);
            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}