using Signalr.Backplane.Service.Interfaces.Providers;

namespace Signalr.Backplane.Service.Providers
{
    public class BearerAuthenticationProvider : IBearerAuthenticationProvider
    {
        /// <summary>
        ///     How long a token can lives.
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        ///     Name of identity which is set to request claim.
        /// </summary>
        public string IdentityName { get; set; }

        /// <summary>
        ///     Key which is used for token encryption.
        /// </summary>
        public string Key { get; set; }
    }
}