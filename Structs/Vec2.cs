namespace tcpServer.Structs
{
    public struct Vec2
    {
        private static byte[] empty = { 0, 0,0,0 };
        public short X;
        public short Y;
        public Vec2(short x, short y)
        {
            X = x;
            Y = y;
        }
        public static Vec2 Zero { get; }

        public byte[] ToByte()
        {
            empty[1] = (byte)(X >> 8);
            empty[0] = (byte)(X & 255);
            empty[3] = (byte)(Y >> 8);
            empty[2] = (byte)(Y & 255);
            return empty;
        }
    }
}
