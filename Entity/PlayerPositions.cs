using System;

namespace tcpServer
{
    public static class PlayerPositions
    {
        public static byte[] playerPositions = new byte[] { 1 };
        public static void AddToPlayerPositions(byte[] buffer, int ConnectedClients)
        {
            if (playerPositions.Length < ConnectedClients * 2)
            {
                    int lastplayerPosLength = playerPositions.Length;
                    Array.Resize(ref playerPositions, ConnectedClients * 2 + 1);
                    playerPositions[lastplayerPosLength-2] = buffer[0];
                    playerPositions[lastplayerPosLength - 1] = buffer[1];
            }
            else
            {
                Array.Resize(ref playerPositions, ConnectedClients * 2+1);
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
