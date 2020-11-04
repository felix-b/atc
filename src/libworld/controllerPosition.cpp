// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include "libworld.h"

using namespace std;

namespace world
{
    void ControllerPosition::progressTo(chrono::microseconds timestamp)
    {
        m_frequency->progressTo(timestamp);
        if (m_controller)
        {
            m_controller->progressTo(timestamp);
        }
    }

    void ControllerPosition::clearFlights()
    {
        if (m_frequency)
        {
            m_frequency->clearTransmissions();
        }
        if (m_controller)
        {
            m_controller->clearFlights();
        }
    }

    void ControllerPosition::startListenOnFrequency()
    {
        m_frequency->addListener([=](shared_ptr<Intent> intent) {
            if (m_controller)
            {
                m_controller->receiveIntent(intent);
            }
        });
    }

    void ControllerPosition::selectActiveRunways(vector<string>& departureRunways, vector<string>& arrivalRunways)
    {
        if (m_controller)
        {
            m_host->writeLog("CFACIL|Selecting active runways");
            departureRunways.clear();
            arrivalRunways.clear();
            m_controller->selectActiveRunways(departureRunways, arrivalRunways);
        }
        else
        {
            m_host->writeLog("CFACIL|Cannot select active runways: controller not assigned");
        }
    }
}
