// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <memory>
#include <cctype>
#include <sstream>
#include "libworld.h"

namespace
{
    static std::string normalizeRunwayName(const std::string& name)
    {
        if (name.empty())
        {
            return name;
        }

        if (name.size() > 1 && name.front() == '0' && std::isdigit(static_cast<unsigned char>(name.at(1))))
        {
            return name.substr(1);
        }

        if (std::isdigit(static_cast<unsigned char>(name.front())))
        {
            if (name.size() == 1 || (name.size() == 2 && std::isalpha(static_cast<unsigned char>(name.at(1)))))
            {
                return std::string("0") + name;
            }
        }

        return name;
    }

    static bool runwayHasIlsCoverage(const world::Airport& airport, const shared_ptr<world::Runway>& runway)
    {
        auto taxiNet = airport.taxiNet();
        if (!taxiNet || !runway)
        {
            return false;
        }

        for (const auto& edge : taxiNet->edges())
        {
            if (edge->activeZones().ils.has(runway))
            {
                return true;
            }
        }

        return false;
    }

    static shared_ptr<world::Runway> findLongestRunwayFrom(const vector<shared_ptr<world::Runway>>& runways)
    {
        const auto compareRunwayLength = [](const shared_ptr<world::Runway>& r1, const shared_ptr<world::Runway>& r2) {
            return (r1->lengthMeters() < r2->lengthMeters());
        };

        auto it = max_element(runways.begin(), runways.end(), compareRunwayLength);
        return it != runways.end() ? *it : nullptr;
    }
}

namespace world
{
    shared_ptr<Runway> Airport::findLongestRunway() const
    {
        shared_ptr<Runway> longestRunway = findLongestRunwayFrom(m_runways);

        if (!longestRunway)
        {
            stringstream errorMessage;
            errorMessage
                << "Could not find longest runway at ["
                << m_header.icao()
                << "] are there any runways at this airport??";
            throw runtime_error(errorMessage.str());
        }

        return longestRunway;
    }

    shared_ptr<Runway> Airport::findPreferredArrivalRunway() const
    {
        vector<shared_ptr<Runway>> candidates;

        for (const auto& runwayName : m_mutableState->activeArrivalRunways)
        {
            auto runway = tryFindRunway(runwayName);
            if (runway)
            {
                candidates.push_back(runway);
            }
        }

        if (candidates.empty())
        {
            candidates = m_runways;
        }

        if (candidates.empty())
        {
            stringstream errorMessage;
            errorMessage
                << "Could not find preferred arrival runway at ["
                << m_header.icao()
                << "] are there any runways at this airport??";
            throw runtime_error(errorMessage.str());
        }

        vector<shared_ptr<Runway>> ilsCandidates;
        for (const auto& runway : candidates)
        {
            if (runwayHasIlsCoverage(*this, runway))
            {
                ilsCandidates.push_back(runway);
            }
        }

        const auto& preferredCandidates = !ilsCandidates.empty() ? ilsCandidates : candidates;
        auto preferredRunway = findLongestRunwayFrom(preferredCandidates);

        if (!preferredRunway)
        {
            stringstream errorMessage;
            errorMessage
                << "Could not resolve preferred arrival runway at ["
                << m_header.icao()
                << "]";
            throw runtime_error(errorMessage.str());
        }

        return preferredRunway;
    }

    const vector<shared_ptr<Runway>>& Airport::findLongestParallelRunwayGroup() const
    {
        const auto calcAverageRunwayLength = [](const vector<shared_ptr<Runway>>& group)->float {
            float sum = 0;
            for (int i = 0 ; i < group.size() ; i++)
            {
                sum += group[i]->lengthMeters();
            }
            return group.size() > 0 ? sum / group.size() : 0;
        };

        vector<float> averages;
        transform(
            m_parallelRunwayGroups.begin(),
            m_parallelRunwayGroups.end(),
            back_inserter(averages),
            calcAverageRunwayLength);

        int indexOfMaxAverage = distance(averages.begin(), max_element(averages.begin(), averages.end()));
        return m_parallelRunwayGroups.at(indexOfMaxAverage);
    }

    shared_ptr<Runway> Airport::getRunwayOrThrow(const string& name) const
    {
        auto runway = tryFindRunway(name);
        if (!runway)
        {
            stringstream errorMessage;
            errorMessage 
                << "Runway '" << name 
                << "' could not be found at airport '" << m_header.icao() << "'";
            throw runtime_error(errorMessage.str());
        }
        return runway;
    }

    const Runway::End& Airport::getRunwayEndOrThrow(const string& name) const
    {
        auto runway = getRunwayOrThrow(name);
        return runway->getEndOrThrow(name);
    }

    shared_ptr<Runway> Airport::tryFindRunway(const string& name) const
    {
        auto found = m_runwayByName.find(name);
        return found != m_runwayByName.end()
            ? found->second
            : [&]() -> shared_ptr<Runway> {
                const std::string normalizedName = normalizeRunwayName(name);
                if (normalizedName != name)
                {
                    const auto normalizedFound = m_runwayByName.find(normalizedName);
                    if (normalizedFound != m_runwayByName.end())
                    {
                        return normalizedFound->second;
                    }
                }
                return nullptr;
            }();
    }

