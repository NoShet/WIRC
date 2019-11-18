using System;
using System.IO;
using System.Threading.Tasks;

namespace WIRC.Common
{
    public class Message
    {
        public string Type { get; private set; }
        public byte[] Data { get; private set; }

        public Message(string type, byte[] data)
        {
            this.Type = type;
            this.Data = data;
        }

        public static void WriteMessage(BinaryWriter writer, Message message)
        {
            writer.Write(message.Type);
            writer.Write(message.Data.Length);
            writer.Write(message.Data);
        }

        public static async Task WriteMessageAsync(BinaryWriter writer, Message message)
            => await Task.Run(() => WriteMessage(writer, message));

        public static Message ReadMessage(BinaryReader reader)
        {
            string type;
            byte[] data;
            int dataLength;

            type = reader.ReadString();
            dataLength = reader.ReadInt32();
            data = reader.ReadBytes(dataLength);

            return new Message(type, data);
        }

        public static async Task<Message> ReadMessageAsync(BinaryReader reader)
            => await Task.Run(() => ReadMessage(reader));
    }
}
