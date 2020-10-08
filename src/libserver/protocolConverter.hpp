#include <functional>

#include "libworld.h"
#include "world.pb.h"

class ProtocolConverter
{
public:
    static void toMessage(shared_ptr<world::Airport> airport, world_proto::Airport& message)
    {
        message.set_icao(airport->header().icao());
        message.mutable_location()->set_lat(30);
        message.mutable_location()->set_lon(-73);

        for (const auto& runway : airport->runways())
        {
            message.mutable_runways()->Add(toMessage(runway));
        }

        for (const auto& parkingStand : airport->parkingStands())
        {
            message.mutable_parking_stands()->Add(toMessage(parkingStand));
        }

        for (const auto& taxiNode : airport->taxiNet()->nodes())
        {
            message.mutable_taxi_nodes()->Add(toMessage(taxiNode));
        }

        for (const auto& taxiEdge : airport->taxiNet()->edges())
        {
            message.mutable_taxi_edges()->Add(toMessage(taxiEdge));
        }
    }

    static world_proto::Runway toMessage(const shared_ptr<world::Runway> runway)
    {
        world_proto::Runway message;

        *message.mutable_end_1() = toMessage(runway->end1());
        *message.mutable_end_2() = toMessage(runway->end2());
        message.set_mask_bit(runway->maskBit());
        message.set_length_meters(runway->lengthMeters());
        message.set_width_meters(runway->widthMeters());

        return message;
    }

    static world_proto::Runway::End toMessage(const world::Runway::End& runwayEnd)
    {
        world_proto::Runway::End message;

        message.set_name(runwayEnd.name());
        message.set_heading(runwayEnd.heading());
        *message.mutable_centerline_point() = toMessage(runwayEnd.centerlinePoint());
        message.set_displaced_threshold_meters(runwayEnd.displacedThresholdMeters());
        message.set_overrun_area_meters(runwayEnd.overrunAreaMeters());

        return message;
    }

    static world_proto::ParkingStand toMessage(const shared_ptr<world::ParkingStand> parkingStand)
    {
        world_proto::ParkingStand message;

        const auto addCategoryIf = [&](world::Aircraft::Category category) {
            if (parkingStand->hasAircraftCategory(category))
            {
                message.mutable_categories()->Add((int32_t)category);
            }
        };

        const auto addOperationIf = [&](world::Aircraft::OperationType operation) {
            if (parkingStand->hasOperationType(operation))
            {
                message.mutable_operation_types()->Add((int32_t)operation);
            }
        };

        message.set_id(parkingStand->id()); 
        message.set_name(parkingStand->name());
        message.set_type((world_proto::ParkingStand_Type)parkingStand->type());
        *message.mutable_location() = toMessage(parkingStand->location());
        message.set_heading(parkingStand->heading());
        message.set_width_code(parkingStand->widthCode());

        addCategoryIf(world::Aircraft::Category::Heavy);
        addCategoryIf(world::Aircraft::Category::Jet);
        addCategoryIf(world::Aircraft::Category::Turboprop);
        addCategoryIf(world::Aircraft::Category::Prop);
        addCategoryIf(world::Aircraft::Category::LightProp);
        addCategoryIf(world::Aircraft::Category::Helicopter);
        addCategoryIf(world::Aircraft::Category::All);

        addOperationIf(world::Aircraft::OperationType::GA);
        addOperationIf(world::Aircraft::OperationType::Airline);
        addOperationIf(world::Aircraft::OperationType::Cargo);
        addOperationIf(world::Aircraft::OperationType::Military);

        for (const string& airlineIcao : parkingStand->airlines())
        {
            message.mutable_airline_icaos()->Add(std::string(airlineIcao));
        }

        return message;
    }

    static world_proto::TaxiNode toMessage(const shared_ptr<world::TaxiNode> taxiNode)
    {
        world_proto::TaxiNode message;

        message.set_id(taxiNode->id());
        message.set_is_junction(taxiNode->isJunction());
        *message.mutable_location() = toMessage(taxiNode->location());

        return message;
    }

    static world_proto::TaxiEdge toMessage(const shared_ptr<world::TaxiEdge> taxiEdge)
    {
        world_proto::TaxiEdge message;

        message.set_id(taxiEdge->id());
        message.set_name(taxiEdge->name());
        message.set_node_id_1(taxiEdge->nodeId1());
        message.set_node_id_2(taxiEdge->nodeId2());
        message.set_type((world_proto::TaxiEdge_Type)taxiEdge->type());

        message.set_is_one_way(taxiEdge->isOneWay());
        message.set_is_high_speed_exit(false);//TODO: update proto taxiEdge->isHighSpeedExit());
        message.set_length_meters(taxiEdge->lengthMeters());
        message.set_heading(0); //TODO: add heading to core model

        message.mutable_active_zones()->set_arrival((uint32_t)taxiEdge->activeZones().arrival.runwaysMask());
        message.mutable_active_zones()->set_departure((uint32_t)taxiEdge->activeZones().departue.runwaysMask());
        message.mutable_active_zones()->set_ils((uint32_t)taxiEdge->activeZones().ils.runwaysMask());

        return message;
    }

    static world_proto::TaxiPath toMessage(const shared_ptr<world::TaxiPath> taxiPath)
    {
        world_proto::TaxiPath message;

        message.set_from_node_id(taxiPath->fromNode->id());
        message.set_to_node_id(taxiPath->toNode->id());

        for (const auto edge : taxiPath->edges)
        {
            message.mutable_edge_ids()->Add(edge->id());
        }

        return message;
    }

    static world_proto::GeoPoint toMessage(const world::UniPoint uniPoint)
    {
        world_proto::GeoPoint message;

        message.set_lat(uniPoint.latitude());
        message.set_lon(uniPoint.longitude());

        return message;
    }
};
