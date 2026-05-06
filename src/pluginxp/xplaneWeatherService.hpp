// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <algorithm>
#include <utility>

#include "XPLMWeather.h"
#include "libworld.h"

using namespace std;
using namespace world;

class XPlaneWeatherService : public WeatherService
{
private:
    shared_ptr<HostServices> m_host;
public:
    explicit XPlaneWeatherService(shared_ptr<HostServices> host) :
        m_host(std::move(host))
    {
        if (m_host)
        {
            m_host->writeLog("WXSRV|XPlaneWeatherService initialized");
        }
    }
public:
    WeatherSnapshot getWeatherAt(const GeoPoint& location, float altitudeMeters = 0.0f) override
    {
        WeatherSnapshot snapshot;

#if defined(XPLM400)
        snapshot.available = true;
        XPLMWeatherInfo_t info = { 0 };
        info.structSize = sizeof(XPLMWeatherInfo_t);

        const int detailed = XPLMGetWeatherAtLocation(location.latitude, location.longitude, altitudeMeters, &info);
        snapshot.detailed = detailed != 0;
        snapshot.temperatureC = info.temperature_alt;
        snapshot.dewpointC = info.dewpoint_alt;
        snapshot.pressurePa = info.pressure_alt > 0.0f ? info.pressure_alt : info.pressure_sl;
        snapshot.visibilityMeters = info.visibility;
        snapshot.precipitationRate = info.precip_rate_alt;
        snapshot.windDirectionTrueDegrees = info.wind_dir_alt;
        snapshot.windSpeedMetersPerSecond = max(0.0f, info.wind_spd_alt);
        snapshot.windGustMetersPerSecond = 0.0f;

        for (int i = 0 ; i < XPLM_NUM_WIND_LAYERS ; ++i)
        {
            const auto& layer = info.wind_layers[i];
            if (layer.speed < 0.0f)
            {
                continue;
            }

            snapshot.windLayerAltitudesMeters.push_back(layer.alt_msl);
            snapshot.windLayerSpeedsMetersPerSecond.push_back(layer.speed);
            snapshot.windLayerDirectionsTrueDegrees.push_back(layer.direction);
            snapshot.windGustMetersPerSecond = max(snapshot.windGustMetersPerSecond, layer.gust_speed);
        }

        for (int i = 0 ; i < XPLM_NUM_CLOUD_LAYERS ; ++i)
        {
            const auto& layer = info.cloud_layers[i];
            if (layer.coverage <= 0.0f && layer.alt_base <= 0.0f && layer.alt_top <= 0.0f)
            {
                continue;
            }

            snapshot.cloudLayerCoverages.push_back(layer.coverage);
            snapshot.cloudLayerBasesMeters.push_back(layer.alt_base);
            snapshot.cloudLayerTopsMeters.push_back(layer.alt_top);
        }
#else
        if (m_host)
        {
            static bool s_loggedMissingApi = false;
            if (!s_loggedMissingApi)
            {
                s_loggedMissingApi = true;
                m_host->writeLog("WXSRV|XPLMWeather API unavailable at compile time; returning empty snapshot");
            }
        }
#endif

        return snapshot;
    }
};
