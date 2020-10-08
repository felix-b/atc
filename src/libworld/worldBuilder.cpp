// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <algorithm>
#include <chrono>
#include "libworld.h"
#include "stlhelpers.h"
#include "simplePhraseologyService.hpp"

using namespace std;
using namespace world;

namespace world
{
    shared_ptr<World> WorldBuilder::assembleSampleWorld(
        shared_ptr<HostServices> host, 
        const vector<shared_ptr<Airport>>& airports)
    {
        auto world = shared_ptr<World>(new World(
            host, 
            chrono::system_clock::to_time_t(chrono::system_clock::now())
        ));

        for (const auto& airport : airports)
        {
            world->m_airports.push_back(airport);
            world->m_airportByIcao.insert({airport->header().icao(), airport});
            if (airport->tower())
            {
                auto airspace = airport->tower()->airspace();
                world->m_airspaces.push_back(airspace);
                world->m_airspaceById.insert({airspace->id(), airspace});
                world->m_controlFacilities.push_back(airport->tower());
            }
        }

        return world;
    }

    shared_ptr<Airport> WorldBuilder::assembleAirport(
        shared_ptr<HostServices> host,
        const Airport::Header& header,
        const vector<shared_ptr<Runway>>& runways,
        const vector<shared_ptr<ParkingStand>>& parkingStands,
        const vector<shared_ptr<TaxiNode>>& taxiNodes,
        const vector<shared_ptr<TaxiEdge>>& taxiEdges,
        shared_ptr<ControlFacility> tower,
        shared_ptr<ControlledAirspace> airspace)
    {
        auto airport = make_shared<Airport>(header);
        for (auto rwy : runways)
        {
            airport->m_runways.push_back(rwy);
        }
        airport->m_taxiNet = assembleTaxiNet(host, runways, taxiNodes, taxiEdges);

        for (auto parking : parkingStands)
        {
            airport->m_parkingStands.push_back(parking);
            airport->m_parkingStandByName.insert({ parking->m_name, parking });
        }

        fixUpEdgesAndRunways(host, airport);
        linkAirportTowerAirspace(host, airport, tower, airspace);

        return airport;
    }

    shared_ptr<ControlFacility> WorldBuilder::assembleAirportTower(
        shared_ptr<HostServices> host, 
        const Airport::Header& header,
        shared_ptr<ControlledAirspace> airspace,
        const vector<ControllerPosition::Structure>& positions)
    {
        auto tower = make_shared<ControlFacility>();

        const auto getPositionCallSign = [&](const ControllerPosition::Structure& init) {
            switch (init.type)
            {
                case ControllerPosition::Type::ClearanceDelivery:
                    return tower->callSign() + " Clearance";
                case ControllerPosition::Type::Ground:
                    return tower->callSign() + " Ground";
                case ControllerPosition::Type::Local:
                    return tower->callSign() + " Tower";
                case ControllerPosition::Type::Approach:
                    return tower->callSign() + " Approach";
                case ControllerPosition::Type::Departure:
                    return tower->callSign() + " Departure";
            }
            return tower->callSign();
        };

        const auto assemblePosition = [&](const ControllerPosition::Structure& init) {
            auto frequency = shared_ptr<Frequency>(new Frequency(
                host,
                init.frequencyKhz,
                header.datum(), //TODO: use tower location
                10.0  //TODO: real-life antenna radius?
            ));
            auto radarScope = shared_ptr<RadarScope>(new RadarScope(
                airspace,
                init.scopeLimit
            ));
            auto position = shared_ptr<ControllerPosition>(new ControllerPosition(
                host,
                init.type,
                tower,
                getPositionCallSign(init),
                frequency,
                radarScope
            ));
            frequency->m_controllerPosition = position;

            return position;
        };

        //TODO: tower callsign will sound unrealistically. Manual configuration?
        tower->m_callSign = SimplePhraseologyService::spellIcaoCode(header.icao()[0] == 'K'
            ? header.icao().substr(1)  // ICAO->FAA
            : header.icao());

        tower->m_name = header.icao() + " Tower";
        tower->m_type = ControlFacility::Type::Tower;
        tower->m_airspace = airspace;
        
        for (const auto& posInit : positions)
        {
            const auto position = assemblePosition(posInit);
            position->m_controller = host->createAIController(position);
            tower->m_positions.push_back(position);
            
//            host->writeLog(
//                "Initialized controller position [%s] on frequency [%d]",
//                position->callSign().c_str(),
//                position->frequency()->khz());
        }

        return tower;
    }

