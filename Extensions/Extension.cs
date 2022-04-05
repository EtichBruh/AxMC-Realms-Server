using tcpServer.Structs;

namespace Extensions
{
    public static class Extension
    {
        public static Vec2 ToVec2(this byte[] b)
        {
            var result = Vec2.Zero;
            result.X = (short)((b[1] << 8) + b[0]);
            result.Y = (short)((b[3] << 8) + b[2]);
            return result;
        }
    }
}
