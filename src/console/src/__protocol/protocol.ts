
export type AircraftCategory = 'heavy' | 'jet' | 'turboprop' | 'prop' | 'helicopter';
export type AircraftOperationType = 'ga' | 'airline' | 'cargo' | 'military';
export type ParkingStandType = 'gate' | 'remote' | 'hangar' | 'unknown';
export type ParkingStandWidthClass = 'A'|'B'|'C'|'D'|'E'|'F';
export type TaxiEdgeType = 'taxiway' | 'runway';

export interface GeoPoint {
    lat: number;
    lon: number;
    alt: number;
}

export interface Discriminator {
    _t: string;
}

export interface Airport extends Partial<Discriminator> {
    runways: Runway[];
    parkingStands: ParkingStand[];
    taxiNet: TaxiNet;
}

export interface Runway {
    width: number;
    length: number;
    end1: RunwayEnd;
    end2: RunwayEnd;
}

export interface RunwayEnd {
    lat: Number;
    lon: number;
    name: string;
}

export interface ParkingStand {
    name: string;
    type: ParkingStandType;
    width: ParkingStandWidthClass;
    lat: number;
    lon: number;
    heading: number;
    categories: AircraftCategory[];
    operations: AircraftOperationType[];
    airlines: string[];
}

export interface TaxiNet {
    nodes: TaxiNode[];
    edges: TaxiEdge[];
}

export interface TaxiNode {
    id: number;
    location: GeoPoint;
}

export interface TaxiEdgeActiveZones {
    departure?: string[];
    arrival?: string[];
    ils?: string[];
}

export interface TaxiEdge {
    id: number;
    name: string;
    type: TaxiEdgeType;
    nodeId1: number;
    nodeId2: number;
    isOneWay?: boolean;
    isJunction?: boolean;
    isHighSpeedExit?: boolean;
    activeZones?: TaxiEdgeActiveZones | null;
}

export interface TaxiPath extends Partial<Discriminator> {
    fromNodeId: number;
    toNodeId: number;
    edgeIds: number[];
}
