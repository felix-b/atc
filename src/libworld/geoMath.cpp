// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#define _USE_MATH_DEFINES
#include <cmath>
#include <iostream>
#include <sstream>
#include <iomanip>
#include "libworld.h"

using namespace std;

static const double PI = 3.141592653589793;
static const double TWO_PI = 2 * 3.141592653589793;
static const double EARTH_RADIUS_METERS = 6371000.0; //TODO: more precision?

double normalizeRadians(double a)
{
    while (a <= -PI) {
        a += TWO_PI;
    }
    while (a > PI) {
        a -= TWO_PI;
    }
    return a;
}

bool isClockwiseArc(double a0, double a1) 
{
    if (a0 >= 0 && a1 >= 0) {
        return (a0 >= a1);
    }
    if (a0 < 0 && a1 < 0) {
        return (a0 >= a1);
    }
    if (a0 >= 0 && a1 < 0) {
        double d = a0 + std::abs(a1);
        return (d < PI);
    }
    if (a0 < 0 && a1 >= 0) {
        double d = std::abs(a0) + a1;
        return (d >= PI);
    }
    // should never get here
    throw runtime_error("isClockwiseArc internal error");
}

double getArcAngleDelta(double a0, double a1) 
{
    if (a0 * a1 >= 0) {
        return a1 - a0;
    }
    if (a0 >= 0 && a1 < 0) {
        double d = a0 + abs(a1);
        return (d < M_PI ? -d : 2 * M_PI - d);
    }
    if (a0 < 0 && a1 >= 0) {
        double d = abs(a0) + a1;
        return (d < M_PI ? d : -(2 * M_PI - d));
    }
    // should never get here
    throw runtime_error("getArcAngleDelta internal error");
}

double calcTurnAngle(const world::GeoMath::TurnData& input) {
    double d1x = input.e1p0.longitude - input.e1p1.longitude;
    double d1y = input.e1p0.latitude - input.e1p1.latitude;
    double d2x = input.e2p1.longitude - input.e2p0.longitude;
    double d2y = input.e2p1.latitude - input.e2p0.latitude;
    double angle = atan2(d1x * d2y - d1y * d2x, d1x * d2x + d1y * d2y);
    return angle;
}

namespace world
{
    double GeoMath::pi()
    {
        return PI;
    }
    
    double GeoMath::twoPi()
    {
        return TWO_PI;
    }

    double GeoMath::degreesToRadians(double degrees)
    {
        double radians = degrees * PI / 180.0; //TODO: optimize? pre-computed table?
        return radians;
    }

    double GeoMath::radiansToDegrees(double degrees)
    {
        double radians = degrees * 180.0 / PI; //TODO: optimize? pre-computed table?
        return radians;
    }

    double GeoMath::headingToAngleDegrees(double headingDegrees)
    {
        return (90.0f - headingDegrees + (headingDegrees >= 270 ? 360 : 0));
    }

    double GeoMath::headingToAngleRadians(double headingDegrees)
    {
        return degreesToRadians(headingToAngleDegrees(headingDegrees));
    }

    double GeoMath::radiansToHeading(double radians)
    {
        double degrees = radiansToDegrees(radians);
        double heading = 90 - degrees;
        return (heading >= 0 ? heading : 360 + heading);
    }

    GeoPoint GeoMath::getPointAtDistance(const GeoPoint& origin, float headingDegrees, float distanceMeters)
    {   
        // great circle distance by haversine formula

        double northBearingRadians = degreesToRadians(headingDegrees);
        double lat0 = degreesToRadians(origin.latitude);
        double lon0 = degreesToRadians(origin.longitude);

        double sinLat0 = sin(lat0);
        double cosLat0 = cos(lat0);
        double dR = (double)distanceMeters/EARTH_RADIUS_METERS;
        double sinDR = sin(dR);
        double cosDR = cos(dR);

        double lat1 = asin(
            sinLat0 * cosDR +
            cosLat0 * sinDR * cos(northBearingRadians)
        );

        double lon1 = lon0 + atan2(
            sin(northBearingRadians) * sinDR * cosLat0,
            cosDR - sinLat0 * sin(lat1)
        );  

        return GeoPoint(
            radiansToDegrees(lat1), 
            radiansToDegrees(lon1), 
            origin.altitude);
    }

