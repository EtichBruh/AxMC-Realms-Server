namespace tcpServer.Structs
{
    public struct Vec2
    {
        private static byte[] empty = { 0, 0,0,0 };
        public ushort X;
        public ushort Y;
        public Vec2(ushort x, ushort y)
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
