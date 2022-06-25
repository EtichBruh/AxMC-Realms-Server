using System;

namespace tcpServer
{
    public static class PlayerPositions
    {
        public static byte[] Positions = new byte[] { 1 };
        public static void AddToPositions(byte[] buffer, int Connections)
        {
            if (Positions.Length < Connections * 2)
            {
                    int PrevLen = Positions.Length;
                    Array.Resize(ref Positions, Connections * 2 + 1);
                    Positions[PrevLen-2] = buffer[0];
                    Positions[PrevLen - 1] = buffer[1];
            }
            else
            {
                Array.Resize(ref Positions, Connections * 2+1);
            }
        }
    }
}
/*                if (m_numConnectedSockets > 1)
                {
                    Packet packet = new(e.Buffer);
                    if(packet.ReadHeader() == PacketId.Position && packet.Count > 1)
                    {
                        PlayerPositions.AddToPlayerPositions(packet.ReadDataAfterHeader(), m_numConnectedSockets);
                            packet.SetData(PlayerPositions.playerPositions);
                            e.SetBuffer(packet.ReadData(),0,packet.Count);
                    }
                    if(packet.ReadHeader() == PacketId.Hello)
                    {
                        e.SetBuffer(new byte[] { (byte)m_numConnectedSockets },0,1);
                    }*/
