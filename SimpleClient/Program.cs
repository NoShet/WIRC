using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WIRC.Common;

namespace WIRC
{
    class SimpleClient
    {
        static Client client;
        static IPEndPoint serverEP = IPEndPoint.Parse("127.0.0.1:2424");

        static Logger Logger { get; set; }

        static int Main(string[] args)
            => Task.Run(async () =>
        {
            Logger = new Logger(Console.Out);

            if(args.Length >= 1)
                serverEP = IPEndPoint.Parse(args[0]);

            Log(LogLevel.Notice, $"Connecting to {serverEP}");

            client = new Client(Logger);

            try
            {
                await client.ConnectAsync(serverEP);
            }
            catch (SocketException e)
            {
                Log(LogLevel.Error, e.Message);
                return 1;
            }

            Log(LogLevel.Notice, $"Connected to {serverEP}");

            await client.RunAsync(MessageSendLoopAsync, MessageReceivedHandlerAsync);

            // TODO cleanup

            return 0;
        }).Result;

        private static void Log(LogLevel level, string message) =>
            Logger.Log(level, message);

        private static async Task MessageSendLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Message message = await Task.Run(() =>
                {
                    var messageText = Console.ReadLine();
                    var data = Encoding.UTF8.GetBytes(messageText);

                    return new Message("text", data);
                }, cancellationToken);

                await client.SendMessageAsync(message);
            }
        }

        private static async Task MessageReceivedHandlerAsync(Message message, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                var messageText = Encoding.UTF8.GetString(message.Data);

                Console.WriteLine($"Type = {message.Type}: {messageText}");
            });
        }
    }
}
