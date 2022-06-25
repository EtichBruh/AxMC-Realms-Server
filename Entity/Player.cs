using System.Net.Sockets;
using tcpServer.Structs;

namespace tcpServer.Entity
{
    public class Player
    {
        public Socket Sock;
        public static Player[] Players;
        public Vec2 Position;
        public int ConnectedId;

        public Player(Socket s)
        {
            Sock = s;
        }
        public byte[] PositionByte()
        {
            return Position.ToByte();
        }
    }
}
