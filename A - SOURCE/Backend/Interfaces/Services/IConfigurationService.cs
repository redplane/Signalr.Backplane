namespace Signalr.Backplane.Service.Interfaces.Services
{
    public interface IConfigurationService
    {
        /// <summary>
        /// Activation token expiration time (in secs)
        /// </summary>
        int TokenExpiration { get; set; }

        /// <summary>
        /// Forgot password token expiration (in secs)
        /// </summary>
        int ForgotPasswordTokenExpiration { get; set; }
    }
}