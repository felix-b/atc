import React from 'react'
import { GoogleMap, useJsApiLoader, Marker } from '@react-google-maps/api';
import { ClientToServer, ServerToClient, DeepPartial, AircraftMessage_Situation, GeoPoint, ServerToClient_ReplyQueryTraffic, AircraftMessage } from "./proto/atc";
import { GeoRect, TrafficEntry, TrafficEntryMap, TrafficQuery, TrafficService } from './appServices';
import { AppServices } from './appServices';


type AirTrafficLayerProps = {
    services: AppServices;
};

type AirTrafficLayerState = {
    query: TrafficQuery;
    entries: TrafficEntry[];
};

type AirTrafficLayerPureProps = {
    query: TrafficQuery;
    entries: TrafficEntry[];
}

type AirplaneMarkerProps = { 
    position: google.maps.LatLngLiteral; 
    heading: number;
    altitudeFeetMsl: number;
    wasUpdatedFromServer: boolean;
};

const getRefreshIntervalMilliseconds = (zoomLevel: number): number => {
    return 5000;
    // if (zoomLevel < 5) {
    //     return 5000;
    // }
    // if (zoomLevel < 8) {
    //     return 1000;
    // }
    // if (zoomLevel < 10) {
    //     return 500;
    // }
    // return 100;
}

export function AirTrafficLayerPure({entries}: AirTrafficLayerPureProps) {
    return (
        <>
            {entries.map(({ aircraft, effectiveData: data, wasUpdatedFromServer }) => {
                return (
                    <AirplaneMarker 
                        key={aircraft.id} 
                        position={{ lat: data.location!.lat, lng: data.location!.lon }}
                        heading={data.heading!}
                        altitudeFeetMsl={18000} 
                        wasUpdatedFromServer={wasUpdatedFromServer}/>
                );
            })}
        </>
    );
}

export const AirTrafficLayer = ({services:{trafficService}}: AirTrafficLayerProps) => {
    //console.log('>>> AirTrafficLayer.RENDER() bounds =', bounds);

    const [state, setState] = React.useState<AirTrafficLayerState | undefined>(undefined);

    React.useEffect(() => {
        console.log('>>> AirTrafficLayer > subscribing to TrafficService');

        const unsubscribe = trafficService.subscribe((query, newEntries) => {
            //console.log('>>> AirTrafficLayer > UPDATING TRAFFIC ENTRIES');
            setState({ 
                query, 
                entries: [...newEntries.values()],
            });
        });

        return () => {
            console.log('>>> AirTrafficLayer > unsubscribing from TrafficService');
            unsubscribe();
        };
    }, []);

    return (state 
        ? <AirTrafficLayerPure query={state.query} entries={state.entries} />
        : <></>  
    );
}

function AirplaneMarker({ position, heading, wasUpdatedFromServer }: AirplaneMarkerProps) {
    return (
        <Marker
            icon={{
                path: "m5 12.97-5.329-1.938-5.329 1.938c-.153.056-.325.033-.458-.06-.133-.094-.213-.247-.213-.41v-1.446c0-.52.264-.996.705-1.272l3.295-2.059v-6.484l-9.314 3.726c-.328.129-.686-.111-.686-.465v-.958c0-.815.398-1.581 1.066-2.048l8.934-6.254v-4.24c0-1.103.897-2 2-2s2 .897 2 2v4.24l8.934 6.253c.668.468 1.066 1.234 1.066 2.049v.958c0 .355-.361.596-.686.464l-9.314-3.726v6.484l3.295 2.06c.441.277.705.752.705 1.272v1.446c0 .163-.08.316-.213.41-.134.093-.306.116-.458.06z",
                fillColor: wasUpdatedFromServer ? "cyan" : "yellow",
                fillOpacity: 0.9,
                scale: 1,
                strokeColor: wasUpdatedFromServer ? "blue" : "red",
                strokeWeight: 3,
                rotation: heading
            }}
            position={{
                ...position
            }}
        />
    );
}

