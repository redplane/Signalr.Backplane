namespace Signalr.Backplane.Service.Interfaces.Providers
{
    public interface IBearerAuthenticationProvider
    {
        #region Properties

        /// <summary>
        ///     Key which is used for protecting token.
        /// </summary>
        string Key { get; set; }

        /// <summary>
        ///     Life-time of token (second)
        /// </summary>
        int Duration { get; set; }

        /// <summary>
        ///     Name of claim identity.
        /// </summary>
        string IdentityName { get; set; }

        #endregion
    }
}