using System;
using Atc.Data.Primitives;
using Geo;
using Geo.Geodesy;

namespace Atc.Math
{
    public static class GeoMath
    {
        private static readonly SpheroidCalculator _earthSpheroid = new SpheroidCalculator(Spheroid.Wgs84); 
        
        public static void CalculateGreatCircleLine(in GeoPoint end1, in GeoPoint end2, out GeoLine line)
        {
            var p1 = new Coordinate(end1.Lat, end1.Lon);
            var p2 = new Coordinate(end2.Lat, end2.Lon);
            var calcResult = GeodeticCalculations.CalculateGreatCircleLine(p1, p2);

            line = new GeoLine(
                end1,
                end2,
                length: Distance.FromMeters((float) calcResult.Distance.SiValue),
                bearing12: Bearing.FromTrueDegrees((float) calcResult.Bearing12),
                bearing21: Bearing.FromTrueDegrees((float) calcResult.Bearing21));
        }

        public static void CalculateGreatCircleDestination(in GeoPoint origin, Bearing bearing, Distance distance, out GeoPoint destination)
        {
            var p1 = new Coordinate(origin.Lat, origin.Lon);
            var calcResult = _earthSpheroid.CalculateOrthodromicLine(
                new Coordinate(origin.Lat, origin.Lon),
                bearing.Degrees,
                distance.Meters);

            destination = new GeoPoint(calcResult.Coordinate2.Latitude, calcResult.Coordinate2.Longitude);
        }
    }
}