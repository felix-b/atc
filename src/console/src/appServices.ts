import React from "react";
import { AircraftMessage, AircraftMessage_Situation, ClientToServer, DeepPartial, ServerToClient } from "./proto/atc";

export interface AppServices {
    readonly trafficService: TrafficService;
    readonly worldService: WorldServiceClient;
}

export interface WorldServiceClient {
    onOpen: (listener: StatusListener) => void;
    onMessage: (payloadType: IncomingPayloadType, listener: MessageListener) => RemoveMessageListener;
    sendMessage: (message: DeepPartial<ClientToServer>) => void;
    tuneRadio: (khz: number) => void;
}

export type IncomingPayloadType = keyof Omit<
    ServerToClient, 
    'id'|'sentAt'|'replyToRequestId'|'sentAt'|'requestSentAt'|'requestReceivedAt'
>;
export type IncomingMetaProps = keyof Pick<
    ServerToClient, 
    'id'|'sentAt'|'replyToRequestId'|'sentAt'|'requestSentAt'|'requestReceivedAt'
>;

export type OutgoingPayloadType = keyof Omit<ClientToServer, 'id'>;
export type OutgoingMetaProps = keyof Pick<ClientToServer, 'id'>; 

export type MessageListener = (envelope: ServerToClient) => void;
export type StatusListener = () => void;
export type RemoveMessageListener = () => void;

export type GeoRect = {
    minLat: number; 
    minLon: number; 
    maxLat: number; 
    maxLon: number; 
};

export interface TrafficService {
    beginQuery(query: TrafficQuery): void;
    subscribe(callback: TrafficServiceCallback): TrafficServiceUnsubscribe;
    start(): void;
    stop(): void;
}

export type TrafficQuery = {
    bounds: GeoRect;
    cancellationKey?: string;
}

export interface TrafficEntry {
    aircraft: Omit<AircraftMessage, 'situation'>;
    serverData: AircraftMessage_Situation;
    serverDataTimestamp: Timestamp;
    localData: AircraftMessage_Situation;
    localDataTimestamp: Timestamp;
    effectiveData: AircraftMessage_Situation;
    effectiveDataTimestamp: Timestamp;
    wasUpdatedFromServer: boolean;
}

export type Timestamp = number; // as returned by Date.getTime()
export type TrafficEntryMap = Map<string, TrafficEntry>;

export type TrafficServiceCallback = (query: TrafficQuery, entries: TrafficEntryMap) => void;
export type TrafficServiceUnsubscribe = () => void;
