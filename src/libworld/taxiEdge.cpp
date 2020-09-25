// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <cmath>
#include <iostream>
#include "libworld.h"

using namespace std;

namespace world
{
    TaxiEdge::TaxiEdge(
        int _id,
        const string& _name,
        const int _nodeId1,
        const int _nodeId2,
        Type _type,
        bool _isOneWay,
        bool _isHighSpeedExit,
        float _lengthMeters
    ) : 
        m_id(_id),
        m_name(_name),
        m_nodeId1(_nodeId1),
        m_nodeId2(_nodeId2),
        m_type(_type),
        m_isOneWay(_isOneWay),
        m_isHighSpeedExit(_isHighSpeedExit),
        m_lengthMeters(_lengthMeters),
        m_heading(0)
    {
    }

    TaxiEdge::TaxiEdge(shared_ptr<TaxiEdge> _source, bool _flippingOver) :
        m_id(_source->m_id),
        m_type(_source->m_type),
        m_isOneWay(_source->m_isOneWay),
        m_isHighSpeedExit(_source->m_isHighSpeedExit),
        m_name(_source->m_name),
        m_lengthMeters(_source->m_lengthMeters),
        m_heading(_flippingOver ? GeoMath::flipHeading(_source->m_heading) : _source->m_heading),
        m_nodeId1(_flippingOver ? _source->m_nodeId2 : _source->m_nodeId1),
        m_nodeId2(_flippingOver ? _source->m_nodeId1 : _source->m_nodeId2),
        m_node1(_flippingOver ? _source->m_node2 : _source->m_node1),
        m_node2(_flippingOver ? _source->m_node1 : _source->m_node2),
        m_activeZones(_source->m_activeZones),
        m_flipOver(_source)
    {
    }
    
    TaxiEdge::TaxiEdge(const UniPoint& _fromPoint, const UniPoint& _toPoint) :
        m_id(-1),
        m_type(TaxiEdge::Type::Taxiway),
        m_isOneWay(true),
        m_isHighSpeedExit(false),
        m_name(""),
        m_lengthMeters(GeoMath::getDistanceMeters(_fromPoint.geo(), _toPoint.geo())),
        m_heading(GeoMath::getHeadingFromPoints(_fromPoint.geo(), _toPoint.geo())),
        m_nodeId1(-1),
        m_nodeId2(-1),
        m_node1(make_shared<TaxiNode>(-1, _fromPoint)),
        m_node2(make_shared<TaxiNode>(-1, _toPoint))
    {
    }

    shared_ptr<TaxiEdge> TaxiEdge::flipOver(shared_ptr<TaxiEdge> source)
    {
        if (!source->canFlipOver())
        {
            throw runtime_error("This edge cannot be flipped over");
        }
        if (!source->m_flipOver)
        {
            source->m_flipOver = shared_ptr<TaxiEdge>(new TaxiEdge(source, true));
        }
        return source->m_flipOver;
    }
    
    float TaxiEdge::calculateTaxiDistance(const shared_ptr<TaxiNode>& from, const shared_ptr<TaxiNode>& to)
    {   
        return sqrt(
            pow(from->m_location.x() - to->m_location.x(), 2) + 
            pow(from->m_location.z() - to->m_location.z(), 2)
        );
    }

    float TaxiEdge::calculateTaxiDistance(const UniPoint& from, const UniPoint& to)
    {   
        const auto& localFrom = from.local();
        const auto& localTo = to.local();

        return sqrt(
            pow(localFrom.x - localTo.x, 2) + 
            pow(localFrom.z - localTo.z, 2)
        );
    }
}
