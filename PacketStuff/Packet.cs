using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tcpServer
{
    public class Packet
    {
        private byte[] buffer;
        public Packet(byte[] _buffer)
        {
            buffer = _buffer;
        }
        public PacketId ReadHeader()
        {
            return (PacketId)buffer[0];
        }
        public byte[] ReadAfterHeader()
        {
            byte[] Data = new byte[buffer.Length -1];
            Array.Copy(buffer, 1, Data, 0, Data.Length);
            return Data;
        }
        public int Count => buffer.Length;
        public byte[] ReadData()
        {
            return buffer;
        }
        public void SetHeader(PacketId pId)
        {
            byte[] Data = buffer;
            Array.Resize(ref buffer, buffer.Length + 1);
            Array.Copy(Data, 0, buffer, 1, Data.Length);
            buffer[0] = (byte)pId;
        }
        /// <summary>
        /// Sets buffer data to <paramref name="_dataToSet"/>
        /// </summary>
        /// <param name="_dataToSet">include packetId in 1st byte</param>
        public void SetData(byte[] _dataToSet)
        {
            buffer = _dataToSet;
        }
    }
}
