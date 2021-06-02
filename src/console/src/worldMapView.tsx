import React from 'react'
import { GoogleMap, useJsApiLoader, Marker, TrafficLayer } from '@react-google-maps/api';
import { ClientToServer, ServerToClient, DeepPartial, AircraftMessage_Situation, GeoPoint, ServerToClient_ReplyQueryTraffic, AircraftMessage } from "./proto/atc";
import { AppServices, GeoRect } from './appServices';
import { AirTrafficLayer } from './airTrafficLayer';
import { LoadScriptUrlOptions } from '@react-google-maps/api/dist/utils/make-load-script-url';

type WorldMapViewState = {
    map: google.maps.Map | null;
    bounds: GeoRect | undefined;
};

const initialState: WorldMapViewState = {
    map: null,
    bounds: undefined
};

const googleMapLibraries: LoadScriptUrlOptions['libraries'] = [
    'geometry'
];

let nextGlobalMapInstanceId = 1;
let globalMapInstance: google.maps.Map | null = null;
let globalMapInstanceId: number | undefined = undefined;

function WorldMapView(props: AppServices) {

    const instanceId = nextGlobalMapInstanceId++;
    console.log('>>> WorldMapView.RENDER() INSTANCE ID', instanceId);

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
        libraries: googleMapLibraries
    });

    const [state, setState] = React.useState<WorldMapViewState>(initialState);

    const onLoad = React.useCallback(function callback(loadedMap) {
        console.log(`>>> WorldMapView[${instanceId}].onLoad() !!!!!!!!!!!!!`);

        const bounds = new window.google.maps.LatLngBounds({ lat: 32, lng: 33 }, { lat: 34, lng: 35 });
        loadedMap.fitBounds(bounds);

        globalMapInstance = loadedMap;
        globalMapInstanceId = instanceId;

        (window as any).theMap = loadedMap;
        setState({ map: loadedMap, bounds: undefined });
    }, []);

    const onUnmount = React.useCallback(function callback(map) {
        console.log(`>>> WorldMapView[${instanceId}].onUnmount() state =`, state);
        setState(initialState);
    }, []);

    const onIdle = React.useCallback(() => {
        console.log(`>>> WorldMapView[${instanceId}].onIdle() state =`, state);
        if (globalMapInstanceId === instanceId) {
            const bounds = globalMapInstance?.getBounds();
            console.log('>>> GOOGLE MAPS > onIdle', bounds);

            props.trafficService.beginQuery({
                bounds: geoRectFromBounds(bounds)
            });

            // setState({ 
            //     map: globalMapInstance, 
            //     bounds: geoRectFromBounds(bounds)
            // });
        } else {
            console.log('>>> GOOGLE MAPS > onIdle: GLOBAL INSTANCE MISMATCH!');
        }
    }, []);

    return isLoaded ? (
        <GoogleMap
            mapContainerStyle={containerStyle}
            center={center}
            zoom={50}
            onLoad={onLoad}
            onIdle={onIdle} 
            onUnmount={onUnmount}
        >
            {true /*!!state.bounds*/
                ? <AirTrafficLayer 
                    services={props} 
                    bounds={ /*state.bounds*/  { minLat: -90, minLon: -180, maxLat: 90, maxLon: 180 } } 
                /> 
                : <></> 
            }
        </GoogleMap>
    ) : <></>
}

function geoRectFromBounds(bounds: google.maps.LatLngBounds | null | undefined): GeoRect {
    if (!bounds) {
        throw new Error(`geoRectFromBounds: argument 'bounds' is null or undefined`);
    }
    
    const ne = bounds.getNorthEast();
    const sw = bounds.getSouthWest();
    
    return {
        minLat: sw.lat(),
        minLon: sw.lng(),
        maxLat: ne.lat(),
        maxLon: ne.lng()
    };
}

// function MapMarkerLayer(props: MapMarkerLayerProps) {
    
//     const [airplanes, setAirplanes] = React.useState<DeepPartial<AircraftMessage_Situation>[]>(props.traffic);

//     const moveStraight = (location: Partial<GeoPoint>, heading: number, timeHours: number): Partial<GeoPoint> => {
//         const distanceMeters = timeHours * groundSpeedKt * 1852;
//         const origin = new google.maps.LatLng(location.lat!, location.lon!);
        
//         const destination = window.google.maps.geometry.spherical.computeOffset(origin, distanceMeters, heading);
//         return { 
//             lat: destination.lat(), 
//             lon: destination.lng() 
//         };
        
//         // switch (heading) {
//         //     case 0: return {...location, lat: location.lat! + 0.00025};
//         //     case 90: return {...location, lon: location.lon! + 0.00025};
//         //     case 180: return {...location, lat: location.lat! - 0.00025};
//         //     case 270: return {...location, lon: location.lon! - 0.00025};
//         //     default: return location;
//         // }
//     }


//     const updateTrafficSituation = (hoursElapsed: number) => {
//         let newAirplanes = airplanes.map(airplane => {
//             return {
//                 ...airplane,
//                 location: moveStraight(airplane.location!, airplane.heading!, hoursElapsed)
//             };
//         });
//         setAirplanes(newAirplanes);
//     };

//     let intervalId: number;
//     let lastUpdateTime = (new Date()).getTime();
    
//     React.useEffect(() => {
//         lastUpdateTime = (new Date()).getTime();
//         intervalId = window.setInterval(() => {
//             const now = (new Date()).getTime();
//             const elapsedMs = now - lastUpdateTime;
//             const elapsedHours = elapsedMs / 3600000;
//             lastUpdateTime = now;
//             updateTrafficSituation(elapsedHours);
//         }, 100);

//         return () => {
//             window.clearInterval(intervalId);
//         };
//     });


//     // WorldServiceEndpoint.sendMessage({
//     //     queryTraffic: {
//     //         minLat: 10.0,
//     //         minLon: 10.0,
//     //         maxLat: 31.0,
//     //         maxLon: 31.0
//     //     }});
//     //replyQueryTraffic


//     return (
//         <>
//             {airplanes.map(aircraft => {
//                 return (
//                     <AirplaneMarker 
//                         key={aircraft.squawk} 
//                         position={{ lat: aircraft.location!.lat!, lng: aircraft.location!.lon! }}
//                         heading={aircraft.heading!}
//                         altitudeFeetMsl={18000} />
//                 );
//             })}
//         </>
//     );
// }

// function AirplaneMarker({ position, heading }: AirplaneMarkerProps) {
//     return (
//         <Marker
//             icon={{
//                 path: "m5 12.97-5.329-1.938-5.329 1.938c-.153.056-.325.033-.458-.06-.133-.094-.213-.247-.213-.41v-1.446c0-.52.264-.996.705-1.272l3.295-2.059v-6.484l-9.314 3.726c-.328.129-.686-.111-.686-.465v-.958c0-.815.398-1.581 1.066-2.048l8.934-6.254v-4.24c0-1.103.897-2 2-2s2 .897 2 2v4.24l8.934 6.253c.668.468 1.066 1.234 1.066 2.049v.958c0 .355-.361.596-.686.464l-9.314-3.726v6.484l3.295 2.06c.441.277.705.752.705 1.272v1.446c0 .163-.08.316-.213.41-.134.093-.306.116-.458.06z",
//                 fillColor: "yellow",
//                 fillOpacity: 0.9,
//                 scale: 2,
//                 strokeColor: "red",
//                 strokeWeight: 3,
//                 rotation: heading
//             }}
//             position={{
//                 ...position
//             }}
//         />
//     );
// }

export default React.memo(WorldMapView)

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