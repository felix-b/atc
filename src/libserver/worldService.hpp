#pragma once

#include <functional>
#include <memory>
#include <fstream>
#include <unordered_map>
#include <stdarg.h> 

#include "libworld.h"
#include "libdataxp.h"
#include "world.pb.h"
#include "interfaces.hpp"
#include "protocolConverter.hpp"

using namespace std;
using namespace world;

namespace server
{
    class WorldService : public WorldServiceInterface
    {
    private:
        shared_ptr<HostServices> m_host;
    public:
        WorldService(shared_ptr<HostServices> _host) :
            m_host(_host)
        {
        }
    public:

        void setBroadcastInterface(DispatcherInterface::ReplyCallback broadcastInterface) override
        {
            //TODO
        }

        void connect(
            const world_proto::ClientToServer_Connect& request,
            world_proto::ServerToClient& replyEnvelope) override
        {
            m_host->writeLog("SRVSVC|received connect");

            if (request.token().compare("HELLO") == 0)
            {
                replyEnvelope.mutable_reply_connect()->set_server_banner("AT&C plugin");
                m_host->writeLog("SRVSVC|connect > reply OK");
            }
            else
            {
                replyEnvelope.mutable_fault_declined()->set_message("Bad token");
                m_host->writeLog("SRVSVC|connect > reply DECLINE");
            }
        }

        void queryAirport(
            const world_proto::ClientToServer_QueryAirport& request,
            world_proto::ServerToClient& replyEnvelope) override
        {
            m_host->writeLog("SRVSVC|received queryAirport icao[%s]", request.icao_code().c_str());

            try
            {
                const auto airport = m_host->getWorld()->getAirport(request.icao_code());
                ProtocolConverter::toMessage(
                    airport,
                    *replyEnvelope.mutable_reply_query_airport()->mutable_airport()
                );
                m_host->writeLog("SRVSVC|queryAirport > reply APT OK");
            }
            catch (const exception& e)
            {
                const auto fault = replyEnvelope.mutable_fault_not_found();
                fault->set_message("Airport not found");
                m_host->writeLog("SRVSVC|queryAirport > reply NOT FOUND (error: %s)", e.what());
            }
        }

        void queryTaxiPath(
            const world_proto::ClientToServer_QueryTaxiPath& request,
            world_proto::ServerToClient& replyEnvelope) override
        {
            m_host->writeLog(
                "SRVSVC|received queryAirport apt[%s] from[%f,%f] to[%f,%f] ac[%s]",
                request.airport_icao().c_str(),
                request.from_point().lat(),
                request.from_point().lon(),
                request.to_point().lat(),
                request.to_point().lon(),
                request.aircraft_model_icao().c_str());

            const auto isTaxiEdge = [](const shared_ptr<TaxiEdge>& edge) {
                return (edge->type() == TaxiEdge::Type::Taxiway);
            };
            const auto hasTaxiEdges = [isTaxiEdge](const shared_ptr<TaxiNode>& node) {
                return hasAny<shared_ptr<TaxiEdge>>(node->edges(), isTaxiEdge);
            };

            world::GeoPoint fromPoint = { request.from_point().lat(), request.from_point().lon(), 0 };
            world::GeoPoint toPoint = { request.to_point().lat(), request.to_point().lon(), 0 };

            shared_ptr<Airport> airport;
            try
            {
                airport = m_host->getWorld()->getAirport(request.airport_icao());
            }
            catch (const exception& e)
            {
                const auto fault = replyEnvelope.mutable_fault_not_found();
                fault->set_message("Airport not found");
                m_host->writeLog("SRVSVC|queryTaxiPath > reply FAULT APT NOT FOUND (error: %s)", e.what());
            }

            const auto fromNode = airport->taxiNet()->findClosestNode(fromPoint, hasTaxiEdges);
            const auto toNode = airport->taxiNet()->findClosestNode(toPoint, hasTaxiEdges);

            if (!fromNode || !toNode)
            {
                m_host->writeLog("SRVSVC|queryTaxiPath > reply FAULT ENDS NOT FOUND");
                replyEnvelope.mutable_fault_not_found()->set_message("Origin/destination taxi node not found");
                return;
            }

            const auto taxiPath = TaxiPath::find(airport->taxiNet(), fromNode, toNode);
            if (!taxiPath)
            {
                m_host->writeLog("SRVSVC|queryTaxiPath > reply FAULT PATH NOT FOUND");
                replyEnvelope.mutable_fault_not_found()->set_message("Taxi path does not exist");
                return;
            }

            *replyEnvelope.mutable_reply_query_taxi_path()->mutable_taxi_path() = ProtocolConverter::toMessage(taxiPath);
            m_host->writeLog("SRVSVC|queryTaxiPath > reply PATH OK");
        }
    };
}
