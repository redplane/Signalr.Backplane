using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNet.SignalR.Hubs;

namespace Signalr.Backplane.Service.SignalrHubs
{
    [HubName("SystemMessage")]
    public class SystemMessageHub : ParentHub
    {
        /// <summary>
        /// Callback when client disconnects from hub.
        /// </summary>
        /// <param name="stopCalled"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public override Task OnDisconnected(bool stopCalled)
        {
            return base.OnDisconnected(stopCalled);
        }
    }
}