    shared_ptr<ControlledAirspace> WorldBuilder::assembleSampleAirportControlZone(const Airport::Header& header)
    {
        //TODO: use airspace actual data
        auto airspace = WorldBuilder::assembleSimpleAirspace(
            AirspaceClass::ClassB,
            ControlledAirspace::Type::ControlZone,
            header.datum(),
            10,
            ALTITUDE_GROUND,
            18000,
            header.icao(),
            header.icao(),
            header.name(),
            header.icao()
        );
        return airspace;
    }

    void WorldBuilder::addActiveZone(
        shared_ptr<TaxiEdge> edge, 
        const string& runwayName,
        bool departure,
        bool arrival,
        bool ils)
    {
        if (departure) 
        {
            edge->m_activeZones.departue.add(runwayName);
        }

        if (arrival) 
        {
            edge->m_activeZones.arrival.add(runwayName);
        }

        if (ils) 
        {
            edge->m_activeZones.ils.add(runwayName);
        }
    }

    void WorldBuilder::tidyAirportElevations(
        shared_ptr<HostServices> host,
        shared_ptr<Airport> airport)
    {
        for (const auto& runway : airport->runways())
        {
            runway->m_end1.m_elevationFeet = host->queryTerrainElevationAt(runway->m_end1.m_centerlinePoint.geo());
            runway->m_end2.m_elevationFeet = host->queryTerrainElevationAt(runway->m_end2.m_centerlinePoint.geo());
        }
    }

    shared_ptr<TaxiNet> WorldBuilder::assembleTaxiNet(
        shared_ptr<HostServices> host,
        const vector<shared_ptr<Runway>>& runways,
        const vector<shared_ptr<TaxiNode>>& nodes,
        const vector<shared_ptr<TaxiEdge>>& edges)
    {
        auto net = make_shared<TaxiNet>(nodes, edges);

        for (const auto& node : nodes)
        {
            net->m_nodeById.insert({node->m_id, node});
        }

        for (const auto& edge : edges)
        {
            auto& node1 = getValueOrThrow(net->m_nodeById, edge->m_nodeId1);
            auto& node2 = getValueOrThrow(net->m_nodeById, edge->m_nodeId2);

            edge->m_node1 = node1;
            edge->m_node2 = node2;
            edge->m_lengthMeters = TaxiEdge::calculateTaxiDistance(node1, node2);
            edge->m_heading = GeoMath::getHeadingFromPoints(node1->location().geo(), node2->location().geo());

            node1->m_edges.push_back(edge);
            if (edge->canFlipOver())
            {
                node2->m_edges.push_back(TaxiEdge::flipOver(edge));
            }
        }

        return net;
    }

    shared_ptr<ControlledAirspace> WorldBuilder::assembleSimpleAirspace(
        const AirspaceClass& classification,
        ControlledAirspace::Type type,
        const GeoPoint& centerPoint, 
        float radiusNm, 
        float lowerLimitFeet,
        float upperLimitFeet,
        const string& areaCode,
        const string& icaoCode,
        const string& centerName,
        const string& name)
    {
        GeoPolygon lateralBounds({ 
            GeoPolygon::circleEdge(centerPoint, radiusNm)
        });
        auto geometry = shared_ptr<AirspaceGeometry>(new AirspaceGeometry(
            lateralBounds, 
            lowerLimitFeet != ALTITUDE_GROUND, 
            lowerLimitFeet, 
            true, 
            upperLimitFeet
        ));
        auto airspace = shared_ptr<ControlledAirspace>(new ControlledAirspace(
            1,
            areaCode,
            icaoCode,
            centerName,
            name, 
            type, 
            classification,
            geometry));
            
        return airspace;
    }