    float GeoMath::getDistanceMeters(const GeoPoint& p1, const GeoPoint& p2)
    {
        double lat1 = p1.latitude * PI / 180.0;
        double lat2 = p2.latitude * PI / 180.0;
        double deltaLat = (p2.latitude - p1.latitude) * PI / 180.0;
        double deltaLon = (p2.longitude - p1.longitude) * PI / 180.0;

        double a = 
            sin(deltaLat/2) * sin(deltaLat/2) +
            cos(lat1) * cos(lat2) *
            sin(deltaLon/2) * sin(deltaLon/2);
        
        double c = 2 * atan2(sqrt(a), sqrt(1-a));
        double distance = EARTH_RADIUS_METERS * c;
        return distance;
    }

    double GeoMath::distanceToRadians(float distanceMeters)
    {
        return distanceMeters / EARTH_RADIUS_METERS;
    }
    
    float GeoMath::flipHeading(float headingDegrees)
    {
        return abs(headingDegrees) < 180
            ? headingDegrees + 180
            : headingDegrees - 180;
    }

    float GeoMath::getHeadingFromPoints(const GeoPoint& origin, const GeoPoint& destination)
    {
        // double bearingRadians = atan2(destination.latitude - origin.latitude, destination.longitude - origin.longitude);
        // float bearingDegrees = radiansToHeading(bearingRadians);// fmod(bearingRadians * 180.0 / PI + 360, 360);
        // return bearingDegrees;

        double lat1 = degreesToRadians(origin.latitude);
        double lon1 = degreesToRadians(origin.longitude);

        double lat2 = degreesToRadians(destination.latitude);
        double lon2 = degreesToRadians(destination.longitude);

        double y = sin(lon2-lon1) * cos(lat2);
        double x = 
            cos(lat1) * sin(lat2) -
            sin(lat1) * cos(lat2) * cos(lon2-lon1);
        
        double bearingRadians = atan2(y, x);
        float bearingDegrees = fmod(bearingRadians * 180.0 / PI + 360, 360);
        
        return bearingDegrees;
    }

    double GeoMath::getRadiansFromPoints(const GeoPoint& origin, const GeoPoint& destination)
    {
        return atan2(destination.latitude - origin.latitude, destination.longitude - origin.longitude);
    }

