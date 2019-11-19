using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.IO;
using WIRC.Common;
using System.Collections.Concurrent;

namespace WIRC
{
    partial class Server
    {
        public Server(IPEndPoint bindAddress, TextWriter log)
        {
            this.bindAddress = bindAddress;
            Logger = new Logger(log);
        }

        private readonly IPEndPoint bindAddress;

        public Logger Logger { get; set; }

        CancellationTokenSource masterCancel = new CancellationTokenSource();

        public async Task Start()
        {
            Task acceptClientsTask = AcceptClientsAsync(bindAddress, masterCancel.Token);
            Task receiveMessagesTask = ReceiveMessagesAsync(masterCancel.Token);
            Task sendMessagesTask = SendMessagesAsync(masterCancel.Token);

            var tasks = new Task[] { acceptClientsTask, receiveMessagesTask, sendMessagesTask };

            await Task.WhenAny(tasks);
            masterCancel.Cancel();

            await Task.WhenAll(tasks);

            // TODO cleanup
        }

        public void Stop()
        {
            masterCancel.Cancel();
        }

        private readonly static TimeSpan LoopSleepTime = TimeSpan.FromMilliseconds(10);
        private Dictionary<string, ClientInfo> clients = new Dictionary<string, ClientInfo>();
        private Random random = new Random();
        private ConcurrentQueue<Message> messageQueue = new ConcurrentQueue<Message>();

        // TODO possibly spawn a send task for each message received
        // TODO possibly spawn a send task to send to each client
        private async Task SendMessagesAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var deadClients = new HashSet<ClientInfo>();

                if (messageQueue.Count == 0)
                    await Task.Delay(LoopSleepTime, token);

                while (messageQueue.TryDequeue(out Message message))
                {
                    foreach (ClientInfo client in clients.Values)
                    {
                        // send message
                        try
                        {
                            client.SendMessage(message);
                        }
                        catch (IOException e)
                        {
                            // Thrown when client disconnects
                        }
                        catch (Exception e)
                        {
                            Log(LogLevel.Error, $"{client.ID} {e.Message}");
                        }

                        // remember to remove if DC'd
                        if(!client.Connection.Connected)
                        {
                            deadClients.Add(client);
                        }
                    }

                    // remove clients that were found DC'd
                    foreach(ClientInfo client in deadClients)
                    {
                        clients.Remove(client.ID);
                        Log(LogLevel.Notice, $"{client.ID} disconnected");
                    }
                }
            }
        }

        // TODO use a dedicated task for each client
        // Currently using a spin loop to check for messages
        private async Task ReceiveMessagesAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach (ClientInfo client in clients.Values)
                {
                    if (!client.NetworkStream.DataAvailable)
                        continue;

                    Message message = client.ReadMessage();

                    messageQueue.Enqueue(message);
                }

                await Task.Delay(LoopSleepTime, token);
            }
        }

        private async Task AcceptClientsAsync(IPEndPoint bindAddress, CancellationToken token)
        {
            var listener = new TcpListener(bindAddress);
            listener.Start(20);

            Log(LogLevel.Notice, $"Listening for connections on {bindAddress}");

            while (!token.IsCancellationRequested)
            {
                TcpClient clientConnection = await listener.AcceptTcpClientAsync();

                ClientInfo client = new ClientInfo(clientConnection, GenerateClientName());
                clients.Add(client.ID, client);

                Log(LogLevel.Notice, $"{client.ID} connected");
            }

            listener.Stop();
        }

        private void Log(LogLevel level, string message) =>
            Logger.Log(level, message);

        private string GenerateClientName()
        {
            string id = $"User{random.Next()}";

            // TODO check name isn't already taken

            return id;
        }

        class ClientInfo
        {
            public string ID { get; private set; }
            public TcpClient Connection { get; private set; }
            public NetworkStream NetworkStream { get; private set; }
            public BinaryReader BinaryReader { get; private set; }
            public BinaryWriter BinaryWriter { get; private set; }
            public Message ReadMessage()
                => Message.ReadMessage(BinaryReader);
            public void SendMessage(Message m)
                => Message.WriteMessage(BinaryWriter, m);

            public ClientInfo(TcpClient tcpClient, string id)
            {
                Connection = tcpClient;
                NetworkStream = Connection.GetStream();
                ID = id;
                BinaryReader = new BinaryReader(NetworkStream);
                BinaryWriter = new BinaryWriter(NetworkStream);
            }
        }
    }
}