    bool Airport::isRunwayActive(const string& runwayName) const
    {
        auto runwayToCheck = getRunwayOrThrow(runwayName);

        for (const auto& name : m_mutableState->activeArrivalRunways)
        {
            auto arrival = getRunwayOrThrow(name);
            if (arrival == runwayToCheck)
            {
                return true;
            }
        }

        for (const auto& name : m_mutableState->activeDepartureRunways)
        {
            auto departure = getRunwayOrThrow(name);
            if (departure == runwayToCheck)
            {
                return true;
            }
        }

        return false;
    }

    shared_ptr<ParkingStand> Airport::getParkingStandOrThrow(const string& name) const
    {
        auto parking = tryFindParkingStand(name);
        if (!parking)
        {
            throw runtime_error("Specified parking stand could not be found");
        }
        return parking;
    }

    shared_ptr<ParkingStand> Airport::tryFindParkingStand(const string& name) const
    {
        auto found = m_parkingStandByName.find(name);
        return found != m_parkingStandByName.end()
            ? found->second
            : nullptr;
    }

    shared_ptr<ParkingStand> Airport::findClosestParkingStand(const GeoPoint& location)
    {
        ClosestItemFinder<ParkingStand> finder(location);
        for (const auto& gate : m_parkingStands)
        {
            finder.next(gate);
        }
        return finder.getClosest();
    }

    void Airport::selectActiveRunways()
    {
        m_mutableState->activeDepartureRunways.clear();
        m_mutableState->activeArrivalRunways.clear();

        bool hasSelectedRunways = false;

        if (m_tower)
        {
            for (const auto& position : m_tower->positions())
            {
                if (position->type() == ControllerPosition::Type::Local)
                {
                    position->selectActiveRunways(
                        m_mutableState->activeDepartureRunways,
                        m_mutableState->activeArrivalRunways);
                    hasSelectedRunways = !m_mutableState->activeDepartureRunways.empty() || !m_mutableState->activeArrivalRunways.empty();
                }
            }
        }

        if (!hasSelectedRunways && !m_runways.empty())
        {
            const auto preferredArrivalRunway = findPreferredArrivalRunway();

            if (hasParallelRunways())
            {
                const auto& longestGroup = findLongestParallelRunwayGroup();
                if (longestGroup.size() >= 2)
                {
                    auto departureRunwayName = longestGroup.at(0)->end1().name();
                    auto preferredArrivalRunwayName = preferredArrivalRunway->end1().name();

                    if (departureRunwayName == preferredArrivalRunwayName)
                    {
                        departureRunwayName = longestGroup.at(1)->end1().name();
                    }

                    m_mutableState->activeDepartureRunways.push_back(departureRunwayName);
                    m_mutableState->activeArrivalRunways.push_back(preferredArrivalRunwayName);
                    if (longestGroup.size() > 2)
                    {
                        m_mutableState->activeArrivalRunways.push_back(longestGroup.at(longestGroup.size() - 1)->end1().name());
                    }
                    hasSelectedRunways = true;
                }
                else
                {
                    const auto preferredArrivalRunwayName = preferredArrivalRunway->end1().name();
                    m_mutableState->activeDepartureRunways.push_back(preferredArrivalRunwayName);
                    m_mutableState->activeArrivalRunways.push_back(preferredArrivalRunwayName);
                    hasSelectedRunways = true;
                }
            }
            else
            {
                const auto preferredArrivalRunwayName = preferredArrivalRunway->end1().name();
                m_mutableState->activeDepartureRunways.push_back(preferredArrivalRunwayName);
                m_mutableState->activeArrivalRunways.push_back(preferredArrivalRunwayName);
                hasSelectedRunways = true;
            }
        }

        if (hasSelectedRunways)
        {
            calculateActiveRunwaysBounds();
        }
    }

    void Airport::selectArrivalAndDepartureTaxiways()
    {
        for (const auto& departureRunwayName : m_mutableState->activeDepartureRunways)
        {
            const auto& runwayEnd = getRunwayOrThrow(departureRunwayName)->getEndOrThrow(departureRunwayName);

            for (const auto &gate : m_parkingStands)
            {
                m_taxiNet->tryFindDepartureTaxiPathToRunway(gate->location().geo(), runwayEnd);
            }
        }
    }

    void Airport::calculateActiveRunwaysBounds()
    {
        for (const auto& runway : m_mutableState->activeArrivalRunways)
        {
            getRunwayOrThrow(runway)->calculateBounds();
        }

        for (const auto& runway : m_mutableState->activeDepartureRunways)
        {
            getRunwayOrThrow(runway)->calculateBounds();
        }
    }

    shared_ptr<TrafficFlow> Airport::getActiveFlow(float windDirectionDegrees) const
    {
        for (const auto& flow : m_trafficFlows)
        {
            if (flow->matchesWind(windDirectionDegrees))
            {
                return flow;
            }
        }
        return nullptr;
    }
}
