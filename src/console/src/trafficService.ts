import { RemoveMessageListener, Timestamp, TrafficEntry, TrafficEntryMap, TrafficQuery, TrafficService, TrafficServiceCallback, WorldServiceClient } from "./appServices";
import { AircraftMessage, AircraftMessage_Situation, GeoPoint, ServerToClient } from "./proto/atc";



export function createTrafficService(worldService: WorldServiceClient): TrafficService {

    const subscribers = new Set<TrafficServiceCallback>();
    let intervalId: number | undefined = undefined;
    let endpointUnsubscribes: RemoveMessageListener[] = []; 

    const getNow = (): Timestamp => (new Date()).getTime();
    const createEntryMap = () => new Map<string, TrafficEntry>();

    let entries: TrafficEntryMap = createEntryMap();
    let lastQuery: TrafficQuery | undefined = undefined;
    let lastNotifySubscribersTimestamp = 0;
    
    //const getElapsed = (t0: Timestamp, t1: Timestamp): Timestamp => t1 - t0;

    const performLocalExtrapolation = (situation: AircraftMessage_Situation, elapsed: Timestamp): AircraftMessage_Situation => {
        
        const groundSpeedKt = 350; //TODO: AircraftMessage_Situation must have it
        const moveStraight = (
            location: Partial<GeoPoint>, 
            heading: number, 
            timeHours: number
        ): GeoPoint => {
            const distanceMeters = timeHours * groundSpeedKt * 1852;
            const origin = new google.maps.LatLng(location.lat!, location.lon!);
            
            const destination = window.google.maps.geometry.spherical.computeOffset(origin, distanceMeters, heading);
            return { 
                lat: destination.lat(), 
                lon: destination.lng() 
            };
        }

        const elapsedHours = elapsed / 3600000;
        const newLocation = moveStraight(situation.location!, situation.heading!, elapsedHours);
        return {
            ...situation,
            location: newLocation
        };
    };

    const performUpdatesOnEntry = (entry: TrafficEntry, now: Timestamp): Timestamp => {
        // if entry server data is stale enough (>100ms):
        // - calculate local situation based on last server data and the time passed
        // - mark local data as effective
        // otherwise
        // - mark server data as effective
        // mutate existing entry object
        // return timestamp of the effective data

        // if (now - entry.serverDataTimestamp < 50) {
        //     entry.effectiveData = entry.serverData;
        //     entry.effectiveDataTimestamp = entry.serverDataTimestamp;
        // } else {
            entry.localData = performLocalExtrapolation(entry.serverData, now - entry.serverDataTimestamp);
            entry.localDataTimestamp = now;
            entry.effectiveData = entry.localData;
            entry.effectiveDataTimestamp = now;
        //}

        entry.wasUpdatedFromServer = (now - entry.serverDataTimestamp < 1500);
        return entry.effectiveDataTimestamp;
    };

    const createAircraftEntryInMap = (aircraft: AircraftMessage, now: Timestamp, destination: TrafficEntryMap) => {
        const newEntry: TrafficEntry = {
            aircraft: aircraft,
            serverData: aircraft.situation!,
            serverDataTimestamp: now,
            localData: aircraft.situation!,
            localDataTimestamp: now,
            effectiveData: aircraft.situation!,
            effectiveDataTimestamp: now,
            wasUpdatedFromServer: true
        };
        destination.set(`${aircraft.id}`, newEntry);
    }

    const receiveQueryResults = (envelope: ServerToClient) => {
        // replace all entries with data from server
        //TODO: create new map from results

        const results = envelope.replyQueryTraffic?.trafficBatch!;
        const now = getNow();
        const newMap = new Map<string, TrafficEntry>();

        for (let result of results) {
            createAircraftEntryInMap(result, now, newMap);
        }

        console.log('>>> TrafficService > receiveQueryResults', results.length, 'ids:', results.map(r => r.id).join(','));
        entries = newMap;
    };

    const onAircraftUpdatedNotification = (envelope: ServerToClient) => {
        // update entries with latest data from server

        const notification = envelope.notifyAircraftSituationUpdated!;
        const { airctaftId: aircraftId, situation } = notification; //TODO fix the typo in proto

        let entry = entries.get(`${aircraftId}`);
        if (entry) {
            entry.serverData = situation!;
            entry.localData = situation!;
            entry.serverDataTimestamp = getNow();
        }

        //TODO: what if the entry doesn't exist?
    };

    const onAircraftCreatedNotification = (envelope: ServerToClient) => {
        const notification = envelope.notifyAircraftCreated!;
        const { aircraft } = notification;

        createAircraftEntryInMap(aircraft!, getNow(), entries);
    };

    const onAircraftRemovedNotification = (envelope: ServerToClient) => {
        const notification = envelope.notifyAircraftRemoved!;
        entries.delete(`${notification.airctaftId}`);
    };

    const performUpdates = () => {
        // update all entries with local extrapolations and/or effective data/timestamp
        // if effective timestamp of any entry is past last subscribers invocation:
        // - update last subscribers invocation timestamp
        // - invoke the subscribers

        const now = getNow();
        let mostRecentDataTimestamp: Timestamp = 0;

        entries.forEach(entry => {
            const effectiveTimestamp = performUpdatesOnEntry(entry, now);
            if (effectiveTimestamp > mostRecentDataTimestamp) {
                mostRecentDataTimestamp = effectiveTimestamp;
            }
        });

        if (!!lastQuery && mostRecentDataTimestamp > lastNotifySubscribersTimestamp) {
            lastNotifySubscribersTimestamp = now;
            subscribers.forEach(callback => callback(lastQuery!, entries));
        }
    }

    const isIdenticalQuery = (newQuery: TrafficQuery): boolean => {
        if (!lastQuery) {
            return false;
        }

        const oldRect = lastQuery.bounds;
        const newRect = newQuery.bounds;

        return (
            newRect.minLat === oldRect.minLat &&
            newRect.minLon === oldRect.minLon &&
            newRect.maxLat === oldRect.maxLat &&
            newRect.maxLon === oldRect.maxLon
        );
    }

    const service: TrafficService = {
        beginQuery(query: TrafficQuery) {
            service.start();
            if (isIdenticalQuery(query)) {
                return;
            }

            lastQuery = query;

            const { bounds } = query;
            worldService.sendMessage({
                queryTraffic: {
                    minLat: bounds.minLat,
                    minLon: bounds.minLon,
                    maxLat: bounds.maxLat,
                    maxLon: bounds.maxLon,
                }
            });
        },
        subscribe(callback) {
            subscribers.add(callback);
            return () => {
                subscribers.delete(callback);
            }
        },
        start() {
            if (!intervalId) {
                intervalId = window.setInterval(performUpdates, 100);
                endpointUnsubscribes = [
                    worldService.onMessage('replyQueryTraffic', receiveQueryResults),
                    worldService.onMessage('notifyAircraftSituationUpdated', onAircraftUpdatedNotification),
                    worldService.onMessage('notifyAircraftCreated', onAircraftCreatedNotification),
                    worldService.onMessage('notifyAircraftRemoved', onAircraftRemovedNotification),
                ];
            }
        },
        stop() {
            if (intervalId) {
                window.clearInterval(intervalId);
                endpointUnsubscribes.forEach(unsub => unsub());

                intervalId = undefined;
                endpointUnsubscribes = [];
                lastQuery = undefined;
            }
        }
    };

    return service;
}