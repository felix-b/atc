// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <sstream>
#include <iomanip>
#include "libworld.h"

using namespace std;


namespace world
{
    constexpr float Aircraft::MaxAltitudeAGL;

    void Aircraft::assignFlight(shared_ptr<Flight> _flight)
    {
        m_flight = _flight;
    }

    void Aircraft::setFrequencyKhz(int _frequencyKhz)
    {
        auto flightPtr = flight().lock();
        if (!flightPtr)
        {
            host()->writeLog("AIRCRF|TUNE WARNING: aircraft [%d] has no flight assigned, ignoring", m_id);
            return;
        }

        m_frequencyKhz = _frequencyKhz;

        auto newFrequency = flightPtr
            ? host()->getWorld()->tryFindCommFrequency(flightPtr, _frequencyKhz)
            : nullptr;

        setFrequency(newFrequency);
    }

    void Aircraft::setFrequency(shared_ptr<Frequency> _frequency)
    {
        if (m_frequency && m_frequencyListenerId >= 0)
        {
            m_frequency->removeListener(m_frequencyListenerId);
            m_frequencyListenerId = -1;
            m_frequencyKhz = -1;
        }

        m_frequency = _frequency;

        if (m_frequency)
        {
            m_frequencyKhz = m_frequency->khz();
            m_frequencyListenerId = m_frequency->addListener([=](shared_ptr<Intent> intent) {
                m_onCommTransmission(intent);
            });
        }

        auto flightPtr = m_flight.lock();
        auto controllerPosition = m_frequency ? m_frequency->controllerPosition() : nullptr;
        m_host->writeLog(
            controllerPosition
            ? "AIRCRF|TUNE: aircraft[%d] flight[%s] tuned COM1 to khz[%d] ATC[%s]"
            : "AIRCRF|TUNE WARNING: aircraft[%d] flight[%s] tuned COM1 to khz[%d] - but no ATC on frequency",
            m_id,
            flightPtr ? flightPtr->callSign().c_str() : "N/A",
            m_frequencyKhz,
            controllerPosition ? controllerPosition->callSign().c_str() : "N/A");
    }

    shared_ptr<World::ChangeSet> Aircraft::getWorldChangeSet() const
    {
        return m_onChanges();
    }

    shared_ptr<Flight> Aircraft::getFlightOrThrow()
    {
        auto flightPtr = m_flight.lock();
        if (flightPtr)
        {
            return flightPtr;
        }

        throw runtime_error("Aircraft id[" + to_string(m_id) + "] getFlightOrThrow: no flight was assigned");
    }
}
