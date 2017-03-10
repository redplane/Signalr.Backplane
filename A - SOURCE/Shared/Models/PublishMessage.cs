namespace Signalr.Backplane.Shared.Models
{
    public class PublishMessage
    {
        /// <summary>
        /// Id of publishing.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Message information.
        /// </summary>
        public SendMessage Message { get; set; }
    }
}