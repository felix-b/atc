// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include "libworld.h"

using namespace std;

namespace world
{
    UniPoint::UniPoint(const GeoPoint &_geo) :
        m_geo(_geo), m_local({-1,-1,-1}), m_assignedType(Type::geo)
    {
    }

    UniPoint::UniPoint(shared_ptr<HostServices> _services, const LocalPoint& _local) :
        m_services(_services), m_local(_local), m_assignedType(Type::local)
    {
        m_geo = m_services->localToGeo(m_local);
    }

    UniPoint::UniPoint(shared_ptr<HostServices> _services, const GeoPoint& _geo) :
        m_services(_services), m_geo(_geo), m_assignedType(Type::geo)
    {
        m_local = m_services->geoToLocal(m_geo);
    }

    UniPoint UniPoint::fromLocal(shared_ptr<HostServices> _services, const LocalPoint& _local)
    {
        return UniPoint(_services, _local);
    }

    UniPoint UniPoint::fromLocal(shared_ptr<HostServices> _services, float _x, float _y, float _z)
    {
        return UniPoint(_services, LocalPoint({_x, _y, _z}));
    }

    UniPoint UniPoint::fromGeo(shared_ptr<HostServices> _services, const GeoPoint& _geo)
    {
        return UniPoint(_services, _geo);
    }

    UniPoint UniPoint::fromGeo(
        shared_ptr<HostServices> _services, 
        double _latitude, 
        double _longitude, 
        double _altitude)
    {
        return UniPoint(_services, GeoPoint({_latitude, _longitude, _altitude}));
    }
}