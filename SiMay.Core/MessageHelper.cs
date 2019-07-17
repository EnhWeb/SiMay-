using System;
using static SiMay.Serialize.PacketSerializeHelper;

namespace SiMay.Core
{

    public static class MessageHelper
    {
        public static byte[] CopyMessageHeadTo(MessageHead cmd, object entity)
        {
            return CopyMessageHeadTo(cmd, SerializePacket(entity));
        }
        public static byte[] CopyMessageHeadTo(MessageHead cmd, byte[] data, int size)
        {
            byte[] buff = new byte[size + 2];
            BitConverter.GetBytes((short)cmd).CopyTo(buff, 0);
            Array.Copy(data, 0, buff, 2, size);

            return buff;
        }

        public static byte[] CopyMessageHeadTo(MessageHead cmd, byte[] data = null)
        {
            if (data == null)
                data = new byte[] { };

            return CopyMessageHeadTo(cmd, data, data.Length);
        }

        public static byte[] CopyMessageHeadTo(MessageHead cmd, string str)
        {
            byte[] data = str.UnicodeStringToBytes();

            return CopyMessageHeadTo(cmd, data, data.Length);
        }

        public static MessageHead GetMessageHead(this byte[] data)
            => (MessageHead)BitConverter.ToInt16(data, 0);

        public static byte[] GetMessageBody(this byte[] data)
        {
            byte[] bytes = new byte[data.Length - 2];
            Array.Copy(data, 2, bytes, 0, bytes.Length);
            return bytes;
        }
        public static T GetMessageEntity<T>(this byte[] data) 
            where T : new()
        {
            var entity = DeserializePacket<T>(GetMessageBody(data));
            return entity;
        }
    }
}