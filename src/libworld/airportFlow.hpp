//
// AirportFlow - Defines runway configurations for different wind conditions
// Supports multiple active runways based on airport-specific flow patterns
//
#pragma once

#include <string>
#include <vector>
#include <memory>
#include "libworld.h"

using namespace std;

namespace world
{
    // Defines runway usage for a specific flow configuration
    struct FlowRunwayConfig
    {
        string runwayName;
        bool useForArrival;
        bool useForDeparture;
        
        FlowRunwayConfig(const string& name, bool arrival, bool departure)
            : runwayName(name), useForArrival(arrival), useForDeparture(departure) {}
    };

    // Defines a flow pattern (e.g., "NORTH_FLOW", "SOUTH_FLOW") for an airport
    // based on wind direction and other operational preferences
    class AirportFlow
    {
    private:
        string m_name;
        // Wind direction range that activates this flow (true degrees)
        float m_windFromDegrees;
        float m_windToDegrees;
        vector<FlowRunwayConfig> m_runways;
        
    public:
        AirportFlow(const string& name, float windFrom, float windTo)
            : m_name(name), m_windFromDegrees(windFrom), m_windToDegrees(windTo) {}
        
        const string& name() const { return m_name; }
        
        // Check if wind direction matches this flow
        bool matchesWind(float windDirectionDegrees) const
        {
            // Handle wrap-around case (e.g., 350° to 010°)
            if (m_windFromDegrees > m_windToDegrees)
            {
                return windDirectionDegrees >= m_windFromDegrees || windDirectionDegrees <= m_windToDegrees;
            }
            return windDirectionDegrees >= m_windFromDegrees && windDirectionDegrees <= m_windToDegrees;
        }
        
        void addRunway(const string& name, bool arrival, bool departure)
        {
            m_runways.push_back(FlowRunwayConfig(name, arrival, departure));
        }
        
        vector<string> getArrivalRunways() const
        {
            vector<string> result;
            for (const auto& rwy : m_runways)
            {
                if (rwy.useForArrival)
                    result.push_back(rwy.runwayName);
            }
            return result;
        }
        
        vector<string> getDepartureRunways() const
        {
            vector<string> result;
            for (const auto& rwy : m_runways)
            {
                if (rwy.useForDeparture)
                    result.push_back(rwy.runwayName);
            }
            return result;
        }
        
        const vector<FlowRunwayConfig>& allRunways() const { return m_runways; }
    };

    // Registry of airport flows
    class AirportFlowRegistry
    {
    private:
        unordered_map<string, vector<AirportFlow>> m_flows;
        
    public:
        void registerFlow(const string& airportIcao, const AirportFlow& flow)
        {
            m_flows[airportIcao].push_back(flow);
        }
        
        // Get active flow for an airport based on wind direction
        // Returns nullptr if no specific flow is configured
        const AirportFlow* getActiveFlow(const string& airportIcao, float windDirectionDegrees) const
        {
            auto it = m_flows.find(airportIcao);
            if (it == m_flows.end())
                return nullptr;
            
            for (const auto& flow : it->second)
            {
                if (flow.matchesWind(windDirectionDegrees))
                    return &flow;
            }
            return nullptr;
        }
        
        bool hasFlows(const string& airportIcao) const
        {
            return m_flows.find(airportIcao) != m_flows.end();
        }
        
        // Setup known airport flows
        static AirportFlowRegistry createWithDefaultFlows();
    };
}