    void GeoMath::calculateTurn(const GeoMath::TurnData& input, GeoMath::TurnArc& output, shared_ptr<HostServices> host)
    {
        stringstream logstr;
        logstr << setprecision(11) << endl;
        logstr << "---begin calculateTurn---" << endl;
        logstr << "input.e1p0=" << input.e1p0.latitude << "," << input.e1p0.longitude << endl;
        logstr << "input.e1p1=" << input.e1p1.latitude << "," << input.e1p1.longitude << endl;
        logstr << "input.e1HeadingRad=" << input.e1HeadingRad << endl;
        logstr << "input.e2p0=" << input.e2p0.latitude << "," << input.e2p0.longitude << endl;
        logstr << "input.e2p1=" << input.e2p1.latitude << "," << input.e2p1.longitude << endl;
        logstr << "input.e2HeadingRad=" << input.e2HeadingRad << endl;
        logstr << "input.radius=" << input.radius << endl;

        host->writeLog(logstr.str().c_str());
        logstr.str("");
        logstr << endl;

        double angleBetweenEdges = calcTurnAngle(input); 
        logstr << "angleBetweenEdges=" << angleBetweenEdges << endl;

        double edgeShortenDistance = std::abs(input.radius / tan(angleBetweenEdges / 2));
        logstr << "edgeShortenDistance=" << edgeShortenDistance << endl;

        GeoPoint arcStartPoint = {
            input.e1p1.latitude + (edgeShortenDistance * sin(input.e1HeadingRad + PI)),
            input.e1p1.longitude + (edgeShortenDistance * cos(input.e1HeadingRad + PI)),
            0   
        };
        logstr << "arcStartPoint=" << arcStartPoint.latitude << "," << arcStartPoint.longitude << endl;

        GeoPoint arcEndPoint = {
            input.e2p0.latitude + (edgeShortenDistance * sin(input.e2HeadingRad)),
            input.e2p0.longitude + (edgeShortenDistance * cos(input.e2HeadingRad)),
            0
        };
        logstr << "arcEndPoint=" << arcEndPoint.latitude << "," << arcEndPoint.longitude << endl;

        double e1NormalToCenterAngle = (angleBetweenEdges > 0
            ? input.e1HeadingRad - PI / 2
            : input.e1HeadingRad + PI / 2);
        logstr << "e1NormalToCenterAngle=" << e1NormalToCenterAngle << endl;

        double e2NormalToCenterAngle = (angleBetweenEdges > 0
            ? input.e2HeadingRad - PI / 2
            : input.e2HeadingRad + PI / 2);
        logstr << "e2NormalToCenterAngle=" << e2NormalToCenterAngle << endl;

        GeoPoint arcCenterPoint = {
            arcStartPoint.latitude + (input.radius * sin(e1NormalToCenterAngle)),
            arcStartPoint.longitude + (input.radius * cos(e1NormalToCenterAngle)),
            0
        };
        double arcStartAngle = normalizeRadians(e1NormalToCenterAngle + PI);
        logstr << "arcStartAngle=" << arcStartAngle << endl;
        double arcEndAngle = normalizeRadians(e2NormalToCenterAngle + PI);
        logstr << "arcEndAngle=" << arcEndAngle << endl;
        
        double arcDeltaAngle = getArcAngleDelta(arcStartAngle, arcEndAngle);
        // arcStartAngle > M_PI_2 && arcStartAngle <= M_PI && arcEndAngle < -M_PI_2 && arcEndAngle >= -M_PI
        // ? M_PI - arcStartAngle + arcEndAngle + M_PI
        // : arcEndAngle - arcStartAngle);

        output.p0 = arcStartPoint;
        output.p1 = arcEndPoint;
        output.arcCenter = arcCenterPoint;
        output.arcStartAngle = arcStartAngle;
        output.arcEndAngle = arcEndAngle;
        output.arcDeltaAngle = arcDeltaAngle;
        output.arcRadius = input.radius;
        output.arcClockwise = isClockwiseArc(arcStartAngle, arcEndAngle);
        output.arcLengthMeters = abs(arcDeltaAngle) * getDistanceMeters(arcStartPoint, arcCenterPoint);

        host->writeLog(logstr.str().c_str());
        logstr.str("");
        logstr << endl;

        logstr << "abs(arcDeltaAngle)=" << abs(arcDeltaAngle) << " ; "
               << "getDistanceMeters(arcStartPoint, arcCenterPoint)=" << getDistanceMeters(arcStartPoint, arcCenterPoint) << " ; "
               << "arcLengthMeters=" << output.arcLengthMeters << endl;

        output.heading0 = GeoMath::radiansToHeading(input.e1HeadingRad);
        output.heading1 = GeoMath::radiansToHeading(input.e2HeadingRad);
        logstr << "arcClockwise=" << (output.arcClockwise ? "true" : "false") << endl;

        logstr << "---end calculateTurn---" << endl;
        host->writeLog(logstr.str().c_str());
    }   

    float GeoMath::getTurnDegrees(float fromHeading, float toHeading)
    {
        float deltaHeading = (360 + toHeading) - (360 + fromHeading);
        if (deltaHeading > 180)
        {
            return deltaHeading - 360;
        }
        if (deltaHeading < -180)
        {
            return deltaHeading + 360;
        }
        return deltaHeading;
    }

    float GeoMath::addTurnToHeading(float heading, float turnDegrees)
    {
        auto newHeading = heading + turnDegrees;
        if (newHeading > 360)
        {
            return newHeading - 360;
        }
        if (newHeading < 0)
        {
            return newHeading + 360;
        }
        return newHeading;
    }
}