    void WorldBuilder::fixUpEdgesAndRunways(
        shared_ptr<HostServices> host,
        shared_ptr<Airport> airport)
    {
        const auto calcRunwayHeadings = [airport]() {
            for (const auto& runway : airport->m_runways)
            {
                auto& end1 = runway->m_end1;
                auto& end2 = runway->m_end2;
                end1.m_heading = GeoMath::getHeadingFromPoints(
                    end1.m_centerlinePoint.geo(), 
                    end2.m_centerlinePoint.geo());
                end2.m_heading = GeoMath::getHeadingFromPoints(
                    end2.m_centerlinePoint.geo(), 
                    end1.m_centerlinePoint.geo());
                runway->m_lengthMeters = GeoMath::getDistanceMeters(
                    end1.m_centerlinePoint.geo(),
                    end2.m_centerlinePoint.geo());

                // runway elevations can be tailored to scenery with a call to tidyAirportElevations()
                // this must be deferred in the sim until terrain probes are available at the airport
                end1.m_elevationFeet = airport->header().elevation();
                end2.m_elevationFeet = airport->header().elevation();
            }
        };

        const auto buildRunwayByNameMap = [&airport]() {
            auto& map = airport->m_runwayByName;
            Runway::Bitmask nextRunwayBit = 1;
            
            for (const auto runway : airport->m_runways)
            {
                runway->m_maskBit = nextRunwayBit;
                nextRunwayBit <<= 1;
                map.insert({ runway->m_end1.m_name, runway });
                map.insert({ runway->m_end2.m_name, runway });
                map.insert({ runway->m_end1.m_name + "/" + runway->m_end2.m_name, runway });
                map.insert({ runway->m_end2.m_name + "/" + runway->m_end1.m_name, runway });
            }
            return map;
        };

        const auto resolveActiveZoneMask = [&airport](ActiveZoneMask& mask) {
            mask.m_runwaysMask = 0;

            if (mask.m_runwayNames.size() > 0)
            {
                for (const string& name : mask.m_runwayNames)
                {
                    const auto& runway = airport->getRunwayOrThrow(name);
                    mask.m_runwaysMask |= runway->m_maskBit;
                }
            }
        };

        const auto resolveHighSpeedExits = [&](const shared_ptr<Runway>& runway, const shared_ptr<TaxiNode>& nodeOnRunway) {
            for (const auto& edge : nodeOnRunway->m_edges)
            {
                if (edge->m_type != TaxiEdge::Type::Taxiway)
                {
                    continue;
                }

                float turnDegrees1 = GeoMath::getTurnDegrees(runway->m_end1.m_heading, edge->m_heading);
                if (abs(turnDegrees1) < 60)
                {
                    edge->m_highSpeedExitRunway = runway->m_end1.m_name;
                    continue;
                }

                float turnDegrees2 = GeoMath::getTurnDegrees(runway->m_end2.m_heading, edge->m_heading);
                if (abs(turnDegrees2) < 60)
                {
                    edge->m_highSpeedExitRunway = runway->m_end2.m_name;
                }
            }
        };

        const auto resolveEdgeRunwayEnds = [&](
            const shared_ptr<Runway>& runway,
            const shared_ptr<TaxiEdge>& edge1,
            const shared_ptr<TaxiEdge>& edge2
        ) {
            float end1Turn = GeoMath::getTurnDegrees(runway->m_end1.m_heading, edge1->m_heading);
            edge1->m_runwayEndName = abs(end1Turn) < 45
                ? runway->m_end1.m_name
                : runway->m_end2.m_name;

            if (edge2)
            {
                edge2->m_runway = runway;
                edge2->m_runwayEndName = abs(end1Turn) < 45
                    ? runway->m_end2.m_name
                    : runway->m_end1.m_name;
            }
        };

        const auto resolveEdgeRunways = [&](shared_ptr<TaxiEdge> edge) {
            switch (edge->m_type)
            {
            case TaxiEdge::Type::Runway:
                edge->m_runway = airport->getRunwayOrThrow(edge->m_name);
                edge->m_runway->m_edges.push_back(edge);
                edge->m_node1->m_hasRunway = true;
                edge->m_node2->m_hasRunway = true;
                resolveEdgeRunwayEnds(edge->m_runway, edge, edge->m_flipOver);
                resolveHighSpeedExits(edge->m_runway, edge->m_node1);
                resolveHighSpeedExits(edge->m_runway, edge->m_node2);
                break;
            case TaxiEdge::Type::Taxiway:
                edge->m_node1->m_hasTaxiway = true;
                edge->m_node2->m_hasTaxiway = true;
                break;
            }

            resolveActiveZoneMask(edge->m_activeZones.departue);
            resolveActiveZoneMask(edge->m_activeZones.arrival);
            resolveActiveZoneMask(edge->m_activeZones.ils);
        };

        const auto resolveAllEdgeRunways = [=]() {
            const auto& taxiEdges = airport->m_taxiNet->m_edges;
            for (const auto edge : taxiEdges)
            {
                resolveEdgeRunways(edge);
                if (edge->canFlipOver())
                {
                    resolveEdgeRunways(TaxiEdge::flipOver(edge));
                }
            }
        };

        buildRunwayByNameMap();
        calcRunwayHeadings();
        resolveAllEdgeRunways();
    }

    void WorldBuilder::linkAirportTowerAirspace(
        shared_ptr<HostServices> host,
        shared_ptr<Airport> airport,
        shared_ptr<ControlFacility> tower,
        shared_ptr<ControlledAirspace> airspace)
    {
        if (tower)
        {
            airport->m_tower = tower;
            tower->m_airport = airport;
            tower->m_airspace = airspace;
        }

        if (airspace)
        {
            airspace->m_airport = airport;
            airspace->m_controllingFacility = tower;
        }
    }
}
