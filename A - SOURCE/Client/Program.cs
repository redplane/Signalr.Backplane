using System;
using Microsoft.AspNet.SignalR.Client;
using Signalr.Backplane.Shared.Models;

namespace Signalr.Backplane.Client
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var signalrUrl = "http://localhost:19894";
            var signalrConnection = new HubConnection(signalrUrl);
            var signalrContext = signalrConnection.CreateHubProxy("message-hub");
            signalrContext.On("obtainSystemMessage", (PublishMessage publishMessage) =>
            {
                Console.WriteLine("".PadRight(20, '-'));
                Console.WriteLine("A message has been detected.");
                Console.WriteLine("Id: {0}", publishMessage.Id);
                var message = publishMessage.Message;
                if (message == null)
                {
                    Console.WriteLine("-- No content --");
                    return;
                }

                Console.WriteLine("Title: {0}", message.Title);
                Console.WriteLine("Content: {0}", message.Content);
                Console.WriteLine("".PadRight(20, '-'));
            });

            // Start hub connection.
            signalrConnection.Start().ContinueWith(task =>
            {

                if (task.IsFaulted)
                {
                    var exception = task.Exception;
                    if (exception != null)
                    {
                        Console.WriteLine(exception.Message);
                        return;
                    }

                    Console.WriteLine("Exception couldn't be analyzed.");
                    return;
                }

                Console.WriteLine("Listener has started. Press any key to exit.");
            }).Wait();
            
            Console.ReadLine();
        }
    }
}