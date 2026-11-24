namespace DaoSuong.Building
{
    public readonly struct BuildCell
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public BuildCell(int x, int y, int z)
        {
            X = x; Y = y; Z = z;
        }
    }
}
