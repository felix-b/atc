namespace Atc.Data.Primitives
{
    public readonly struct GeoLine
    {
        public GeoLine(GeoPoint end1, GeoPoint end2, Distance length, Bearing bearing12, Bearing bearing21)
        {
            End1 = end1;
            End2 = end2;
            Length = length;
            Bearing12 = bearing12;
            Bearing21 = bearing21;
        }

        public readonly GeoPoint End1;
        public readonly GeoPoint End2;
        public readonly Distance Length;
        public readonly Bearing Bearing12;
        public readonly Bearing Bearing21;
    }
}