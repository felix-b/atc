import React from 'react'
import { GoogleMap, useJsApiLoader, Marker } from '@react-google-maps/api';
import { WorldServiceEndpoint } from './worldServiceEndpoint';
import { ClientToServer, ServerToClient, DeepPartial, AircraftMessage_Situation, GeoPoint } from "./proto/atc";

type AirplaneMarkerProps = { 
    position: google.maps.LatLngLiteral; 
    heading: number;
    altitudeFeetMsl: number;
};

type MapMarkerLayerProps = {
    traffic: DeepPartial<AircraftMessage_Situation>[]
};

const initialTrafficArray: DeepPartial<AircraftMessage_Situation>[]  = [
    { squawk: '2222', location: { lat: 32.02, lon: 34.01 }, heading: 0 },
    { squawk: '3333', location: { lat: 32.01, lon: 34.02 }, heading: 90 },
    { squawk: '4444', location: { lat: 32.00, lon: 34.01 }, heading: 180 },
    { squawk: '1111', location: { lat: 32.01, lon: 34.00 }, heading: 270 },
];

function MapView() {

    const containerStyle = {
        width: '100vw',
        height: '100vh'
    };
    
    const center = {
        lat: 32.007258, 
        lng: 34.880114
    };
    
    const { isLoaded } = useJsApiLoader({
        id: 'google-map-script',
        googleMapsApiKey: process.env.REACT_APP_GM!,
        libraries: ['geometry']
    });

    const [map, setMap] = React.useState(null);

    const onLoad = React.useCallback(function callback(map) {
        const bounds = new window.google.maps.LatLngBounds({ lat: 32, lng: 33 }, { lat: 34, lng: 35 });
        map.fitBounds(bounds);
        setMap(map);
    }, []);

    const onUnmount = React.useCallback(function callback(map) {
        setMap(null);
    }, []);

    return isLoaded ? (
        <GoogleMap
            mapContainerStyle={containerStyle}
            center={center}
            zoom={50}
            onLoad={onLoad}
            onUnmount={onUnmount}
        >
            <MapMarkerLayer traffic={initialTrafficArray}/>
        </GoogleMap>
    ) : <></>
}

function MapMarkerLayer(props: MapMarkerLayerProps) {
    
    const [airplanes, setAirplanes] = React.useState<DeepPartial<AircraftMessage_Situation>[]>(props.traffic);

    const moveStraight = (location: Partial<GeoPoint>, heading: number): Partial<GeoPoint> => {
        switch (heading) {
            case 0: return {...location, lat: location.lat! + 0.00025};
            case 90: return {...location, lon: location.lon! + 0.00025};
            case 180: return {...location, lat: location.lat! - 0.00025};
            case 270: return {...location, lon: location.lon! - 0.00025};
            default: return location;
        }
    }

    const updateTrafficSituation = () => {
        let newAirplanes = airplanes.map(airplane => {
            return {
                ...airplane,
                location: moveStraight(airplane.location!, airplane.heading!)
            };
        });
        setAirplanes(newAirplanes);
    };

    let intervalId: NodeJS.Timeout;
    
    React.useEffect(() => {
        intervalId = setInterval(updateTrafficSituation, 100);
        return () => {
            clearInterval(intervalId);
        };
    });

    return (
        <>
            {airplanes.map(aircraft => {
                return (
                    <AirplaneMarker 
                        key={aircraft.squawk} 
                        position={{ lat: aircraft.location!.lat!, lng: aircraft.location!.lon! }}
                        heading={aircraft.heading!}
                        altitudeFeetMsl={18000} />
                );
            })}
        </>
    );
}

function AirplaneMarker({ position, heading }: AirplaneMarkerProps) {
    return (
        <Marker
            icon={{
                path: "m5 12.97-5.329-1.938-5.329 1.938c-.153.056-.325.033-.458-.06-.133-.094-.213-.247-.213-.41v-1.446c0-.52.264-.996.705-1.272l3.295-2.059v-6.484l-9.314 3.726c-.328.129-.686-.111-.686-.465v-.958c0-.815.398-1.581 1.066-2.048l8.934-6.254v-4.24c0-1.103.897-2 2-2s2 .897 2 2v4.24l8.934 6.253c.668.468 1.066 1.234 1.066 2.049v.958c0 .355-.361.596-.686.464l-9.314-3.726v6.484l3.295 2.06c.441.277.705.752.705 1.272v1.446c0 .163-.08.316-.213.41-.134.093-.306.116-.458.06z",
                fillColor: "yellow",
                fillOpacity: 0.9,
                scale: 2,
                strokeColor: "red",
                strokeWeight: 3,
                rotation: heading
            }}
            position={{
                ...position
            }}
        />
    );
}

export default React.memo(MapView)
//<div>Icons made by <a href="https://icon54.com/" title="Pixel perfect">Pixel perfect</a> from <a href="https://www.flaticon.com/" title="Flaticon">www.flaticon.com</a></div>
/*

        <Marker
            icon={{
                path: "m17.329 23.97-5.329-1.938-5.329 1.938c-.153.056-.325.033-.458-.06-.133-.094-.213-.247-.213-.41v-1.446c0-.52.264-.996.705-1.272l3.295-2.059v-6.484l-9.314 3.726c-.328.129-.686-.111-.686-.465v-.958c0-.815.398-1.581 1.066-2.048l8.934-6.254v-4.24c0-1.103.897-2 2-2s2 .897 2 2v4.24l8.934 6.253c.668.468 1.066 1.234 1.066 2.049v.958c0 .355-.361.596-.686.464l-9.314-3.726v6.484l3.295 2.06c.441.277.705.752.705 1.272v1.446c0 .163-.08.316-.213.41-.134.093-.306.116-.458.06z",
                fillColor: "yellow",
                fillOpacity: 0.9,
                scale: 2,
                strokeColor: "gold",
                strokeWeight: 2,
            }}
            position={{
                lat: 37.772,
                lng: -122.214
            }}
        />



    return isLoaded ? (
        <GoogleMap
            mapContainerStyle={containerStyle}
            center={center}
            zoom={100}
            onLoad={onLoad}
            onUnmount={onUnmount}
        >
            { 
            
            }
            <></>
        </GoogleMap>
    ) : <></>


*/