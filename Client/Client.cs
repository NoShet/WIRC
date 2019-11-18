using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WIRC.Common;

namespace WIRC
{
    public class Client
    {
        readonly CancellationTokenSource stopCTS = new CancellationTokenSource();
        CancellationToken stopToken => stopCTS.Token;
        public TcpClient ServerConnection { get; private set; }
        public NetworkStream Stream { get; private set; }
        BinaryReader BinaryReader { get; set; }
        BinaryWriter BinaryWriter { get; set; }
        public Logger Logger { get; set; }

        public async Task RunAsync(Func<CancellationToken, Task> sendMessageLoopAsync, Func<Message, CancellationToken, Task> messageReceivedHandlerAsync)
        {
            if (sendMessageLoopAsync is null)
            {
                throw new ArgumentNullException(nameof(sendMessageLoopAsync));
            }
            if (messageReceivedHandlerAsync is null)
            {
                throw new ArgumentNullException(nameof(messageReceivedHandlerAsync));
            }

            Task sendMessagesTask = sendMessageLoopAsync(stopToken);
            Task receiveMessagesTask = MessageReceiveLoop(messageReceivedHandlerAsync, stopToken);

            await Task.WhenAny(sendMessagesTask, receiveMessagesTask);
            stopCTS.Cancel();
        }

        private async Task MessageReceiveLoop(Func<Message, CancellationToken, Task> messageReceivedHandlerAsync, CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                Message message = await ReadMessageAsync();

                await messageReceivedHandlerAsync(message, cancellationToken);
            }
        }

        // TODO allow changing handlers at runtime
        //public Func<Task> OnMessageReveivedAsync { get; internal set; }
        //public Func<Task> MessageSendLoopAsync { get; internal set; }

        public Client(Logger logger)
        {
            ServerConnection = new TcpClient();
            Logger = logger;
        }

        /// <summary>
        /// Connects the client to the specified endpoint as an asynchronous operation.
        /// </summary>
        /// <param name="serverEP">The endpoint of the remote host to which you intend to connect.</param>
        /// <returns></returns>
        /// <exception cref="SocketException"
        public async Task ConnectAsync(IPEndPoint serverEP)
        {
            await ServerConnection.ConnectAsync(serverEP.Address.ToString(), serverEP.Port);

            Stream = ServerConnection.GetStream();
            BinaryReader = new BinaryReader(Stream);
            BinaryWriter = new BinaryWriter(Stream);
        }

        private void Log(LogLevel level, string message) =>
            Logger?.Log(level, message);

        public async Task SendMessageAsync(Message message) =>
            await Message.WriteMessageAsync(BinaryWriter, message);

        public async Task<Message> ReadMessageAsync() =>
            await Message.ReadMessageAsync(BinaryReader);
    }
}
