/* eslint-disable */
import { Timestamp } from './google/protobuf/timestamp';
import * as Long from 'long';
import { Writer, Reader, util, configure } from 'protobufjs/minimal';


export interface ClientToServer {
  id: number;
  sentAt: Date | undefined;
  connect: ClientToServer_Connect | undefined;
  queryAirport: ClientToServer_QueryAirport | undefined;
  createAircraft: ClientToServer_CreateAircraft | undefined;
  updateAircraftSituation: ClientToServer_UpdateAircraftSituation | undefined;
  removeAircraft: ClientToServer_RemoveAircraft | undefined;
  queryTaxiPath: ClientToServer_QueryTaxiPath | undefined;
}

export interface ClientToServer_Connect {
  token: string;
}

export interface ClientToServer_QueryAirport {
  icaoCode: string;
}

export interface ClientToServer_QueryTaxiPath {
  airportIcao: string;
  aircraftModelIcao: string;
  fromPoint: GeoPoint | undefined;
  toPoint: GeoPoint | undefined;
}

export interface ClientToServer_CreateAircraft {
  aircraft: Aircraft | undefined;
}

export interface ClientToServer_UpdateAircraftSituation {
  aircraftId: number;
  situation: Aircraft_Situation | undefined;
}

export interface ClientToServer_RemoveAircraft {
  aircraftId: number;
}

export interface ServerToClient {
  id: number;
  replyToRequestId: number;
  sentAt: Date | undefined;
  requestSentAt: Date | undefined;
  requestReceivedAt: Date | undefined;
  replyConnect: ServerToClient_ReplyConnect | undefined;
  replyQueryAirport: ServerToClient_ReplyQueryAirport | undefined;
  replyCreateAircraft: ServerToClient_ReplyCreateAircraft | undefined;
  replyQueryTaxiPath: ServerToClient_ReplyQueryTaxiPath | undefined;
  notifyAircraftCreated: ServerToClient_NotifyAircraftCreated | undefined;
  notifyAircraftSituationUpdated: ServerToClient_NotifyAircraftSituationUpdated | undefined;
  notifyAircraftRemoved: ServerToClient_NotifyAircraftRemoved | undefined;
  faultDeclined: ServerToClient_FaultDeclined | undefined;
  faultNotFound: ServerToClient_FaultNotFound | undefined;
}

export interface ServerToClient_FaultDeclined {
  message: string;
}

export interface ServerToClient_FaultNotFound {
  message: string;
}

export interface ServerToClient_ReplyConnect {
  serverBanner: string;
}

export interface ServerToClient_ReplyCreateAircraft {
  createdAircraftId: number;
}

export interface ServerToClient_ReplyQueryAirport {
  airport: Airport | undefined;
}

export interface ServerToClient_ReplyQueryTaxiPath {
  success: boolean;
  taxiPath: TaxiPath | undefined;
}

export interface ServerToClient_NotifyAircraftCreated {
  aircraft: Aircraft | undefined;
}

export interface ServerToClient_NotifyAircraftSituationUpdated {
  airctaftId: number;
  situation: Aircraft_Situation | undefined;
}

export interface ServerToClient_NotifyAircraftRemoved {
  airctaftId: number;
}

export interface GeoPoint {
  lat: number;
  lon: number;
}

export interface GeoPolygon {
  edges: GeoPolygon_GeoEdge[];
}

export interface GeoPolygon_GeoEdge {
  type: GeoEdgeType;
  fromPoint: GeoPoint | undefined;
}

export interface Vector3d {
  lat: number;
  lon: number;
  alt: number;
}

export interface Attitude {
  heading: number;
  pitch: number;
  roll: number;
}

export interface Airport {
  icao: string;
  location: GeoPoint | undefined;
  runways: Runway[];
  parkingStands: ParkingStand[];
  taxiNodes: TaxiNode[];
  taxiEdges: TaxiEdge[];
}

export interface Runway {
  widthMeters: number;
  lengthMeters: number;
  maskBit: number;
  end1: Runway_End | undefined;
  end2: Runway_End | undefined;
}

export interface Runway_End {
  name: string;
  heading: number;
  centerlinePoint: GeoPoint | undefined;
  displacedThresholdMeters: number;
  overrunAreaMeters: number;
}

export interface TaxiNode {
  id: number;
  location: GeoPoint | undefined;
  isJunction: boolean;
}

export interface TaxiEdge {
  id: number;
  name: string;
  nodeId1: number;
  nodeId2: number;
  type: TaxiEdgeType;
  isOneWay: boolean;
  isHighSpeedExit: boolean;
  lengthMeters: number;
  heading: number;
  activeZones: TaxiEdge_ActiveZoneMatrix | undefined;
}

export interface TaxiEdge_ActiveZoneMatrix {
  departure: number;
  arrival: number;
  ils: number;
}

export interface ParkingStand {
  id: number;
  name: string;
  type: ParkingStandType;
  location: GeoPoint | undefined;
  heading: number;
  widthCode: string;
  categories: AircraftCategory[];
  operationTypes: OperationType[];
  airlineIcaos: string[];
}

export interface AirspaceGeometry {
  lateralBounds: GeoPolygon | undefined;
  lowerBoundFeet: number;
  upperBoundFeet: number;
}

export interface Aircraft {
  id: number;
  modelIcao: string;
  airlineIcao: string;
  tailNo: string;
  callSign: string;
  situation: Aircraft_Situation | undefined;
}

export interface Aircraft_Situation {
  location: Vector3d | undefined;
  attitude: Attitude | undefined;
  velocity: Vector3d | undefined;
  acceleration: Vector3d | undefined;
  isOnGround: boolean;
  flapRatio: number;
  spoilerRatio: number;
  gearRatio: number;
  noseWheelAngle: number;
  landingLights: boolean;
  taxiLights: boolean;
  strobeLights: boolean;
  frequencyKhz: number;
  squawk: string;
  modeC: boolean;
  modeS: boolean;
}

export interface TaxiPath {
  fromNodeId: number;
  toNodeId: number;
  edgeIds: number[];
}

const baseClientToServer: object = {
  id: 0,
};

const baseClientToServer_Connect: object = {
  token: "",
};

const baseClientToServer_QueryAirport: object = {
  icaoCode: "",
};

const baseClientToServer_QueryTaxiPath: object = {
  airportIcao: "",
  aircraftModelIcao: "",
};

const baseClientToServer_CreateAircraft: object = {
};

const baseClientToServer_UpdateAircraftSituation: object = {
  aircraftId: 0,
};

const baseClientToServer_RemoveAircraft: object = {
  aircraftId: 0,
};

const baseServerToClient: object = {
  id: 0,
  replyToRequestId: 0,
};

const baseServerToClient_FaultDeclined: object = {
  message: "",
};

const baseServerToClient_FaultNotFound: object = {
  message: "",
};

const baseServerToClient_ReplyConnect: object = {
  serverBanner: "",
};

const baseServerToClient_ReplyCreateAircraft: object = {
  createdAircraftId: 0,
};

const baseServerToClient_ReplyQueryAirport: object = {
};

const baseServerToClient_ReplyQueryTaxiPath: object = {
  success: false,
};

const baseServerToClient_NotifyAircraftCreated: object = {
};

const baseServerToClient_NotifyAircraftSituationUpdated: object = {
  airctaftId: 0,
};

const baseServerToClient_NotifyAircraftRemoved: object = {
  airctaftId: 0,
};

const baseGeoPoint: object = {
  lat: 0,
  lon: 0,
};

const baseGeoPolygon: object = {
};

const baseGeoPolygon_GeoEdge: object = {
  type: 0,
};

const baseVector3d: object = {
  lat: 0,
  lon: 0,
  alt: 0,
};

const baseAttitude: object = {
  heading: 0,
  pitch: 0,
  roll: 0,
};

const baseAirport: object = {
  icao: "",
};

const baseRunway: object = {
  widthMeters: 0,
  lengthMeters: 0,
  maskBit: 0,
};

const baseRunway_End: object = {
  name: "",
  heading: 0,
  displacedThresholdMeters: 0,
  overrunAreaMeters: 0,
};

const baseTaxiNode: object = {
  id: 0,
  isJunction: false,
};

const baseTaxiEdge: object = {
  id: 0,
  name: "",
  nodeId1: 0,
  nodeId2: 0,
  type: 0,
  isOneWay: false,
  isHighSpeedExit: false,
  lengthMeters: 0,
  heading: 0,
};

const baseTaxiEdge_ActiveZoneMatrix: object = {
  departure: 0,
  arrival: 0,
  ils: 0,
};

const baseParkingStand: object = {
  id: 0,
  name: "",
  type: 0,
  heading: 0,
  widthCode: "",
  categories: 0,
  operationTypes: 0,
  airlineIcaos: "",
};

const baseAirspaceGeometry: object = {
  lowerBoundFeet: 0,
  upperBoundFeet: 0,
};

const baseAircraft: object = {
  id: 0,
  modelIcao: "",
  airlineIcao: "",
  tailNo: "",
  callSign: "",
};

const baseAircraft_Situation: object = {
  isOnGround: false,
  flapRatio: 0,
  spoilerRatio: 0,
  gearRatio: 0,
  noseWheelAngle: 0,
  landingLights: false,
  taxiLights: false,
  strobeLights: false,
  frequencyKhz: 0,
  squawk: "",
  modeC: false,
  modeS: false,
};

const baseTaxiPath: object = {
  fromNodeId: 0,
  toNodeId: 0,
  edgeIds: 0,
};

function fromJsonTimestamp(o: any): Date {
  if (o instanceof Date) {
    return o;
  } else if (typeof o === "string") {
    return new Date(o);
  } else {
    return fromTimestamp(Timestamp.fromJSON(o));
  }
}

function toTimestamp(date: Date): Timestamp {
  const seconds = date.getTime() / 1_000;
  const nanos = (date.getTime() % 1_000) * 1_000_000;
  return { seconds, nanos };
}

function fromTimestamp(t: Timestamp): Date {
  let millis = t.seconds * 1_000;
  millis += t.nanos / 1_000_000;
  return new Date(millis);
}

function longToNumber(long: Long) {
  if (long.gt(Number.MAX_SAFE_INTEGER)) {
    throw new globalThis.Error("Value is larger than Number.MAX_SAFE_INTEGER");
  }
  return long.toNumber();
}

export const protobufPackage = 'atc_proto'

export enum GeoEdgeType {
  GEO_EDGE_UNKNOWN = 0,
  GEO_EDGE_ARC_BY_EDGE = 1,
  GEO_EDGE_CIRCLE = 2,
  GEO_EDGE_GREAT_CIRCLE = 3,
  GEO_EDGE_RHUMB_LINE = 4,
  GEO_EDGE_CLOCKWISE_ARC = 5,
  GEO_EDGE_COUNTER_CLOCKWISE_ARC = 6,
  UNRECOGNIZED = -1,
}

export function geoEdgeTypeFromJSON(object: any): GeoEdgeType {
  switch (object) {
    case 0:
    case "GEO_EDGE_UNKNOWN":
      return GeoEdgeType.GEO_EDGE_UNKNOWN;
    case 1:
    case "GEO_EDGE_ARC_BY_EDGE":
      return GeoEdgeType.GEO_EDGE_ARC_BY_EDGE;
    case 2:
    case "GEO_EDGE_CIRCLE":
      return GeoEdgeType.GEO_EDGE_CIRCLE;
    case 3:
    case "GEO_EDGE_GREAT_CIRCLE":
      return GeoEdgeType.GEO_EDGE_GREAT_CIRCLE;
    case 4:
    case "GEO_EDGE_RHUMB_LINE":
      return GeoEdgeType.GEO_EDGE_RHUMB_LINE;
    case 5:
    case "GEO_EDGE_CLOCKWISE_ARC":
      return GeoEdgeType.GEO_EDGE_CLOCKWISE_ARC;
    case 6:
    case "GEO_EDGE_COUNTER_CLOCKWISE_ARC":
      return GeoEdgeType.GEO_EDGE_COUNTER_CLOCKWISE_ARC;
    case -1:
    case "UNRECOGNIZED":
    default:
      return GeoEdgeType.UNRECOGNIZED;
  }
}

export function geoEdgeTypeToJSON(object: GeoEdgeType): string {
  switch (object) {
    case GeoEdgeType.GEO_EDGE_UNKNOWN:
      return "GEO_EDGE_UNKNOWN";
    case GeoEdgeType.GEO_EDGE_ARC_BY_EDGE:
      return "GEO_EDGE_ARC_BY_EDGE";
    case GeoEdgeType.GEO_EDGE_CIRCLE:
      return "GEO_EDGE_CIRCLE";
    case GeoEdgeType.GEO_EDGE_GREAT_CIRCLE:
      return "GEO_EDGE_GREAT_CIRCLE";
    case GeoEdgeType.GEO_EDGE_RHUMB_LINE:
      return "GEO_EDGE_RHUMB_LINE";
    case GeoEdgeType.GEO_EDGE_CLOCKWISE_ARC:
      return "GEO_EDGE_CLOCKWISE_ARC";
    case GeoEdgeType.GEO_EDGE_COUNTER_CLOCKWISE_ARC:
      return "GEO_EDGE_COUNTER_CLOCKWISE_ARC";
    default:
      return "UNKNOWN";
  }
}

export enum AircraftCategory {
  AIRCRAFT_CATEGORY_NONE = 0,
  AIRCRAFT_CATEGORY_HEAVY = 1,
  AIRCRAFT_CATEGORY_JET = 2,
  AIRCRAFT_CATEGORY_TURBOPROP = 4,
  AIRCRAFT_CATEGORY_PROP = 8,
  AIRCRAFT_CATEGORY_LIGHT_PROP = 16,
  AIRCRAFT_CATEGORY_HELICPOTER = 32,
  UNRECOGNIZED = -1,
}

export function aircraftCategoryFromJSON(object: any): AircraftCategory {
  switch (object) {
    case 0:
    case "AIRCRAFT_CATEGORY_NONE":
      return AircraftCategory.AIRCRAFT_CATEGORY_NONE;
    case 1:
    case "AIRCRAFT_CATEGORY_HEAVY":
      return AircraftCategory.AIRCRAFT_CATEGORY_HEAVY;
    case 2:
    case "AIRCRAFT_CATEGORY_JET":
      return AircraftCategory.AIRCRAFT_CATEGORY_JET;
    case 4:
    case "AIRCRAFT_CATEGORY_TURBOPROP":
      return AircraftCategory.AIRCRAFT_CATEGORY_TURBOPROP;
    case 8:
    case "AIRCRAFT_CATEGORY_PROP":
      return AircraftCategory.AIRCRAFT_CATEGORY_PROP;
    case 16:
    case "AIRCRAFT_CATEGORY_LIGHT_PROP":
      return AircraftCategory.AIRCRAFT_CATEGORY_LIGHT_PROP;
    case 32:
    case "AIRCRAFT_CATEGORY_HELICPOTER":
      return AircraftCategory.AIRCRAFT_CATEGORY_HELICPOTER;
    case -1:
    case "UNRECOGNIZED":
    default:
      return AircraftCategory.UNRECOGNIZED;
  }
}

export function aircraftCategoryToJSON(object: AircraftCategory): string {
  switch (object) {
    case AircraftCategory.AIRCRAFT_CATEGORY_NONE:
      return "AIRCRAFT_CATEGORY_NONE";
    case AircraftCategory.AIRCRAFT_CATEGORY_HEAVY:
      return "AIRCRAFT_CATEGORY_HEAVY";
    case AircraftCategory.AIRCRAFT_CATEGORY_JET:
      return "AIRCRAFT_CATEGORY_JET";
    case AircraftCategory.AIRCRAFT_CATEGORY_TURBOPROP:
      return "AIRCRAFT_CATEGORY_TURBOPROP";
    case AircraftCategory.AIRCRAFT_CATEGORY_PROP:
      return "AIRCRAFT_CATEGORY_PROP";
    case AircraftCategory.AIRCRAFT_CATEGORY_LIGHT_PROP:
      return "AIRCRAFT_CATEGORY_LIGHT_PROP";
    case AircraftCategory.AIRCRAFT_CATEGORY_HELICPOTER:
      return "AIRCRAFT_CATEGORY_HELICPOTER";
    default:
      return "UNKNOWN";
  }
}

export enum OperationType {
  AIRCRAFT_OPERATION_NONE = 0,
  AIRCRAFT_OPERATION_GA = 1,
  AIRCRAFT_OPERATION_AIRLINE = 2,
  AIRCRAFT_OPERATION_CARGO = 4,
  AIRCRAFT_OPERATION_MILITARY = 8,
  UNRECOGNIZED = -1,
}

export function operationTypeFromJSON(object: any): OperationType {
  switch (object) {
    case 0:
    case "AIRCRAFT_OPERATION_NONE":
      return OperationType.AIRCRAFT_OPERATION_NONE;
    case 1:
    case "AIRCRAFT_OPERATION_GA":
      return OperationType.AIRCRAFT_OPERATION_GA;
    case 2:
    case "AIRCRAFT_OPERATION_AIRLINE":
      return OperationType.AIRCRAFT_OPERATION_AIRLINE;
    case 4:
    case "AIRCRAFT_OPERATION_CARGO":
      return OperationType.AIRCRAFT_OPERATION_CARGO;
    case 8:
    case "AIRCRAFT_OPERATION_MILITARY":
      return OperationType.AIRCRAFT_OPERATION_MILITARY;
    case -1:
    case "UNRECOGNIZED":
    default:
      return OperationType.UNRECOGNIZED;
  }
}

export function operationTypeToJSON(object: OperationType): string {
  switch (object) {
    case OperationType.AIRCRAFT_OPERATION_NONE:
      return "AIRCRAFT_OPERATION_NONE";
    case OperationType.AIRCRAFT_OPERATION_GA:
      return "AIRCRAFT_OPERATION_GA";
    case OperationType.AIRCRAFT_OPERATION_AIRLINE:
      return "AIRCRAFT_OPERATION_AIRLINE";
    case OperationType.AIRCRAFT_OPERATION_CARGO:
      return "AIRCRAFT_OPERATION_CARGO";
    case OperationType.AIRCRAFT_OPERATION_MILITARY:
      return "AIRCRAFT_OPERATION_MILITARY";
    default:
      return "UNKNOWN";
  }
}

export enum ParkingStandType {
  PARKING_UNKNOWN = 0,
  PARKING_GATE = 1,
  PARKING_REMOTE = 2,
  PARKING_HANGAR = 3,
  UNRECOGNIZED = -1,
}

export function parkingStandTypeFromJSON(object: any): ParkingStandType {
  switch (object) {
    case 0:
    case "PARKING_UNKNOWN":
      return ParkingStandType.PARKING_UNKNOWN;
    case 1:
    case "PARKING_GATE":
      return ParkingStandType.PARKING_GATE;
    case 2:
    case "PARKING_REMOTE":
      return ParkingStandType.PARKING_REMOTE;
    case 3:
    case "PARKING_HANGAR":
      return ParkingStandType.PARKING_HANGAR;
    case -1:
    case "UNRECOGNIZED":
    default:
      return ParkingStandType.UNRECOGNIZED;
  }
}

export function parkingStandTypeToJSON(object: ParkingStandType): string {
  switch (object) {
    case ParkingStandType.PARKING_UNKNOWN:
      return "PARKING_UNKNOWN";
    case ParkingStandType.PARKING_GATE:
      return "PARKING_GATE";
    case ParkingStandType.PARKING_REMOTE:
      return "PARKING_REMOTE";
    case ParkingStandType.PARKING_HANGAR:
      return "PARKING_HANGAR";
    default:
      return "UNKNOWN";
  }
}

export enum TaxiEdgeType {
  TAXI_EDGE_GROUNDWAY = 0,
  TAXI_EDGE_TAXIWAY = 1,
  TAXI_EDGE_RUNWAY = 2,
  UNRECOGNIZED = -1,
}

export function taxiEdgeTypeFromJSON(object: any): TaxiEdgeType {
  switch (object) {
    case 0:
    case "TAXI_EDGE_GROUNDWAY":
      return TaxiEdgeType.TAXI_EDGE_GROUNDWAY;
    case 1:
    case "TAXI_EDGE_TAXIWAY":
      return TaxiEdgeType.TAXI_EDGE_TAXIWAY;
    case 2:
    case "TAXI_EDGE_RUNWAY":
      return TaxiEdgeType.TAXI_EDGE_RUNWAY;
    case -1:
    case "UNRECOGNIZED":
    default:
      return TaxiEdgeType.UNRECOGNIZED;
  }
}

export function taxiEdgeTypeToJSON(object: TaxiEdgeType): string {
  switch (object) {
    case TaxiEdgeType.TAXI_EDGE_GROUNDWAY:
      return "TAXI_EDGE_GROUNDWAY";
    case TaxiEdgeType.TAXI_EDGE_TAXIWAY:
      return "TAXI_EDGE_TAXIWAY";
    case TaxiEdgeType.TAXI_EDGE_RUNWAY:
      return "TAXI_EDGE_RUNWAY";
    default:
      return "UNKNOWN";
  }
}

export const ClientToServer = {
  encode(message: ClientToServer, writer: Writer = Writer.create()): Writer {
    writer.uint32(8).uint64(message.id);
    if (message.sentAt !== undefined && message.sentAt !== undefined) {
      Timestamp.encode(toTimestamp(message.sentAt), writer.uint32(18).fork()).ldelim();
    }
    if (message.connect !== undefined) {
      ClientToServer_Connect.encode(message.connect, writer.uint32(810).fork()).ldelim();
    }
    if (message.queryAirport !== undefined) {
      ClientToServer_QueryAirport.encode(message.queryAirport, writer.uint32(818).fork()).ldelim();
    }
    if (message.createAircraft !== undefined) {
      ClientToServer_CreateAircraft.encode(message.createAircraft, writer.uint32(826).fork()).ldelim();
    }
    if (message.updateAircraftSituation !== undefined) {
      ClientToServer_UpdateAircraftSituation.encode(message.updateAircraftSituation, writer.uint32(834).fork()).ldelim();
    }
    if (message.removeAircraft !== undefined) {
      ClientToServer_RemoveAircraft.encode(message.removeAircraft, writer.uint32(842).fork()).ldelim();
    }
    if (message.queryTaxiPath !== undefined) {
      ClientToServer_QueryTaxiPath.encode(message.queryTaxiPath, writer.uint32(850).fork()).ldelim();
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ClientToServer {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseClientToServer } as ClientToServer;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.id = longToNumber(reader.uint64() as Long);
          break;
        case 2:
          message.sentAt = fromTimestamp(Timestamp.decode(reader, reader.uint32()));
          break;
        case 101:
          message.connect = ClientToServer_Connect.decode(reader, reader.uint32());
          break;
        case 102:
          message.queryAirport = ClientToServer_QueryAirport.decode(reader, reader.uint32());
          break;
        case 103:
          message.createAircraft = ClientToServer_CreateAircraft.decode(reader, reader.uint32());
          break;
        case 104:
          message.updateAircraftSituation = ClientToServer_UpdateAircraftSituation.decode(reader, reader.uint32());
          break;
        case 105:
          message.removeAircraft = ClientToServer_RemoveAircraft.decode(reader, reader.uint32());
          break;
        case 106:
          message.queryTaxiPath = ClientToServer_QueryTaxiPath.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ClientToServer {
    const message = { ...baseClientToServer } as ClientToServer;
    if (object.id !== undefined && object.id !== null) {
      message.id = Number(object.id);
    } else {
      message.id = 0;
    }
    if (object.sentAt !== undefined && object.sentAt !== null) {
      message.sentAt = fromJsonTimestamp(object.sentAt);
    } else {
      message.sentAt = undefined;
    }
    if (object.connect !== undefined && object.connect !== null) {
      message.connect = ClientToServer_Connect.fromJSON(object.connect);
    } else {
      message.connect = undefined;
    }
    if (object.queryAirport !== undefined && object.queryAirport !== null) {
      message.queryAirport = ClientToServer_QueryAirport.fromJSON(object.queryAirport);
    } else {
      message.queryAirport = undefined;
    }
    if (object.createAircraft !== undefined && object.createAircraft !== null) {
      message.createAircraft = ClientToServer_CreateAircraft.fromJSON(object.createAircraft);
    } else {
      message.createAircraft = undefined;
    }
    if (object.updateAircraftSituation !== undefined && object.updateAircraftSituation !== null) {
      message.updateAircraftSituation = ClientToServer_UpdateAircraftSituation.fromJSON(object.updateAircraftSituation);
    } else {
      message.updateAircraftSituation = undefined;
    }
    if (object.removeAircraft !== undefined && object.removeAircraft !== null) {
      message.removeAircraft = ClientToServer_RemoveAircraft.fromJSON(object.removeAircraft);
    } else {
      message.removeAircraft = undefined;
    }
    if (object.queryTaxiPath !== undefined && object.queryTaxiPath !== null) {
      message.queryTaxiPath = ClientToServer_QueryTaxiPath.fromJSON(object.queryTaxiPath);
    } else {
      message.queryTaxiPath = undefined;
    }
    return message;
  },
  fromPartial(object: DeepPartial<ClientToServer>): ClientToServer {
    const message = { ...baseClientToServer } as ClientToServer;
    if (object.id !== undefined && object.id !== null) {
      message.id = object.id;
    } else {
      message.id = 0;
    }
    if (object.sentAt !== undefined && object.sentAt !== null) {
      message.sentAt = object.sentAt;
    } else {
      message.sentAt = undefined;
    }
    if (object.connect !== undefined && object.connect !== null) {
      message.connect = ClientToServer_Connect.fromPartial(object.connect);
    } else {
      message.connect = undefined;
    }
    if (object.queryAirport !== undefined && object.queryAirport !== null) {
      message.queryAirport = ClientToServer_QueryAirport.fromPartial(object.queryAirport);
    } else {
      message.queryAirport = undefined;
    }
    if (object.createAircraft !== undefined && object.createAircraft !== null) {
      message.createAircraft = ClientToServer_CreateAircraft.fromPartial(object.createAircraft);
    } else {
      message.createAircraft = undefined;
    }
    if (object.updateAircraftSituation !== undefined && object.updateAircraftSituation !== null) {
      message.updateAircraftSituation = ClientToServer_UpdateAircraftSituation.fromPartial(object.updateAircraftSituation);
    } else {
      message.updateAircraftSituation = undefined;
    }
    if (object.removeAircraft !== undefined && object.removeAircraft !== null) {
      message.removeAircraft = ClientToServer_RemoveAircraft.fromPartial(object.removeAircraft);
    } else {
      message.removeAircraft = undefined;
    }
    if (object.queryTaxiPath !== undefined && object.queryTaxiPath !== null) {
      message.queryTaxiPath = ClientToServer_QueryTaxiPath.fromPartial(object.queryTaxiPath);
    } else {
      message.queryTaxiPath = undefined;
    }
    return message;
  },
  toJSON(message: ClientToServer): unknown {
    const obj: any = {};
    message.id !== undefined && (obj.id = message.id);
    message.sentAt !== undefined && (obj.sentAt = message.sentAt !== undefined ? message.sentAt.toISOString() : null);
    message.connect !== undefined && (obj.connect = message.connect ? ClientToServer_Connect.toJSON(message.connect) : undefined);
    message.queryAirport !== undefined && (obj.queryAirport = message.queryAirport ? ClientToServer_QueryAirport.toJSON(message.queryAirport) : undefined);
    message.createAircraft !== undefined && (obj.createAircraft = message.createAircraft ? ClientToServer_CreateAircraft.toJSON(message.createAircraft) : undefined);
    message.updateAircraftSituation !== undefined && (obj.updateAircraftSituation = message.updateAircraftSituation ? ClientToServer_UpdateAircraftSituation.toJSON(message.updateAircraftSituation) : undefined);
    message.removeAircraft !== undefined && (obj.removeAircraft = message.removeAircraft ? ClientToServer_RemoveAircraft.toJSON(message.removeAircraft) : undefined);
    message.queryTaxiPath !== undefined && (obj.queryTaxiPath = message.queryTaxiPath ? ClientToServer_QueryTaxiPath.toJSON(message.queryTaxiPath) : undefined);
    return obj;
  },
};

export const ClientToServer_Connect = {
  encode(message: ClientToServer_Connect, writer: Writer = Writer.create()): Writer {
    writer.uint32(10).string(message.token);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ClientToServer_Connect {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseClientToServer_Connect } as ClientToServer_Connect;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.token = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ClientToServer_Connect {
    const message = { ...baseClientToServer_Connect } as ClientToServer_Connect;
    if (object.token !== undefined && object.token !== null) {
      message.token = String(object.token);
    } else {
      message.token = "";
    }
    return message;
  },
  fromPartial(object: DeepPartial<ClientToServer_Connect>): ClientToServer_Connect {
    const message = { ...baseClientToServer_Connect } as ClientToServer_Connect;
    if (object.token !== undefined && object.token !== null) {
      message.token = object.token;
    } else {
      message.token = "";
    }
    return message;
  },
  toJSON(message: ClientToServer_Connect): unknown {
    const obj: any = {};
    message.token !== undefined && (obj.token = message.token);
    return obj;
  },
};

export const ClientToServer_QueryAirport = {
  encode(message: ClientToServer_QueryAirport, writer: Writer = Writer.create()): Writer {
    writer.uint32(10).string(message.icaoCode);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ClientToServer_QueryAirport {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseClientToServer_QueryAirport } as ClientToServer_QueryAirport;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.icaoCode = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ClientToServer_QueryAirport {
    const message = { ...baseClientToServer_QueryAirport } as ClientToServer_QueryAirport;
    if (object.icaoCode !== undefined && object.icaoCode !== null) {
      message.icaoCode = String(object.icaoCode);
    } else {
      message.icaoCode = "";
    }
    return message;
  },
  fromPartial(object: DeepPartial<ClientToServer_QueryAirport>): ClientToServer_QueryAirport {
    const message = { ...baseClientToServer_QueryAirport } as ClientToServer_QueryAirport;
    if (object.icaoCode !== undefined && object.icaoCode !== null) {
      message.icaoCode = object.icaoCode;
    } else {
      message.icaoCode = "";
    }
    return message;
  },
  toJSON(message: ClientToServer_QueryAirport): unknown {
    const obj: any = {};
    message.icaoCode !== undefined && (obj.icaoCode = message.icaoCode);
    return obj;
  },
};

export const ClientToServer_QueryTaxiPath = {
  encode(message: ClientToServer_QueryTaxiPath, writer: Writer = Writer.create()): Writer {
    writer.uint32(10).string(message.airportIcao);
    writer.uint32(18).string(message.aircraftModelIcao);
    if (message.fromPoint !== undefined && message.fromPoint !== undefined) {
      GeoPoint.encode(message.fromPoint, writer.uint32(26).fork()).ldelim();
    }
    if (message.toPoint !== undefined && message.toPoint !== undefined) {
      GeoPoint.encode(message.toPoint, writer.uint32(34).fork()).ldelim();
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ClientToServer_QueryTaxiPath {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseClientToServer_QueryTaxiPath } as ClientToServer_QueryTaxiPath;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.airportIcao = reader.string();
          break;
        case 2:
          message.aircraftModelIcao = reader.string();
          break;
        case 3:
          message.fromPoint = GeoPoint.decode(reader, reader.uint32());
          break;
        case 4:
          message.toPoint = GeoPoint.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ClientToServer_QueryTaxiPath {
    const message = { ...baseClientToServer_QueryTaxiPath } as ClientToServer_QueryTaxiPath;
    if (object.airportIcao !== undefined && object.airportIcao !== null) {
      message.airportIcao = String(object.airportIcao);
    } else {
      message.airportIcao = "";
    }
    if (object.aircraftModelIcao !== undefined && object.aircraftModelIcao !== null) {
      message.aircraftModelIcao = String(object.aircraftModelIcao);
    } else {
      message.aircraftModelIcao = "";
    }
    if (object.fromPoint !== undefined && object.fromPoint !== null) {
      message.fromPoint = GeoPoint.fromJSON(object.fromPoint);
    } else {
      message.fromPoint = undefined;
    }
    if (object.toPoint !== undefined && object.toPoint !== null) {
      message.toPoint = GeoPoint.fromJSON(object.toPoint);
    } else {
      message.toPoint = undefined;
    }
    return message;
  },
  fromPartial(object: DeepPartial<ClientToServer_QueryTaxiPath>): ClientToServer_QueryTaxiPath {
    const message = { ...baseClientToServer_QueryTaxiPath } as ClientToServer_QueryTaxiPath;
    if (object.airportIcao !== undefined && object.airportIcao !== null) {
      message.airportIcao = object.airportIcao;
    } else {
      message.airportIcao = "";
    }
    if (object.aircraftModelIcao !== undefined && object.aircraftModelIcao !== null) {
      message.aircraftModelIcao = object.aircraftModelIcao;
    } else {
      message.aircraftModelIcao = "";
    }
    if (object.fromPoint !== undefined && object.fromPoint !== null) {
      message.fromPoint = GeoPoint.fromPartial(object.fromPoint);
    } else {
      message.fromPoint = undefined;
    }
    if (object.toPoint !== undefined && object.toPoint !== null) {
      message.toPoint = GeoPoint.fromPartial(object.toPoint);
    } else {
      message.toPoint = undefined;
    }
    return message;
  },
  toJSON(message: ClientToServer_QueryTaxiPath): unknown {
    const obj: any = {};
    message.airportIcao !== undefined && (obj.airportIcao = message.airportIcao);
    message.aircraftModelIcao !== undefined && (obj.aircraftModelIcao = message.aircraftModelIcao);
    message.fromPoint !== undefined && (obj.fromPoint = message.fromPoint ? GeoPoint.toJSON(message.fromPoint) : undefined);
    message.toPoint !== undefined && (obj.toPoint = message.toPoint ? GeoPoint.toJSON(message.toPoint) : undefined);
    return obj;
  },
};

export const ClientToServer_CreateAircraft = {
  encode(message: ClientToServer_CreateAircraft, writer: Writer = Writer.create()): Writer {
    if (message.aircraft !== undefined && message.aircraft !== undefined) {
      Aircraft.encode(message.aircraft, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ClientToServer_CreateAircraft {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseClientToServer_CreateAircraft } as ClientToServer_CreateAircraft;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.aircraft = Aircraft.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ClientToServer_CreateAircraft {
    const message = { ...baseClientToServer_CreateAircraft } as ClientToServer_CreateAircraft;
    if (object.aircraft !== undefined && object.aircraft !== null) {
      message.aircraft = Aircraft.fromJSON(object.aircraft);
    } else {
      message.aircraft = undefined;
    }
    return message;
  },
  fromPartial(object: DeepPartial<ClientToServer_CreateAircraft>): ClientToServer_CreateAircraft {
    const message = { ...baseClientToServer_CreateAircraft } as ClientToServer_CreateAircraft;
    if (object.aircraft !== undefined && object.aircraft !== null) {
      message.aircraft = Aircraft.fromPartial(object.aircraft);
    } else {
      message.aircraft = undefined;
    }
    return message;
  },
  toJSON(message: ClientToServer_CreateAircraft): unknown {
    const obj: any = {};
    message.aircraft !== undefined && (obj.aircraft = message.aircraft ? Aircraft.toJSON(message.aircraft) : undefined);
    return obj;
  },
};

export const ClientToServer_UpdateAircraftSituation = {
  encode(message: ClientToServer_UpdateAircraftSituation, writer: Writer = Writer.create()): Writer {
    writer.uint32(8).int32(message.aircraftId);
    if (message.situation !== undefined && message.situation !== undefined) {
      Aircraft_Situation.encode(message.situation, writer.uint32(18).fork()).ldelim();
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ClientToServer_UpdateAircraftSituation {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseClientToServer_UpdateAircraftSituation } as ClientToServer_UpdateAircraftSituation;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.aircraftId = reader.int32();
          break;
        case 2:
          message.situation = Aircraft_Situation.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ClientToServer_UpdateAircraftSituation {
    const message = { ...baseClientToServer_UpdateAircraftSituation } as ClientToServer_UpdateAircraftSituation;
    if (object.aircraftId !== undefined && object.aircraftId !== null) {
      message.aircraftId = Number(object.aircraftId);
    } else {
      message.aircraftId = 0;
    }
    if (object.situation !== undefined && object.situation !== null) {
      message.situation = Aircraft_Situation.fromJSON(object.situation);
    } else {
      message.situation = undefined;
    }
    return message;
  },
  fromPartial(object: DeepPartial<ClientToServer_UpdateAircraftSituation>): ClientToServer_UpdateAircraftSituation {
    const message = { ...baseClientToServer_UpdateAircraftSituation } as ClientToServer_UpdateAircraftSituation;
    if (object.aircraftId !== undefined && object.aircraftId !== null) {
      message.aircraftId = object.aircraftId;
    } else {
      message.aircraftId = 0;
    }
    if (object.situation !== undefined && object.situation !== null) {
      message.situation = Aircraft_Situation.fromPartial(object.situation);
    } else {
      message.situation = undefined;
    }
    return message;
  },
  toJSON(message: ClientToServer_UpdateAircraftSituation): unknown {
    const obj: any = {};
    message.aircraftId !== undefined && (obj.aircraftId = message.aircraftId);
    message.situation !== undefined && (obj.situation = message.situation ? Aircraft_Situation.toJSON(message.situation) : undefined);
    return obj;
  },
};

export const ClientToServer_RemoveAircraft = {
  encode(message: ClientToServer_RemoveAircraft, writer: Writer = Writer.create()): Writer {
    writer.uint32(8).int32(message.aircraftId);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ClientToServer_RemoveAircraft {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseClientToServer_RemoveAircraft } as ClientToServer_RemoveAircraft;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.aircraftId = reader.int32();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ClientToServer_RemoveAircraft {
    const message = { ...baseClientToServer_RemoveAircraft } as ClientToServer_RemoveAircraft;
    if (object.aircraftId !== undefined && object.aircraftId !== null) {
      message.aircraftId = Number(object.aircraftId);
    } else {
      message.aircraftId = 0;
    }
    return message;
  },
  fromPartial(object: DeepPartial<ClientToServer_RemoveAircraft>): ClientToServer_RemoveAircraft {
    const message = { ...baseClientToServer_RemoveAircraft } as ClientToServer_RemoveAircraft;
    if (object.aircraftId !== undefined && object.aircraftId !== null) {
      message.aircraftId = object.aircraftId;
    } else {
      message.aircraftId = 0;
    }
    return message;
  },
  toJSON(message: ClientToServer_RemoveAircraft): unknown {
    const obj: any = {};
    message.aircraftId !== undefined && (obj.aircraftId = message.aircraftId);
    return obj;
  },
};

export const ServerToClient = {
  encode(message: ServerToClient, writer: Writer = Writer.create()): Writer {
    writer.uint32(16).uint64(message.id);
    writer.uint32(24).uint64(message.replyToRequestId);
    if (message.sentAt !== undefined && message.sentAt !== undefined) {
      Timestamp.encode(toTimestamp(message.sentAt), writer.uint32(34).fork()).ldelim();
    }
    if (message.requestSentAt !== undefined && message.requestSentAt !== undefined) {
      Timestamp.encode(toTimestamp(message.requestSentAt), writer.uint32(42).fork()).ldelim();
    }
    if (message.requestReceivedAt !== undefined && message.requestReceivedAt !== undefined) {
      Timestamp.encode(toTimestamp(message.requestReceivedAt), writer.uint32(50).fork()).ldelim();
    }
    if (message.replyConnect !== undefined) {
      ServerToClient_ReplyConnect.encode(message.replyConnect, writer.uint32(8810).fork()).ldelim();
    }
    if (message.replyQueryAirport !== undefined) {
      ServerToClient_ReplyQueryAirport.encode(message.replyQueryAirport, writer.uint32(8818).fork()).ldelim();
    }
    if (message.replyCreateAircraft !== undefined) {
      ServerToClient_ReplyCreateAircraft.encode(message.replyCreateAircraft, writer.uint32(8826).fork()).ldelim();
    }
    if (message.replyQueryTaxiPath !== undefined) {
      ServerToClient_ReplyQueryTaxiPath.encode(message.replyQueryTaxiPath, writer.uint32(8850).fork()).ldelim();
    }
    if (message.notifyAircraftCreated !== undefined) {
      ServerToClient_NotifyAircraftCreated.encode(message.notifyAircraftCreated, writer.uint32(1610).fork()).ldelim();
    }
    if (message.notifyAircraftSituationUpdated !== undefined) {
      ServerToClient_NotifyAircraftSituationUpdated.encode(message.notifyAircraftSituationUpdated, writer.uint32(1618).fork()).ldelim();
    }
    if (message.notifyAircraftRemoved !== undefined) {
      ServerToClient_NotifyAircraftRemoved.encode(message.notifyAircraftRemoved, writer.uint32(1626).fork()).ldelim();
    }
    if (message.faultDeclined !== undefined) {
      ServerToClient_FaultDeclined.encode(message.faultDeclined, writer.uint32(24010).fork()).ldelim();
    }
    if (message.faultNotFound !== undefined) {
      ServerToClient_FaultNotFound.encode(message.faultNotFound, writer.uint32(24018).fork()).ldelim();
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ServerToClient {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseServerToClient } as ServerToClient;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 2:
          message.id = longToNumber(reader.uint64() as Long);
          break;
        case 3:
          message.replyToRequestId = longToNumber(reader.uint64() as Long);
          break;
        case 4:
          message.sentAt = fromTimestamp(Timestamp.decode(reader, reader.uint32()));
          break;
        case 5:
          message.requestSentAt = fromTimestamp(Timestamp.decode(reader, reader.uint32()));
          break;
        case 6:
          message.requestReceivedAt = fromTimestamp(Timestamp.decode(reader, reader.uint32()));
          break;
        case 1101:
          message.replyConnect = ServerToClient_ReplyConnect.decode(reader, reader.uint32());
          break;
        case 1102:
          message.replyQueryAirport = ServerToClient_ReplyQueryAirport.decode(reader, reader.uint32());
          break;
        case 1103:
          message.replyCreateAircraft = ServerToClient_ReplyCreateAircraft.decode(reader, reader.uint32());
          break;
        case 1106:
          message.replyQueryTaxiPath = ServerToClient_ReplyQueryTaxiPath.decode(reader, reader.uint32());
          break;
        case 201:
          message.notifyAircraftCreated = ServerToClient_NotifyAircraftCreated.decode(reader, reader.uint32());
          break;
        case 202:
          message.notifyAircraftSituationUpdated = ServerToClient_NotifyAircraftSituationUpdated.decode(reader, reader.uint32());
          break;
        case 203:
          message.notifyAircraftRemoved = ServerToClient_NotifyAircraftRemoved.decode(reader, reader.uint32());
          break;
        case 3001:
          message.faultDeclined = ServerToClient_FaultDeclined.decode(reader, reader.uint32());
          break;
        case 3002:
          message.faultNotFound = ServerToClient_FaultNotFound.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ServerToClient {
    const message = { ...baseServerToClient } as ServerToClient;
    if (object.id !== undefined && object.id !== null) {
      message.id = Number(object.id);
    } else {
      message.id = 0;
    }
    if (object.replyToRequestId !== undefined && object.replyToRequestId !== null) {
      message.replyToRequestId = Number(object.replyToRequestId);
    } else {
      message.replyToRequestId = 0;
    }
    if (object.sentAt !== undefined && object.sentAt !== null) {
      message.sentAt = fromJsonTimestamp(object.sentAt);
    } else {
      message.sentAt = undefined;
    }
    if (object.requestSentAt !== undefined && object.requestSentAt !== null) {
      message.requestSentAt = fromJsonTimestamp(object.requestSentAt);
    } else {
      message.requestSentAt = undefined;
    }
    if (object.requestReceivedAt !== undefined && object.requestReceivedAt !== null) {
      message.requestReceivedAt = fromJsonTimestamp(object.requestReceivedAt);
    } else {
      message.requestReceivedAt = undefined;
    }
    if (object.replyConnect !== undefined && object.replyConnect !== null) {
      message.replyConnect = ServerToClient_ReplyConnect.fromJSON(object.replyConnect);
    } else {
      message.replyConnect = undefined;
    }
    if (object.replyQueryAirport !== undefined && object.replyQueryAirport !== null) {
      message.replyQueryAirport = ServerToClient_ReplyQueryAirport.fromJSON(object.replyQueryAirport);
    } else {
      message.replyQueryAirport = undefined;
    }
    if (object.replyCreateAircraft !== undefined && object.replyCreateAircraft !== null) {
      message.replyCreateAircraft = ServerToClient_ReplyCreateAircraft.fromJSON(object.replyCreateAircraft);
    } else {
      message.replyCreateAircraft = undefined;
    }
    if (object.replyQueryTaxiPath !== undefined && object.replyQueryTaxiPath !== null) {
      message.replyQueryTaxiPath = ServerToClient_ReplyQueryTaxiPath.fromJSON(object.replyQueryTaxiPath);
    } else {
      message.replyQueryTaxiPath = undefined;
    }
    if (object.notifyAircraftCreated !== undefined && object.notifyAircraftCreated !== null) {
      message.notifyAircraftCreated = ServerToClient_NotifyAircraftCreated.fromJSON(object.notifyAircraftCreated);
    } else {
      message.notifyAircraftCreated = undefined;
    }
    if (object.notifyAircraftSituationUpdated !== undefined && object.notifyAircraftSituationUpdated !== null) {
      message.notifyAircraftSituationUpdated = ServerToClient_NotifyAircraftSituationUpdated.fromJSON(object.notifyAircraftSituationUpdated);
    } else {
      message.notifyAircraftSituationUpdated = undefined;
    }
    if (object.notifyAircraftRemoved !== undefined && object.notifyAircraftRemoved !== null) {
      message.notifyAircraftRemoved = ServerToClient_NotifyAircraftRemoved.fromJSON(object.notifyAircraftRemoved);
    } else {
      message.notifyAircraftRemoved = undefined;
    }
    if (object.faultDeclined !== undefined && object.faultDeclined !== null) {
      message.faultDeclined = ServerToClient_FaultDeclined.fromJSON(object.faultDeclined);
    } else {
      message.faultDeclined = undefined;
    }
    if (object.faultNotFound !== undefined && object.faultNotFound !== null) {
      message.faultNotFound = ServerToClient_FaultNotFound.fromJSON(object.faultNotFound);
    } else {
      message.faultNotFound = undefined;
    }
    return message;
  },
  fromPartial(object: DeepPartial<ServerToClient>): ServerToClient {
    const message = { ...baseServerToClient } as ServerToClient;
    if (object.id !== undefined && object.id !== null) {
      message.id = object.id;
    } else {
      message.id = 0;
    }
    if (object.replyToRequestId !== undefined && object.replyToRequestId !== null) {
      message.replyToRequestId = object.replyToRequestId;
    } else {
      message.replyToRequestId = 0;
    }
    if (object.sentAt !== undefined && object.sentAt !== null) {
      message.sentAt = object.sentAt;
    } else {
      message.sentAt = undefined;
    }
    if (object.requestSentAt !== undefined && object.requestSentAt !== null) {
      message.requestSentAt = object.requestSentAt;
    } else {
      message.requestSentAt = undefined;
    }
    if (object.requestReceivedAt !== undefined && object.requestReceivedAt !== null) {
      message.requestReceivedAt = object.requestReceivedAt;
    } else {
      message.requestReceivedAt = undefined;
    }
    if (object.replyConnect !== undefined && object.replyConnect !== null) {
      message.replyConnect = ServerToClient_ReplyConnect.fromPartial(object.replyConnect);
    } else {
      message.replyConnect = undefined;
    }
    if (object.replyQueryAirport !== undefined && object.replyQueryAirport !== null) {
      message.replyQueryAirport = ServerToClient_ReplyQueryAirport.fromPartial(object.replyQueryAirport);
    } else {
      message.replyQueryAirport = undefined;
    }
    if (object.replyCreateAircraft !== undefined && object.replyCreateAircraft !== null) {
      message.replyCreateAircraft = ServerToClient_ReplyCreateAircraft.fromPartial(object.replyCreateAircraft);
    } else {
      message.replyCreateAircraft = undefined;
    }
    if (object.replyQueryTaxiPath !== undefined && object.replyQueryTaxiPath !== null) {
      message.replyQueryTaxiPath = ServerToClient_ReplyQueryTaxiPath.fromPartial(object.replyQueryTaxiPath);
    } else {
      message.replyQueryTaxiPath = undefined;
    }
    if (object.notifyAircraftCreated !== undefined && object.notifyAircraftCreated !== null) {
      message.notifyAircraftCreated = ServerToClient_NotifyAircraftCreated.fromPartial(object.notifyAircraftCreated);
    } else {
      message.notifyAircraftCreated = undefined;
    }
    if (object.notifyAircraftSituationUpdated !== undefined && object.notifyAircraftSituationUpdated !== null) {
      message.notifyAircraftSituationUpdated = ServerToClient_NotifyAircraftSituationUpdated.fromPartial(object.notifyAircraftSituationUpdated);
    } else {
      message.notifyAircraftSituationUpdated = undefined;
    }
    if (object.notifyAircraftRemoved !== undefined && object.notifyAircraftRemoved !== null) {
      message.notifyAircraftRemoved = ServerToClient_NotifyAircraftRemoved.fromPartial(object.notifyAircraftRemoved);
    } else {
      message.notifyAircraftRemoved = undefined;
    }
    if (object.faultDeclined !== undefined && object.faultDeclined !== null) {
      message.faultDeclined = ServerToClient_FaultDeclined.fromPartial(object.faultDeclined);
    } else {
      message.faultDeclined = undefined;
    }
    if (object.faultNotFound !== undefined && object.faultNotFound !== null) {
      message.faultNotFound = ServerToClient_FaultNotFound.fromPartial(object.faultNotFound);
    } else {
      message.faultNotFound = undefined;
    }
    return message;
  },
  toJSON(message: ServerToClient): unknown {
    const obj: any = {};
    message.id !== undefined && (obj.id = message.id);
    message.replyToRequestId !== undefined && (obj.replyToRequestId = message.replyToRequestId);
    message.sentAt !== undefined && (obj.sentAt = message.sentAt !== undefined ? message.sentAt.toISOString() : null);
    message.requestSentAt !== undefined && (obj.requestSentAt = message.requestSentAt !== undefined ? message.requestSentAt.toISOString() : null);
    message.requestReceivedAt !== undefined && (obj.requestReceivedAt = message.requestReceivedAt !== undefined ? message.requestReceivedAt.toISOString() : null);
    message.replyConnect !== undefined && (obj.replyConnect = message.replyConnect ? ServerToClient_ReplyConnect.toJSON(message.replyConnect) : undefined);
    message.replyQueryAirport !== undefined && (obj.replyQueryAirport = message.replyQueryAirport ? ServerToClient_ReplyQueryAirport.toJSON(message.replyQueryAirport) : undefined);
    message.replyCreateAircraft !== undefined && (obj.replyCreateAircraft = message.replyCreateAircraft ? ServerToClient_ReplyCreateAircraft.toJSON(message.replyCreateAircraft) : undefined);
    message.replyQueryTaxiPath !== undefined && (obj.replyQueryTaxiPath = message.replyQueryTaxiPath ? ServerToClient_ReplyQueryTaxiPath.toJSON(message.replyQueryTaxiPath) : undefined);
    message.notifyAircraftCreated !== undefined && (obj.notifyAircraftCreated = message.notifyAircraftCreated ? ServerToClient_NotifyAircraftCreated.toJSON(message.notifyAircraftCreated) : undefined);
    message.notifyAircraftSituationUpdated !== undefined && (obj.notifyAircraftSituationUpdated = message.notifyAircraftSituationUpdated ? ServerToClient_NotifyAircraftSituationUpdated.toJSON(message.notifyAircraftSituationUpdated) : undefined);
    message.notifyAircraftRemoved !== undefined && (obj.notifyAircraftRemoved = message.notifyAircraftRemoved ? ServerToClient_NotifyAircraftRemoved.toJSON(message.notifyAircraftRemoved) : undefined);
    message.faultDeclined !== undefined && (obj.faultDeclined = message.faultDeclined ? ServerToClient_FaultDeclined.toJSON(message.faultDeclined) : undefined);
    message.faultNotFound !== undefined && (obj.faultNotFound = message.faultNotFound ? ServerToClient_FaultNotFound.toJSON(message.faultNotFound) : undefined);
    return obj;
  },
};

export const ServerToClient_FaultDeclined = {
  encode(message: ServerToClient_FaultDeclined, writer: Writer = Writer.create()): Writer {
    writer.uint32(10).string(message.message);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ServerToClient_FaultDeclined {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseServerToClient_FaultDeclined } as ServerToClient_FaultDeclined;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.message = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ServerToClient_FaultDeclined {
    const message = { ...baseServerToClient_FaultDeclined } as ServerToClient_FaultDeclined;
    if (object.message !== undefined && object.message !== null) {
      message.message = String(object.message);
    } else {
      message.message = "";
    }
    return message;
  },
  fromPartial(object: DeepPartial<ServerToClient_FaultDeclined>): ServerToClient_FaultDeclined {
    const message = { ...baseServerToClient_FaultDeclined } as ServerToClient_FaultDeclined;
    if (object.message !== undefined && object.message !== null) {
      message.message = object.message;
    } else {
      message.message = "";
    }
    return message;
  },
  toJSON(message: ServerToClient_FaultDeclined): unknown {
    const obj: any = {};
    message.message !== undefined && (obj.message = message.message);
    return obj;
  },
};

export const ServerToClient_FaultNotFound = {
  encode(message: ServerToClient_FaultNotFound, writer: Writer = Writer.create()): Writer {
    writer.uint32(10).string(message.message);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ServerToClient_FaultNotFound {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseServerToClient_FaultNotFound } as ServerToClient_FaultNotFound;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.message = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ServerToClient_FaultNotFound {
    const message = { ...baseServerToClient_FaultNotFound } as ServerToClient_FaultNotFound;
    if (object.message !== undefined && object.message !== null) {
      message.message = String(object.message);
    } else {
      message.message = "";
    }
    return message;
  },
  fromPartial(object: DeepPartial<ServerToClient_FaultNotFound>): ServerToClient_FaultNotFound {
    const message = { ...baseServerToClient_FaultNotFound } as ServerToClient_FaultNotFound;
    if (object.message !== undefined && object.message !== null) {
      message.message = object.message;
    } else {
      message.message = "";
    }
    return message;
  },
  toJSON(message: ServerToClient_FaultNotFound): unknown {
    const obj: any = {};
    message.message !== undefined && (obj.message = message.message);
    return obj;
  },
};

export const ServerToClient_ReplyConnect = {
  encode(message: ServerToClient_ReplyConnect, writer: Writer = Writer.create()): Writer {
    writer.uint32(18).string(message.serverBanner);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ServerToClient_ReplyConnect {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseServerToClient_ReplyConnect } as ServerToClient_ReplyConnect;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 2:
          message.serverBanner = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ServerToClient_ReplyConnect {
    const message = { ...baseServerToClient_ReplyConnect } as ServerToClient_ReplyConnect;
    if (object.serverBanner !== undefined && object.serverBanner !== null) {
      message.serverBanner = String(object.serverBanner);
    } else {
      message.serverBanner = "";
    }
    return message;
  },
  fromPartial(object: DeepPartial<ServerToClient_ReplyConnect>): ServerToClient_ReplyConnect {
    const message = { ...baseServerToClient_ReplyConnect } as ServerToClient_ReplyConnect;
    if (object.serverBanner !== undefined && object.serverBanner !== null) {
      message.serverBanner = object.serverBanner;
    } else {
      message.serverBanner = "";
    }
    return message;
  },
  toJSON(message: ServerToClient_ReplyConnect): unknown {
    const obj: any = {};
    message.serverBanner !== undefined && (obj.serverBanner = message.serverBanner);
    return obj;
  },
};

export const ServerToClient_ReplyCreateAircraft = {
  encode(message: ServerToClient_ReplyCreateAircraft, writer: Writer = Writer.create()): Writer {
    writer.uint32(8).int32(message.createdAircraftId);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ServerToClient_ReplyCreateAircraft {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseServerToClient_ReplyCreateAircraft } as ServerToClient_ReplyCreateAircraft;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.createdAircraftId = reader.int32();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ServerToClient_ReplyCreateAircraft {
    const message = { ...baseServerToClient_ReplyCreateAircraft } as ServerToClient_ReplyCreateAircraft;
    if (object.createdAircraftId !== undefined && object.createdAircraftId !== null) {
      message.createdAircraftId = Number(object.createdAircraftId);
    } else {
      message.createdAircraftId = 0;
    }
    return message;
  },
  fromPartial(object: DeepPartial<ServerToClient_ReplyCreateAircraft>): ServerToClient_ReplyCreateAircraft {
    const message = { ...baseServerToClient_ReplyCreateAircraft } as ServerToClient_ReplyCreateAircraft;
    if (object.createdAircraftId !== undefined && object.createdAircraftId !== null) {
      message.createdAircraftId = object.createdAircraftId;
    } else {
      message.createdAircraftId = 0;
    }
    return message;
  },
  toJSON(message: ServerToClient_ReplyCreateAircraft): unknown {
    const obj: any = {};
    message.createdAircraftId !== undefined && (obj.createdAircraftId = message.createdAircraftId);
    return obj;
  },
};

export const ServerToClient_ReplyQueryAirport = {
  encode(message: ServerToClient_ReplyQueryAirport, writer: Writer = Writer.create()): Writer {
    if (message.airport !== undefined && message.airport !== undefined) {
      Airport.encode(message.airport, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ServerToClient_ReplyQueryAirport {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseServerToClient_ReplyQueryAirport } as ServerToClient_ReplyQueryAirport;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.airport = Airport.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ServerToClient_ReplyQueryAirport {
    const message = { ...baseServerToClient_ReplyQueryAirport } as ServerToClient_ReplyQueryAirport;
    if (object.airport !== undefined && object.airport !== null) {
      message.airport = Airport.fromJSON(object.airport);
    } else {
      message.airport = undefined;
    }
    return message;
  },
  fromPartial(object: DeepPartial<ServerToClient_ReplyQueryAirport>): ServerToClient_ReplyQueryAirport {
    const message = { ...baseServerToClient_ReplyQueryAirport } as ServerToClient_ReplyQueryAirport;
    if (object.airport !== undefined && object.airport !== null) {
      message.airport = Airport.fromPartial(object.airport);
    } else {
      message.airport = undefined;
    }
    return message;
  },
  toJSON(message: ServerToClient_ReplyQueryAirport): unknown {
    const obj: any = {};
    message.airport !== undefined && (obj.airport = message.airport ? Airport.toJSON(message.airport) : undefined);
    return obj;
  },
};

export const ServerToClient_ReplyQueryTaxiPath = {
  encode(message: ServerToClient_ReplyQueryTaxiPath, writer: Writer = Writer.create()): Writer {
    writer.uint32(8).bool(message.success);
    if (message.taxiPath !== undefined && message.taxiPath !== undefined) {
      TaxiPath.encode(message.taxiPath, writer.uint32(18).fork()).ldelim();
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ServerToClient_ReplyQueryTaxiPath {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseServerToClient_ReplyQueryTaxiPath } as ServerToClient_ReplyQueryTaxiPath;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.success = reader.bool();
          break;
        case 2:
          message.taxiPath = TaxiPath.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ServerToClient_ReplyQueryTaxiPath {
    const message = { ...baseServerToClient_ReplyQueryTaxiPath } as ServerToClient_ReplyQueryTaxiPath;
    if (object.success !== undefined && object.success !== null) {
      message.success = Boolean(object.success);
    } else {
      message.success = false;
    }
    if (object.taxiPath !== undefined && object.taxiPath !== null) {
      message.taxiPath = TaxiPath.fromJSON(object.taxiPath);
    } else {
      message.taxiPath = undefined;
    }
    return message;
  },
  fromPartial(object: DeepPartial<ServerToClient_ReplyQueryTaxiPath>): ServerToClient_ReplyQueryTaxiPath {
    const message = { ...baseServerToClient_ReplyQueryTaxiPath } as ServerToClient_ReplyQueryTaxiPath;
    if (object.success !== undefined && object.success !== null) {
      message.success = object.success;
    } else {
      message.success = false;
    }
    if (object.taxiPath !== undefined && object.taxiPath !== null) {
      message.taxiPath = TaxiPath.fromPartial(object.taxiPath);
    } else {
      message.taxiPath = undefined;
    }
    return message;
  },
  toJSON(message: ServerToClient_ReplyQueryTaxiPath): unknown {
    const obj: any = {};
    message.success !== undefined && (obj.success = message.success);
    message.taxiPath !== undefined && (obj.taxiPath = message.taxiPath ? TaxiPath.toJSON(message.taxiPath) : undefined);
    return obj;
  },
};

export const ServerToClient_NotifyAircraftCreated = {
  encode(message: ServerToClient_NotifyAircraftCreated, writer: Writer = Writer.create()): Writer {
    if (message.aircraft !== undefined && message.aircraft !== undefined) {
      Aircraft.encode(message.aircraft, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ServerToClient_NotifyAircraftCreated {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseServerToClient_NotifyAircraftCreated } as ServerToClient_NotifyAircraftCreated;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.aircraft = Aircraft.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ServerToClient_NotifyAircraftCreated {
    const message = { ...baseServerToClient_NotifyAircraftCreated } as ServerToClient_NotifyAircraftCreated;
    if (object.aircraft !== undefined && object.aircraft !== null) {
      message.aircraft = Aircraft.fromJSON(object.aircraft);
    } else {
      message.aircraft = undefined;
    }
    return message;
  },
  fromPartial(object: DeepPartial<ServerToClient_NotifyAircraftCreated>): ServerToClient_NotifyAircraftCreated {
    const message = { ...baseServerToClient_NotifyAircraftCreated } as ServerToClient_NotifyAircraftCreated;
    if (object.aircraft !== undefined && object.aircraft !== null) {
      message.aircraft = Aircraft.fromPartial(object.aircraft);
    } else {
      message.aircraft = undefined;
    }
    return message;
  },
  toJSON(message: ServerToClient_NotifyAircraftCreated): unknown {
    const obj: any = {};
    message.aircraft !== undefined && (obj.aircraft = message.aircraft ? Aircraft.toJSON(message.aircraft) : undefined);
    return obj;
  },
};

export const ServerToClient_NotifyAircraftSituationUpdated = {
  encode(message: ServerToClient_NotifyAircraftSituationUpdated, writer: Writer = Writer.create()): Writer {
    writer.uint32(8).int32(message.airctaftId);
    if (message.situation !== undefined && message.situation !== undefined) {
      Aircraft_Situation.encode(message.situation, writer.uint32(18).fork()).ldelim();
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ServerToClient_NotifyAircraftSituationUpdated {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseServerToClient_NotifyAircraftSituationUpdated } as ServerToClient_NotifyAircraftSituationUpdated;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.airctaftId = reader.int32();
          break;
        case 2:
          message.situation = Aircraft_Situation.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ServerToClient_NotifyAircraftSituationUpdated {
    const message = { ...baseServerToClient_NotifyAircraftSituationUpdated } as ServerToClient_NotifyAircraftSituationUpdated;
    if (object.airctaftId !== undefined && object.airctaftId !== null) {
      message.airctaftId = Number(object.airctaftId);
    } else {
      message.airctaftId = 0;
    }
    if (object.situation !== undefined && object.situation !== null) {
      message.situation = Aircraft_Situation.fromJSON(object.situation);
    } else {
      message.situation = undefined;
    }
    return message;
  },
  fromPartial(object: DeepPartial<ServerToClient_NotifyAircraftSituationUpdated>): ServerToClient_NotifyAircraftSituationUpdated {
    const message = { ...baseServerToClient_NotifyAircraftSituationUpdated } as ServerToClient_NotifyAircraftSituationUpdated;
    if (object.airctaftId !== undefined && object.airctaftId !== null) {
      message.airctaftId = object.airctaftId;
    } else {
      message.airctaftId = 0;
    }
    if (object.situation !== undefined && object.situation !== null) {
      message.situation = Aircraft_Situation.fromPartial(object.situation);
    } else {
      message.situation = undefined;
    }
    return message;
  },
  toJSON(message: ServerToClient_NotifyAircraftSituationUpdated): unknown {
    const obj: any = {};
    message.airctaftId !== undefined && (obj.airctaftId = message.airctaftId);
    message.situation !== undefined && (obj.situation = message.situation ? Aircraft_Situation.toJSON(message.situation) : undefined);
    return obj;
  },
};

export const ServerToClient_NotifyAircraftRemoved = {
  encode(message: ServerToClient_NotifyAircraftRemoved, writer: Writer = Writer.create()): Writer {
    writer.uint32(8).int32(message.airctaftId);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ServerToClient_NotifyAircraftRemoved {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseServerToClient_NotifyAircraftRemoved } as ServerToClient_NotifyAircraftRemoved;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.airctaftId = reader.int32();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ServerToClient_NotifyAircraftRemoved {
    const message = { ...baseServerToClient_NotifyAircraftRemoved } as ServerToClient_NotifyAircraftRemoved;
    if (object.airctaftId !== undefined && object.airctaftId !== null) {
      message.airctaftId = Number(object.airctaftId);
    } else {
      message.airctaftId = 0;
    }
    return message;
  },
  fromPartial(object: DeepPartial<ServerToClient_NotifyAircraftRemoved>): ServerToClient_NotifyAircraftRemoved {
    const message = { ...baseServerToClient_NotifyAircraftRemoved } as ServerToClient_NotifyAircraftRemoved;
    if (object.airctaftId !== undefined && object.airctaftId !== null) {
      message.airctaftId = object.airctaftId;
    } else {
      message.airctaftId = 0;
    }
    return message;
  },
  toJSON(message: ServerToClient_NotifyAircraftRemoved): unknown {
    const obj: any = {};
    message.airctaftId !== undefined && (obj.airctaftId = message.airctaftId);
    return obj;
  },
};

export const GeoPoint = {
  encode(message: GeoPoint, writer: Writer = Writer.create()): Writer {
    writer.uint32(9).double(message.lat);
    writer.uint32(17).double(message.lon);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): GeoPoint {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseGeoPoint } as GeoPoint;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.lat = reader.double();
          break;
        case 2:
          message.lon = reader.double();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): GeoPoint {
    const message = { ...baseGeoPoint } as GeoPoint;
    if (object.lat !== undefined && object.lat !== null) {
      message.lat = Number(object.lat);
    } else {
      message.lat = 0;
    }
    if (object.lon !== undefined && object.lon !== null) {
      message.lon = Number(object.lon);
    } else {
      message.lon = 0;
    }
    return message;
  },
  fromPartial(object: DeepPartial<GeoPoint>): GeoPoint {
    const message = { ...baseGeoPoint } as GeoPoint;
    if (object.lat !== undefined && object.lat !== null) {
      message.lat = object.lat;
    } else {
      message.lat = 0;
    }
    if (object.lon !== undefined && object.lon !== null) {
      message.lon = object.lon;
    } else {
      message.lon = 0;
    }
    return message;
  },
  toJSON(message: GeoPoint): unknown {
    const obj: any = {};
    message.lat !== undefined && (obj.lat = message.lat);
    message.lon !== undefined && (obj.lon = message.lon);
    return obj;
  },
};

export const GeoPolygon = {
  encode(message: GeoPolygon, writer: Writer = Writer.create()): Writer {
    for (const v of message.edges) {
      GeoPolygon_GeoEdge.encode(v!, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): GeoPolygon {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseGeoPolygon } as GeoPolygon;
    message.edges = [];
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.edges.push(GeoPolygon_GeoEdge.decode(reader, reader.uint32()));
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): GeoPolygon {
    const message = { ...baseGeoPolygon } as GeoPolygon;
    message.edges = [];
    if (object.edges !== undefined && object.edges !== null) {
      for (const e of object.edges) {
        message.edges.push(GeoPolygon_GeoEdge.fromJSON(e));
      }
    }
    return message;
  },
  fromPartial(object: DeepPartial<GeoPolygon>): GeoPolygon {
    const message = { ...baseGeoPolygon } as GeoPolygon;
    message.edges = [];
    if (object.edges !== undefined && object.edges !== null) {
      for (const e of object.edges) {
        message.edges.push(GeoPolygon_GeoEdge.fromPartial(e));
      }
    }
    return message;
  },
  toJSON(message: GeoPolygon): unknown {
    const obj: any = {};
    if (message.edges) {
      obj.edges = message.edges.map(e => e ? GeoPolygon_GeoEdge.toJSON(e) : undefined);
    } else {
      obj.edges = [];
    }
    return obj;
  },
};

export const GeoPolygon_GeoEdge = {
  encode(message: GeoPolygon_GeoEdge, writer: Writer = Writer.create()): Writer {
    writer.uint32(8).int32(message.type);
    if (message.fromPoint !== undefined && message.fromPoint !== undefined) {
      GeoPoint.encode(message.fromPoint, writer.uint32(18).fork()).ldelim();
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): GeoPolygon_GeoEdge {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseGeoPolygon_GeoEdge } as GeoPolygon_GeoEdge;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.type = reader.int32() as any;
          break;
        case 2:
          message.fromPoint = GeoPoint.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): GeoPolygon_GeoEdge {
    const message = { ...baseGeoPolygon_GeoEdge } as GeoPolygon_GeoEdge;
    if (object.type !== undefined && object.type !== null) {
      message.type = geoEdgeTypeFromJSON(object.type);
    } else {
      message.type = 0;
    }
    if (object.fromPoint !== undefined && object.fromPoint !== null) {
      message.fromPoint = GeoPoint.fromJSON(object.fromPoint);
    } else {
      message.fromPoint = undefined;
    }
    return message;
  },
  fromPartial(object: DeepPartial<GeoPolygon_GeoEdge>): GeoPolygon_GeoEdge {
    const message = { ...baseGeoPolygon_GeoEdge } as GeoPolygon_GeoEdge;
    if (object.type !== undefined && object.type !== null) {
      message.type = object.type;
    } else {
      message.type = 0;
    }
    if (object.fromPoint !== undefined && object.fromPoint !== null) {
      message.fromPoint = GeoPoint.fromPartial(object.fromPoint);
    } else {
      message.fromPoint = undefined;
    }
    return message;
  },
  toJSON(message: GeoPolygon_GeoEdge): unknown {
    const obj: any = {};
    message.type !== undefined && (obj.type = geoEdgeTypeToJSON(message.type));
    message.fromPoint !== undefined && (obj.fromPoint = message.fromPoint ? GeoPoint.toJSON(message.fromPoint) : undefined);
    return obj;
  },
};

export const Vector3d = {
  encode(message: Vector3d, writer: Writer = Writer.create()): Writer {
    writer.uint32(9).double(message.lat);
    writer.uint32(17).double(message.lon);
    writer.uint32(25).double(message.alt);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): Vector3d {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseVector3d } as Vector3d;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.lat = reader.double();
          break;
        case 2:
          message.lon = reader.double();
          break;
        case 3:
          message.alt = reader.double();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): Vector3d {
    const message = { ...baseVector3d } as Vector3d;
    if (object.lat !== undefined && object.lat !== null) {
      message.lat = Number(object.lat);
    } else {
      message.lat = 0;
    }
    if (object.lon !== undefined && object.lon !== null) {
      message.lon = Number(object.lon);
    } else {
      message.lon = 0;
    }
    if (object.alt !== undefined && object.alt !== null) {
      message.alt = Number(object.alt);
    } else {
      message.alt = 0;
    }
    return message;
  },
  fromPartial(object: DeepPartial<Vector3d>): Vector3d {
    const message = { ...baseVector3d } as Vector3d;
    if (object.lat !== undefined && object.lat !== null) {
      message.lat = object.lat;
    } else {
      message.lat = 0;
    }
    if (object.lon !== undefined && object.lon !== null) {
      message.lon = object.lon;
    } else {
      message.lon = 0;
    }
    if (object.alt !== undefined && object.alt !== null) {
      message.alt = object.alt;
    } else {
      message.alt = 0;
    }
    return message;
  },
  toJSON(message: Vector3d): unknown {
    const obj: any = {};
    message.lat !== undefined && (obj.lat = message.lat);
    message.lon !== undefined && (obj.lon = message.lon);
    message.alt !== undefined && (obj.alt = message.alt);
    return obj;
  },
};

export const Attitude = {
  encode(message: Attitude, writer: Writer = Writer.create()): Writer {
    writer.uint32(13).float(message.heading);
    writer.uint32(21).float(message.pitch);
    writer.uint32(29).float(message.roll);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): Attitude {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseAttitude } as Attitude;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.heading = reader.float();
          break;
        case 2:
          message.pitch = reader.float();
          break;
        case 3:
          message.roll = reader.float();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): Attitude {
    const message = { ...baseAttitude } as Attitude;
    if (object.heading !== undefined && object.heading !== null) {
      message.heading = Number(object.heading);
    } else {
      message.heading = 0;
    }
    if (object.pitch !== undefined && object.pitch !== null) {
      message.pitch = Number(object.pitch);
    } else {
      message.pitch = 0;
    }
    if (object.roll !== undefined && object.roll !== null) {
      message.roll = Number(object.roll);
    } else {
      message.roll = 0;
    }
    return message;
  },
  fromPartial(object: DeepPartial<Attitude>): Attitude {
    const message = { ...baseAttitude } as Attitude;
    if (object.heading !== undefined && object.heading !== null) {
      message.heading = object.heading;
    } else {
      message.heading = 0;
    }
    if (object.pitch !== undefined && object.pitch !== null) {
      message.pitch = object.pitch;
    } else {
      message.pitch = 0;
    }
    if (object.roll !== undefined && object.roll !== null) {
      message.roll = object.roll;
    } else {
      message.roll = 0;
    }
    return message;
  },
  toJSON(message: Attitude): unknown {
    const obj: any = {};
    message.heading !== undefined && (obj.heading = message.heading);
    message.pitch !== undefined && (obj.pitch = message.pitch);
    message.roll !== undefined && (obj.roll = message.roll);
    return obj;
  },
};

export const Airport = {
  encode(message: Airport, writer: Writer = Writer.create()): Writer {
    writer.uint32(10).string(message.icao);
    if (message.location !== undefined && message.location !== undefined) {
      GeoPoint.encode(message.location, writer.uint32(18).fork()).ldelim();
    }
    for (const v of message.runways) {
      Runway.encode(v!, writer.uint32(26).fork()).ldelim();
    }
    for (const v of message.parkingStands) {
      ParkingStand.encode(v!, writer.uint32(34).fork()).ldelim();
    }
    for (const v of message.taxiNodes) {
      TaxiNode.encode(v!, writer.uint32(42).fork()).ldelim();
    }
    for (const v of message.taxiEdges) {
      TaxiEdge.encode(v!, writer.uint32(50).fork()).ldelim();
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): Airport {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseAirport } as Airport;
    message.runways = [];
    message.parkingStands = [];
    message.taxiNodes = [];
    message.taxiEdges = [];
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.icao = reader.string();
          break;
        case 2:
          message.location = GeoPoint.decode(reader, reader.uint32());
          break;
        case 3:
          message.runways.push(Runway.decode(reader, reader.uint32()));
          break;
        case 4:
          message.parkingStands.push(ParkingStand.decode(reader, reader.uint32()));
          break;
        case 5:
          message.taxiNodes.push(TaxiNode.decode(reader, reader.uint32()));
          break;
        case 6:
          message.taxiEdges.push(TaxiEdge.decode(reader, reader.uint32()));
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): Airport {
    const message = { ...baseAirport } as Airport;
    message.runways = [];
    message.parkingStands = [];
    message.taxiNodes = [];
    message.taxiEdges = [];
    if (object.icao !== undefined && object.icao !== null) {
      message.icao = String(object.icao);
    } else {
      message.icao = "";
    }
    if (object.location !== undefined && object.location !== null) {
      message.location = GeoPoint.fromJSON(object.location);
    } else {
      message.location = undefined;
    }
    if (object.runways !== undefined && object.runways !== null) {
      for (const e of object.runways) {
        message.runways.push(Runway.fromJSON(e));
      }
    }
    if (object.parkingStands !== undefined && object.parkingStands !== null) {
      for (const e of object.parkingStands) {
        message.parkingStands.push(ParkingStand.fromJSON(e));
      }
    }
    if (object.taxiNodes !== undefined && object.taxiNodes !== null) {
      for (const e of object.taxiNodes) {
        message.taxiNodes.push(TaxiNode.fromJSON(e));
      }
    }
    if (object.taxiEdges !== undefined && object.taxiEdges !== null) {
      for (const e of object.taxiEdges) {
        message.taxiEdges.push(TaxiEdge.fromJSON(e));
      }
    }
    return message;
  },
  fromPartial(object: DeepPartial<Airport>): Airport {
    const message = { ...baseAirport } as Airport;
    message.runways = [];
    message.parkingStands = [];
    message.taxiNodes = [];
    message.taxiEdges = [];
    if (object.icao !== undefined && object.icao !== null) {
      message.icao = object.icao;
    } else {
      message.icao = "";
    }
    if (object.location !== undefined && object.location !== null) {
      message.location = GeoPoint.fromPartial(object.location);
    } else {
      message.location = undefined;
    }
    if (object.runways !== undefined && object.runways !== null) {
      for (const e of object.runways) {
        message.runways.push(Runway.fromPartial(e));
      }
    }
    if (object.parkingStands !== undefined && object.parkingStands !== null) {
      for (const e of object.parkingStands) {
        message.parkingStands.push(ParkingStand.fromPartial(e));
      }
    }
    if (object.taxiNodes !== undefined && object.taxiNodes !== null) {
      for (const e of object.taxiNodes) {
        message.taxiNodes.push(TaxiNode.fromPartial(e));
      }
    }
    if (object.taxiEdges !== undefined && object.taxiEdges !== null) {
      for (const e of object.taxiEdges) {
        message.taxiEdges.push(TaxiEdge.fromPartial(e));
      }
    }
    return message;
  },
  toJSON(message: Airport): unknown {
    const obj: any = {};
    message.icao !== undefined && (obj.icao = message.icao);
    message.location !== undefined && (obj.location = message.location ? GeoPoint.toJSON(message.location) : undefined);
    if (message.runways) {
      obj.runways = message.runways.map(e => e ? Runway.toJSON(e) : undefined);
    } else {
      obj.runways = [];
    }
    if (message.parkingStands) {
      obj.parkingStands = message.parkingStands.map(e => e ? ParkingStand.toJSON(e) : undefined);
    } else {
      obj.parkingStands = [];
    }
    if (message.taxiNodes) {
      obj.taxiNodes = message.taxiNodes.map(e => e ? TaxiNode.toJSON(e) : undefined);
    } else {
      obj.taxiNodes = [];
    }
    if (message.taxiEdges) {
      obj.taxiEdges = message.taxiEdges.map(e => e ? TaxiEdge.toJSON(e) : undefined);
    } else {
      obj.taxiEdges = [];
    }
    return obj;
  },
};

export const Runway = {
  encode(message: Runway, writer: Writer = Writer.create()): Writer {
    writer.uint32(13).float(message.widthMeters);
    writer.uint32(21).float(message.lengthMeters);
    writer.uint32(24).uint32(message.maskBit);
    if (message.end1 !== undefined && message.end1 !== undefined) {
      Runway_End.encode(message.end1, writer.uint32(34).fork()).ldelim();
    }
    if (message.end2 !== undefined && message.end2 !== undefined) {
      Runway_End.encode(message.end2, writer.uint32(42).fork()).ldelim();
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): Runway {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseRunway } as Runway;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.widthMeters = reader.float();
          break;
        case 2:
          message.lengthMeters = reader.float();
          break;
        case 3:
          message.maskBit = reader.uint32();
          break;
        case 4:
          message.end1 = Runway_End.decode(reader, reader.uint32());
          break;
        case 5:
          message.end2 = Runway_End.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): Runway {
    const message = { ...baseRunway } as Runway;
    if (object.widthMeters !== undefined && object.widthMeters !== null) {
      message.widthMeters = Number(object.widthMeters);
    } else {
      message.widthMeters = 0;
    }
    if (object.lengthMeters !== undefined && object.lengthMeters !== null) {
      message.lengthMeters = Number(object.lengthMeters);
    } else {
      message.lengthMeters = 0;
    }
    if (object.maskBit !== undefined && object.maskBit !== null) {
      message.maskBit = Number(object.maskBit);
    } else {
      message.maskBit = 0;
    }
    if (object.end1 !== undefined && object.end1 !== null) {
      message.end1 = Runway_End.fromJSON(object.end1);
    } else {
      message.end1 = undefined;
    }
    if (object.end2 !== undefined && object.end2 !== null) {
      message.end2 = Runway_End.fromJSON(object.end2);
    } else {
      message.end2 = undefined;
    }
    return message;
  },
  fromPartial(object: DeepPartial<Runway>): Runway {
    const message = { ...baseRunway } as Runway;
    if (object.widthMeters !== undefined && object.widthMeters !== null) {
      message.widthMeters = object.widthMeters;
    } else {
      message.widthMeters = 0;
    }
    if (object.lengthMeters !== undefined && object.lengthMeters !== null) {
      message.lengthMeters = object.lengthMeters;
    } else {
      message.lengthMeters = 0;
    }
    if (object.maskBit !== undefined && object.maskBit !== null) {
      message.maskBit = object.maskBit;
    } else {
      message.maskBit = 0;
    }
    if (object.end1 !== undefined && object.end1 !== null) {
      message.end1 = Runway_End.fromPartial(object.end1);
    } else {
      message.end1 = undefined;
    }
    if (object.end2 !== undefined && object.end2 !== null) {
      message.end2 = Runway_End.fromPartial(object.end2);
    } else {
      message.end2 = undefined;
    }
    return message;
  },
  toJSON(message: Runway): unknown {
    const obj: any = {};
    message.widthMeters !== undefined && (obj.widthMeters = message.widthMeters);
    message.lengthMeters !== undefined && (obj.lengthMeters = message.lengthMeters);
    message.maskBit !== undefined && (obj.maskBit = message.maskBit);
    message.end1 !== undefined && (obj.end1 = message.end1 ? Runway_End.toJSON(message.end1) : undefined);
    message.end2 !== undefined && (obj.end2 = message.end2 ? Runway_End.toJSON(message.end2) : undefined);
    return obj;
  },
};

export const Runway_End = {
  encode(message: Runway_End, writer: Writer = Writer.create()): Writer {
    writer.uint32(10).string(message.name);
    writer.uint32(21).float(message.heading);
    if (message.centerlinePoint !== undefined && message.centerlinePoint !== undefined) {
      GeoPoint.encode(message.centerlinePoint, writer.uint32(26).fork()).ldelim();
    }
    writer.uint32(37).float(message.displacedThresholdMeters);
    writer.uint32(45).float(message.overrunAreaMeters);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): Runway_End {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseRunway_End } as Runway_End;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.name = reader.string();
          break;
        case 2:
          message.heading = reader.float();
          break;
        case 3:
          message.centerlinePoint = GeoPoint.decode(reader, reader.uint32());
          break;
        case 4:
          message.displacedThresholdMeters = reader.float();
          break;
        case 5:
          message.overrunAreaMeters = reader.float();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): Runway_End {
    const message = { ...baseRunway_End } as Runway_End;
    if (object.name !== undefined && object.name !== null) {
      message.name = String(object.name);
    } else {
      message.name = "";
    }
    if (object.heading !== undefined && object.heading !== null) {
      message.heading = Number(object.heading);
    } else {
      message.heading = 0;
    }
    if (object.centerlinePoint !== undefined && object.centerlinePoint !== null) {
      message.centerlinePoint = GeoPoint.fromJSON(object.centerlinePoint);
    } else {
      message.centerlinePoint = undefined;
    }
    if (object.displacedThresholdMeters !== undefined && object.displacedThresholdMeters !== null) {
      message.displacedThresholdMeters = Number(object.displacedThresholdMeters);
    } else {
      message.displacedThresholdMeters = 0;
    }
    if (object.overrunAreaMeters !== undefined && object.overrunAreaMeters !== null) {
      message.overrunAreaMeters = Number(object.overrunAreaMeters);
    } else {
      message.overrunAreaMeters = 0;
    }
    return message;
  },
  fromPartial(object: DeepPartial<Runway_End>): Runway_End {
    const message = { ...baseRunway_End } as Runway_End;
    if (object.name !== undefined && object.name !== null) {
      message.name = object.name;
    } else {
      message.name = "";
    }
    if (object.heading !== undefined && object.heading !== null) {
      message.heading = object.heading;
    } else {
      message.heading = 0;
    }
    if (object.centerlinePoint !== undefined && object.centerlinePoint !== null) {
      message.centerlinePoint = GeoPoint.fromPartial(object.centerlinePoint);
    } else {
      message.centerlinePoint = undefined;
    }
    if (object.displacedThresholdMeters !== undefined && object.displacedThresholdMeters !== null) {
      message.displacedThresholdMeters = object.displacedThresholdMeters;
    } else {
      message.displacedThresholdMeters = 0;
    }
    if (object.overrunAreaMeters !== undefined && object.overrunAreaMeters !== null) {
      message.overrunAreaMeters = object.overrunAreaMeters;
    } else {
      message.overrunAreaMeters = 0;
    }
    return message;
  },
  toJSON(message: Runway_End): unknown {
    const obj: any = {};
    message.name !== undefined && (obj.name = message.name);
    message.heading !== undefined && (obj.heading = message.heading);
    message.centerlinePoint !== undefined && (obj.centerlinePoint = message.centerlinePoint ? GeoPoint.toJSON(message.centerlinePoint) : undefined);
    message.displacedThresholdMeters !== undefined && (obj.displacedThresholdMeters = message.displacedThresholdMeters);
    message.overrunAreaMeters !== undefined && (obj.overrunAreaMeters = message.overrunAreaMeters);
    return obj;
  },
};

export const TaxiNode = {
  encode(message: TaxiNode, writer: Writer = Writer.create()): Writer {
    writer.uint32(8).int32(message.id);
    if (message.location !== undefined && message.location !== undefined) {
      GeoPoint.encode(message.location, writer.uint32(18).fork()).ldelim();
    }
    writer.uint32(24).bool(message.isJunction);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): TaxiNode {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseTaxiNode } as TaxiNode;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.id = reader.int32();
          break;
        case 2:
          message.location = GeoPoint.decode(reader, reader.uint32());
          break;
        case 3:
          message.isJunction = reader.bool();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): TaxiNode {
    const message = { ...baseTaxiNode } as TaxiNode;
    if (object.id !== undefined && object.id !== null) {
      message.id = Number(object.id);
    } else {
      message.id = 0;
    }
    if (object.location !== undefined && object.location !== null) {
      message.location = GeoPoint.fromJSON(object.location);
    } else {
      message.location = undefined;
    }
    if (object.isJunction !== undefined && object.isJunction !== null) {
      message.isJunction = Boolean(object.isJunction);
    } else {
      message.isJunction = false;
    }
    return message;
  },
  fromPartial(object: DeepPartial<TaxiNode>): TaxiNode {
    const message = { ...baseTaxiNode } as TaxiNode;
    if (object.id !== undefined && object.id !== null) {
      message.id = object.id;
    } else {
      message.id = 0;
    }
    if (object.location !== undefined && object.location !== null) {
      message.location = GeoPoint.fromPartial(object.location);
    } else {
      message.location = undefined;
    }
    if (object.isJunction !== undefined && object.isJunction !== null) {
      message.isJunction = object.isJunction;
    } else {
      message.isJunction = false;
    }
    return message;
  },
  toJSON(message: TaxiNode): unknown {
    const obj: any = {};
    message.id !== undefined && (obj.id = message.id);
    message.location !== undefined && (obj.location = message.location ? GeoPoint.toJSON(message.location) : undefined);
    message.isJunction !== undefined && (obj.isJunction = message.isJunction);
    return obj;
  },
};

export const TaxiEdge = {
  encode(message: TaxiEdge, writer: Writer = Writer.create()): Writer {
    writer.uint32(8).int32(message.id);
    writer.uint32(18).string(message.name);
    writer.uint32(24).int32(message.nodeId1);
    writer.uint32(32).int32(message.nodeId2);
    writer.uint32(40).int32(message.type);
    writer.uint32(48).bool(message.isOneWay);
    writer.uint32(56).bool(message.isHighSpeedExit);
    writer.uint32(69).float(message.lengthMeters);
    writer.uint32(77).float(message.heading);
    if (message.activeZones !== undefined && message.activeZones !== undefined) {
      TaxiEdge_ActiveZoneMatrix.encode(message.activeZones, writer.uint32(82).fork()).ldelim();
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): TaxiEdge {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseTaxiEdge } as TaxiEdge;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.id = reader.int32();
          break;
        case 2:
          message.name = reader.string();
          break;
        case 3:
          message.nodeId1 = reader.int32();
          break;
        case 4:
          message.nodeId2 = reader.int32();
          break;
        case 5:
          message.type = reader.int32() as any;
          break;
        case 6:
          message.isOneWay = reader.bool();
          break;
        case 7:
          message.isHighSpeedExit = reader.bool();
          break;
        case 8:
          message.lengthMeters = reader.float();
          break;
        case 9:
          message.heading = reader.float();
          break;
        case 10:
          message.activeZones = TaxiEdge_ActiveZoneMatrix.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): TaxiEdge {
    const message = { ...baseTaxiEdge } as TaxiEdge;
    if (object.id !== undefined && object.id !== null) {
      message.id = Number(object.id);
    } else {
      message.id = 0;
    }
    if (object.name !== undefined && object.name !== null) {
      message.name = String(object.name);
    } else {
      message.name = "";
    }
    if (object.nodeId1 !== undefined && object.nodeId1 !== null) {
      message.nodeId1 = Number(object.nodeId1);
    } else {
      message.nodeId1 = 0;
    }
    if (object.nodeId2 !== undefined && object.nodeId2 !== null) {
      message.nodeId2 = Number(object.nodeId2);
    } else {
      message.nodeId2 = 0;
    }
    if (object.type !== undefined && object.type !== null) {
      message.type = taxiEdgeTypeFromJSON(object.type);
    } else {
      message.type = 0;
    }
    if (object.isOneWay !== undefined && object.isOneWay !== null) {
      message.isOneWay = Boolean(object.isOneWay);
    } else {
      message.isOneWay = false;
    }
    if (object.isHighSpeedExit !== undefined && object.isHighSpeedExit !== null) {
      message.isHighSpeedExit = Boolean(object.isHighSpeedExit);
    } else {
      message.isHighSpeedExit = false;
    }
    if (object.lengthMeters !== undefined && object.lengthMeters !== null) {
      message.lengthMeters = Number(object.lengthMeters);
    } else {
      message.lengthMeters = 0;
    }
    if (object.heading !== undefined && object.heading !== null) {
      message.heading = Number(object.heading);
    } else {
      message.heading = 0;
    }
    if (object.activeZones !== undefined && object.activeZones !== null) {
      message.activeZones = TaxiEdge_ActiveZoneMatrix.fromJSON(object.activeZones);
    } else {
      message.activeZones = undefined;
    }
    return message;
  },
  fromPartial(object: DeepPartial<TaxiEdge>): TaxiEdge {
    const message = { ...baseTaxiEdge } as TaxiEdge;
    if (object.id !== undefined && object.id !== null) {
      message.id = object.id;
    } else {
      message.id = 0;
    }
    if (object.name !== undefined && object.name !== null) {
      message.name = object.name;
    } else {
      message.name = "";
    }
    if (object.nodeId1 !== undefined && object.nodeId1 !== null) {
      message.nodeId1 = object.nodeId1;
    } else {
      message.nodeId1 = 0;
    }
    if (object.nodeId2 !== undefined && object.nodeId2 !== null) {
      message.nodeId2 = object.nodeId2;
    } else {
      message.nodeId2 = 0;
    }
    if (object.type !== undefined && object.type !== null) {
      message.type = object.type;
    } else {
      message.type = 0;
    }
    if (object.isOneWay !== undefined && object.isOneWay !== null) {
      message.isOneWay = object.isOneWay;
    } else {
      message.isOneWay = false;
    }
    if (object.isHighSpeedExit !== undefined && object.isHighSpeedExit !== null) {
      message.isHighSpeedExit = object.isHighSpeedExit;
    } else {
      message.isHighSpeedExit = false;
    }
    if (object.lengthMeters !== undefined && object.lengthMeters !== null) {
      message.lengthMeters = object.lengthMeters;
    } else {
      message.lengthMeters = 0;
    }
    if (object.heading !== undefined && object.heading !== null) {
      message.heading = object.heading;
    } else {
      message.heading = 0;
    }
    if (object.activeZones !== undefined && object.activeZones !== null) {
      message.activeZones = TaxiEdge_ActiveZoneMatrix.fromPartial(object.activeZones);
    } else {
      message.activeZones = undefined;
    }
    return message;
  },
  toJSON(message: TaxiEdge): unknown {
    const obj: any = {};
    message.id !== undefined && (obj.id = message.id);
    message.name !== undefined && (obj.name = message.name);
    message.nodeId1 !== undefined && (obj.nodeId1 = message.nodeId1);
    message.nodeId2 !== undefined && (obj.nodeId2 = message.nodeId2);
    message.type !== undefined && (obj.type = taxiEdgeTypeToJSON(message.type));
    message.isOneWay !== undefined && (obj.isOneWay = message.isOneWay);
    message.isHighSpeedExit !== undefined && (obj.isHighSpeedExit = message.isHighSpeedExit);
    message.lengthMeters !== undefined && (obj.lengthMeters = message.lengthMeters);
    message.heading !== undefined && (obj.heading = message.heading);
    message.activeZones !== undefined && (obj.activeZones = message.activeZones ? TaxiEdge_ActiveZoneMatrix.toJSON(message.activeZones) : undefined);
    return obj;
  },
};

export const TaxiEdge_ActiveZoneMatrix = {
  encode(message: TaxiEdge_ActiveZoneMatrix, writer: Writer = Writer.create()): Writer {
    writer.uint32(8).uint64(message.departure);
    writer.uint32(16).uint64(message.arrival);
    writer.uint32(24).uint64(message.ils);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): TaxiEdge_ActiveZoneMatrix {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseTaxiEdge_ActiveZoneMatrix } as TaxiEdge_ActiveZoneMatrix;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.departure = longToNumber(reader.uint64() as Long);
          break;
        case 2:
          message.arrival = longToNumber(reader.uint64() as Long);
          break;
        case 3:
          message.ils = longToNumber(reader.uint64() as Long);
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): TaxiEdge_ActiveZoneMatrix {
    const message = { ...baseTaxiEdge_ActiveZoneMatrix } as TaxiEdge_ActiveZoneMatrix;
    if (object.departure !== undefined && object.departure !== null) {
      message.departure = Number(object.departure);
    } else {
      message.departure = 0;
    }
    if (object.arrival !== undefined && object.arrival !== null) {
      message.arrival = Number(object.arrival);
    } else {
      message.arrival = 0;
    }
    if (object.ils !== undefined && object.ils !== null) {
      message.ils = Number(object.ils);
    } else {
      message.ils = 0;
    }
    return message;
  },
  fromPartial(object: DeepPartial<TaxiEdge_ActiveZoneMatrix>): TaxiEdge_ActiveZoneMatrix {
    const message = { ...baseTaxiEdge_ActiveZoneMatrix } as TaxiEdge_ActiveZoneMatrix;
    if (object.departure !== undefined && object.departure !== null) {
      message.departure = object.departure;
    } else {
      message.departure = 0;
    }
    if (object.arrival !== undefined && object.arrival !== null) {
      message.arrival = object.arrival;
    } else {
      message.arrival = 0;
    }
    if (object.ils !== undefined && object.ils !== null) {
      message.ils = object.ils;
    } else {
      message.ils = 0;
    }
    return message;
  },
  toJSON(message: TaxiEdge_ActiveZoneMatrix): unknown {
    const obj: any = {};
    message.departure !== undefined && (obj.departure = message.departure);
    message.arrival !== undefined && (obj.arrival = message.arrival);
    message.ils !== undefined && (obj.ils = message.ils);
    return obj;
  },
};

export const ParkingStand = {
  encode(message: ParkingStand, writer: Writer = Writer.create()): Writer {
    writer.uint32(8).int32(message.id);
    writer.uint32(18).string(message.name);
    writer.uint32(24).int32(message.type);
    if (message.location !== undefined && message.location !== undefined) {
      GeoPoint.encode(message.location, writer.uint32(34).fork()).ldelim();
    }
    writer.uint32(45).float(message.heading);
    writer.uint32(50).string(message.widthCode);
    writer.uint32(58).fork();
    for (const v of message.categories) {
      writer.int32(v);
    }
    writer.ldelim();
    writer.uint32(66).fork();
    for (const v of message.operationTypes) {
      writer.int32(v);
    }
    writer.ldelim();
    for (const v of message.airlineIcaos) {
      writer.uint32(74).string(v!);
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): ParkingStand {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseParkingStand } as ParkingStand;
    message.categories = [];
    message.operationTypes = [];
    message.airlineIcaos = [];
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.id = reader.int32();
          break;
        case 2:
          message.name = reader.string();
          break;
        case 3:
          message.type = reader.int32() as any;
          break;
        case 4:
          message.location = GeoPoint.decode(reader, reader.uint32());
          break;
        case 5:
          message.heading = reader.float();
          break;
        case 6:
          message.widthCode = reader.string();
          break;
        case 7:
          if ((tag & 7) === 2) {
            const end2 = reader.uint32() + reader.pos;
            while (reader.pos < end2) {
              message.categories.push(reader.int32() as any);
            }
          } else {
            message.categories.push(reader.int32() as any);
          }
          break;
        case 8:
          if ((tag & 7) === 2) {
            const end2 = reader.uint32() + reader.pos;
            while (reader.pos < end2) {
              message.operationTypes.push(reader.int32() as any);
            }
          } else {
            message.operationTypes.push(reader.int32() as any);
          }
          break;
        case 9:
          message.airlineIcaos.push(reader.string());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): ParkingStand {
    const message = { ...baseParkingStand } as ParkingStand;
    message.categories = [];
    message.operationTypes = [];
    message.airlineIcaos = [];
    if (object.id !== undefined && object.id !== null) {
      message.id = Number(object.id);
    } else {
      message.id = 0;
    }
    if (object.name !== undefined && object.name !== null) {
      message.name = String(object.name);
    } else {
      message.name = "";
    }
    if (object.type !== undefined && object.type !== null) {
      message.type = parkingStandTypeFromJSON(object.type);
    } else {
      message.type = 0;
    }
    if (object.location !== undefined && object.location !== null) {
      message.location = GeoPoint.fromJSON(object.location);
    } else {
      message.location = undefined;
    }
    if (object.heading !== undefined && object.heading !== null) {
      message.heading = Number(object.heading);
    } else {
      message.heading = 0;
    }
    if (object.widthCode !== undefined && object.widthCode !== null) {
      message.widthCode = String(object.widthCode);
    } else {
      message.widthCode = "";
    }
    if (object.categories !== undefined && object.categories !== null) {
      for (const e of object.categories) {
        message.categories.push(aircraftCategoryFromJSON(e));
      }
    }
    if (object.operationTypes !== undefined && object.operationTypes !== null) {
      for (const e of object.operationTypes) {
        message.operationTypes.push(operationTypeFromJSON(e));
      }
    }
    if (object.airlineIcaos !== undefined && object.airlineIcaos !== null) {
      for (const e of object.airlineIcaos) {
        message.airlineIcaos.push(String(e));
      }
    }
    return message;
  },
  fromPartial(object: DeepPartial<ParkingStand>): ParkingStand {
    const message = { ...baseParkingStand } as ParkingStand;
    message.categories = [];
    message.operationTypes = [];
    message.airlineIcaos = [];
    if (object.id !== undefined && object.id !== null) {
      message.id = object.id;
    } else {
      message.id = 0;
    }
    if (object.name !== undefined && object.name !== null) {
      message.name = object.name;
    } else {
      message.name = "";
    }
    if (object.type !== undefined && object.type !== null) {
      message.type = object.type;
    } else {
      message.type = 0;
    }
    if (object.location !== undefined && object.location !== null) {
      message.location = GeoPoint.fromPartial(object.location);
    } else {
      message.location = undefined;
    }
    if (object.heading !== undefined && object.heading !== null) {
      message.heading = object.heading;
    } else {
      message.heading = 0;
    }
    if (object.widthCode !== undefined && object.widthCode !== null) {
      message.widthCode = object.widthCode;
    } else {
      message.widthCode = "";
    }
    if (object.categories !== undefined && object.categories !== null) {
      for (const e of object.categories) {
        message.categories.push(e);
      }
    }
    if (object.operationTypes !== undefined && object.operationTypes !== null) {
      for (const e of object.operationTypes) {
        message.operationTypes.push(e);
      }
    }
    if (object.airlineIcaos !== undefined && object.airlineIcaos !== null) {
      for (const e of object.airlineIcaos) {
        message.airlineIcaos.push(e);
      }
    }
    return message;
  },
  toJSON(message: ParkingStand): unknown {
    const obj: any = {};
    message.id !== undefined && (obj.id = message.id);
    message.name !== undefined && (obj.name = message.name);
    message.type !== undefined && (obj.type = parkingStandTypeToJSON(message.type));
    message.location !== undefined && (obj.location = message.location ? GeoPoint.toJSON(message.location) : undefined);
    message.heading !== undefined && (obj.heading = message.heading);
    message.widthCode !== undefined && (obj.widthCode = message.widthCode);
    if (message.categories) {
      obj.categories = message.categories.map(e => aircraftCategoryToJSON(e));
    } else {
      obj.categories = [];
    }
    if (message.operationTypes) {
      obj.operationTypes = message.operationTypes.map(e => operationTypeToJSON(e));
    } else {
      obj.operationTypes = [];
    }
    if (message.airlineIcaos) {
      obj.airlineIcaos = message.airlineIcaos.map(e => e);
    } else {
      obj.airlineIcaos = [];
    }
    return obj;
  },
};

export const AirspaceGeometry = {
  encode(message: AirspaceGeometry, writer: Writer = Writer.create()): Writer {
    if (message.lateralBounds !== undefined && message.lateralBounds !== undefined) {
      GeoPolygon.encode(message.lateralBounds, writer.uint32(10).fork()).ldelim();
    }
    writer.uint32(21).float(message.lowerBoundFeet);
    writer.uint32(29).float(message.upperBoundFeet);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): AirspaceGeometry {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseAirspaceGeometry } as AirspaceGeometry;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.lateralBounds = GeoPolygon.decode(reader, reader.uint32());
          break;
        case 2:
          message.lowerBoundFeet = reader.float();
          break;
        case 3:
          message.upperBoundFeet = reader.float();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): AirspaceGeometry {
    const message = { ...baseAirspaceGeometry } as AirspaceGeometry;
    if (object.lateralBounds !== undefined && object.lateralBounds !== null) {
      message.lateralBounds = GeoPolygon.fromJSON(object.lateralBounds);
    } else {
      message.lateralBounds = undefined;
    }
    if (object.lowerBoundFeet !== undefined && object.lowerBoundFeet !== null) {
      message.lowerBoundFeet = Number(object.lowerBoundFeet);
    } else {
      message.lowerBoundFeet = 0;
    }
    if (object.upperBoundFeet !== undefined && object.upperBoundFeet !== null) {
      message.upperBoundFeet = Number(object.upperBoundFeet);
    } else {
      message.upperBoundFeet = 0;
    }
    return message;
  },
  fromPartial(object: DeepPartial<AirspaceGeometry>): AirspaceGeometry {
    const message = { ...baseAirspaceGeometry } as AirspaceGeometry;
    if (object.lateralBounds !== undefined && object.lateralBounds !== null) {
      message.lateralBounds = GeoPolygon.fromPartial(object.lateralBounds);
    } else {
      message.lateralBounds = undefined;
    }
    if (object.lowerBoundFeet !== undefined && object.lowerBoundFeet !== null) {
      message.lowerBoundFeet = object.lowerBoundFeet;
    } else {
      message.lowerBoundFeet = 0;
    }
    if (object.upperBoundFeet !== undefined && object.upperBoundFeet !== null) {
      message.upperBoundFeet = object.upperBoundFeet;
    } else {
      message.upperBoundFeet = 0;
    }
    return message;
  },
  toJSON(message: AirspaceGeometry): unknown {
    const obj: any = {};
    message.lateralBounds !== undefined && (obj.lateralBounds = message.lateralBounds ? GeoPolygon.toJSON(message.lateralBounds) : undefined);
    message.lowerBoundFeet !== undefined && (obj.lowerBoundFeet = message.lowerBoundFeet);
    message.upperBoundFeet !== undefined && (obj.upperBoundFeet = message.upperBoundFeet);
    return obj;
  },
};

export const Aircraft = {
  encode(message: Aircraft, writer: Writer = Writer.create()): Writer {
    writer.uint32(8).int32(message.id);
    writer.uint32(18).string(message.modelIcao);
    writer.uint32(26).string(message.airlineIcao);
    writer.uint32(34).string(message.tailNo);
    writer.uint32(42).string(message.callSign);
    if (message.situation !== undefined && message.situation !== undefined) {
      Aircraft_Situation.encode(message.situation, writer.uint32(50).fork()).ldelim();
    }
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): Aircraft {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseAircraft } as Aircraft;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.id = reader.int32();
          break;
        case 2:
          message.modelIcao = reader.string();
          break;
        case 3:
          message.airlineIcao = reader.string();
          break;
        case 4:
          message.tailNo = reader.string();
          break;
        case 5:
          message.callSign = reader.string();
          break;
        case 6:
          message.situation = Aircraft_Situation.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): Aircraft {
    const message = { ...baseAircraft } as Aircraft;
    if (object.id !== undefined && object.id !== null) {
      message.id = Number(object.id);
    } else {
      message.id = 0;
    }
    if (object.modelIcao !== undefined && object.modelIcao !== null) {
      message.modelIcao = String(object.modelIcao);
    } else {
      message.modelIcao = "";
    }
    if (object.airlineIcao !== undefined && object.airlineIcao !== null) {
      message.airlineIcao = String(object.airlineIcao);
    } else {
      message.airlineIcao = "";
    }
    if (object.tailNo !== undefined && object.tailNo !== null) {
      message.tailNo = String(object.tailNo);
    } else {
      message.tailNo = "";
    }
    if (object.callSign !== undefined && object.callSign !== null) {
      message.callSign = String(object.callSign);
    } else {
      message.callSign = "";
    }
    if (object.situation !== undefined && object.situation !== null) {
      message.situation = Aircraft_Situation.fromJSON(object.situation);
    } else {
      message.situation = undefined;
    }
    return message;
  },
  fromPartial(object: DeepPartial<Aircraft>): Aircraft {
    const message = { ...baseAircraft } as Aircraft;
    if (object.id !== undefined && object.id !== null) {
      message.id = object.id;
    } else {
      message.id = 0;
    }
    if (object.modelIcao !== undefined && object.modelIcao !== null) {
      message.modelIcao = object.modelIcao;
    } else {
      message.modelIcao = "";
    }
    if (object.airlineIcao !== undefined && object.airlineIcao !== null) {
      message.airlineIcao = object.airlineIcao;
    } else {
      message.airlineIcao = "";
    }
    if (object.tailNo !== undefined && object.tailNo !== null) {
      message.tailNo = object.tailNo;
    } else {
      message.tailNo = "";
    }
    if (object.callSign !== undefined && object.callSign !== null) {
      message.callSign = object.callSign;
    } else {
      message.callSign = "";
    }
    if (object.situation !== undefined && object.situation !== null) {
      message.situation = Aircraft_Situation.fromPartial(object.situation);
    } else {
      message.situation = undefined;
    }
    return message;
  },
  toJSON(message: Aircraft): unknown {
    const obj: any = {};
    message.id !== undefined && (obj.id = message.id);
    message.modelIcao !== undefined && (obj.modelIcao = message.modelIcao);
    message.airlineIcao !== undefined && (obj.airlineIcao = message.airlineIcao);
    message.tailNo !== undefined && (obj.tailNo = message.tailNo);
    message.callSign !== undefined && (obj.callSign = message.callSign);
    message.situation !== undefined && (obj.situation = message.situation ? Aircraft_Situation.toJSON(message.situation) : undefined);
    return obj;
  },
};

export const Aircraft_Situation = {
  encode(message: Aircraft_Situation, writer: Writer = Writer.create()): Writer {
    if (message.location !== undefined && message.location !== undefined) {
      Vector3d.encode(message.location, writer.uint32(10).fork()).ldelim();
    }
    if (message.attitude !== undefined && message.attitude !== undefined) {
      Attitude.encode(message.attitude, writer.uint32(18).fork()).ldelim();
    }
    if (message.velocity !== undefined && message.velocity !== undefined) {
      Vector3d.encode(message.velocity, writer.uint32(26).fork()).ldelim();
    }
    if (message.acceleration !== undefined && message.acceleration !== undefined) {
      Vector3d.encode(message.acceleration, writer.uint32(34).fork()).ldelim();
    }
    writer.uint32(40).bool(message.isOnGround);
    writer.uint32(53).float(message.flapRatio);
    writer.uint32(61).float(message.spoilerRatio);
    writer.uint32(69).float(message.gearRatio);
    writer.uint32(77).float(message.noseWheelAngle);
    writer.uint32(80).bool(message.landingLights);
    writer.uint32(88).bool(message.taxiLights);
    writer.uint32(96).bool(message.strobeLights);
    writer.uint32(104).int32(message.frequencyKhz);
    writer.uint32(114).string(message.squawk);
    writer.uint32(120).bool(message.modeC);
    writer.uint32(128).bool(message.modeS);
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): Aircraft_Situation {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseAircraft_Situation } as Aircraft_Situation;
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.location = Vector3d.decode(reader, reader.uint32());
          break;
        case 2:
          message.attitude = Attitude.decode(reader, reader.uint32());
          break;
        case 3:
          message.velocity = Vector3d.decode(reader, reader.uint32());
          break;
        case 4:
          message.acceleration = Vector3d.decode(reader, reader.uint32());
          break;
        case 5:
          message.isOnGround = reader.bool();
          break;
        case 6:
          message.flapRatio = reader.float();
          break;
        case 7:
          message.spoilerRatio = reader.float();
          break;
        case 8:
          message.gearRatio = reader.float();
          break;
        case 9:
          message.noseWheelAngle = reader.float();
          break;
        case 10:
          message.landingLights = reader.bool();
          break;
        case 11:
          message.taxiLights = reader.bool();
          break;
        case 12:
          message.strobeLights = reader.bool();
          break;
        case 13:
          message.frequencyKhz = reader.int32();
          break;
        case 14:
          message.squawk = reader.string();
          break;
        case 15:
          message.modeC = reader.bool();
          break;
        case 16:
          message.modeS = reader.bool();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): Aircraft_Situation {
    const message = { ...baseAircraft_Situation } as Aircraft_Situation;
    if (object.location !== undefined && object.location !== null) {
      message.location = Vector3d.fromJSON(object.location);
    } else {
      message.location = undefined;
    }
    if (object.attitude !== undefined && object.attitude !== null) {
      message.attitude = Attitude.fromJSON(object.attitude);
    } else {
      message.attitude = undefined;
    }
    if (object.velocity !== undefined && object.velocity !== null) {
      message.velocity = Vector3d.fromJSON(object.velocity);
    } else {
      message.velocity = undefined;
    }
    if (object.acceleration !== undefined && object.acceleration !== null) {
      message.acceleration = Vector3d.fromJSON(object.acceleration);
    } else {
      message.acceleration = undefined;
    }
    if (object.isOnGround !== undefined && object.isOnGround !== null) {
      message.isOnGround = Boolean(object.isOnGround);
    } else {
      message.isOnGround = false;
    }
    if (object.flapRatio !== undefined && object.flapRatio !== null) {
      message.flapRatio = Number(object.flapRatio);
    } else {
      message.flapRatio = 0;
    }
    if (object.spoilerRatio !== undefined && object.spoilerRatio !== null) {
      message.spoilerRatio = Number(object.spoilerRatio);
    } else {
      message.spoilerRatio = 0;
    }
    if (object.gearRatio !== undefined && object.gearRatio !== null) {
      message.gearRatio = Number(object.gearRatio);
    } else {
      message.gearRatio = 0;
    }
    if (object.noseWheelAngle !== undefined && object.noseWheelAngle !== null) {
      message.noseWheelAngle = Number(object.noseWheelAngle);
    } else {
      message.noseWheelAngle = 0;
    }
    if (object.landingLights !== undefined && object.landingLights !== null) {
      message.landingLights = Boolean(object.landingLights);
    } else {
      message.landingLights = false;
    }
    if (object.taxiLights !== undefined && object.taxiLights !== null) {
      message.taxiLights = Boolean(object.taxiLights);
    } else {
      message.taxiLights = false;
    }
    if (object.strobeLights !== undefined && object.strobeLights !== null) {
      message.strobeLights = Boolean(object.strobeLights);
    } else {
      message.strobeLights = false;
    }
    if (object.frequencyKhz !== undefined && object.frequencyKhz !== null) {
      message.frequencyKhz = Number(object.frequencyKhz);
    } else {
      message.frequencyKhz = 0;
    }
    if (object.squawk !== undefined && object.squawk !== null) {
      message.squawk = String(object.squawk);
    } else {
      message.squawk = "";
    }
    if (object.modeC !== undefined && object.modeC !== null) {
      message.modeC = Boolean(object.modeC);
    } else {
      message.modeC = false;
    }
    if (object.modeS !== undefined && object.modeS !== null) {
      message.modeS = Boolean(object.modeS);
    } else {
      message.modeS = false;
    }
    return message;
  },
  fromPartial(object: DeepPartial<Aircraft_Situation>): Aircraft_Situation {
    const message = { ...baseAircraft_Situation } as Aircraft_Situation;
    if (object.location !== undefined && object.location !== null) {
      message.location = Vector3d.fromPartial(object.location);
    } else {
      message.location = undefined;
    }
    if (object.attitude !== undefined && object.attitude !== null) {
      message.attitude = Attitude.fromPartial(object.attitude);
    } else {
      message.attitude = undefined;
    }
    if (object.velocity !== undefined && object.velocity !== null) {
      message.velocity = Vector3d.fromPartial(object.velocity);
    } else {
      message.velocity = undefined;
    }
    if (object.acceleration !== undefined && object.acceleration !== null) {
      message.acceleration = Vector3d.fromPartial(object.acceleration);
    } else {
      message.acceleration = undefined;
    }
    if (object.isOnGround !== undefined && object.isOnGround !== null) {
      message.isOnGround = object.isOnGround;
    } else {
      message.isOnGround = false;
    }
    if (object.flapRatio !== undefined && object.flapRatio !== null) {
      message.flapRatio = object.flapRatio;
    } else {
      message.flapRatio = 0;
    }
    if (object.spoilerRatio !== undefined && object.spoilerRatio !== null) {
      message.spoilerRatio = object.spoilerRatio;
    } else {
      message.spoilerRatio = 0;
    }
    if (object.gearRatio !== undefined && object.gearRatio !== null) {
      message.gearRatio = object.gearRatio;
    } else {
      message.gearRatio = 0;
    }
    if (object.noseWheelAngle !== undefined && object.noseWheelAngle !== null) {
      message.noseWheelAngle = object.noseWheelAngle;
    } else {
      message.noseWheelAngle = 0;
    }
    if (object.landingLights !== undefined && object.landingLights !== null) {
      message.landingLights = object.landingLights;
    } else {
      message.landingLights = false;
    }
    if (object.taxiLights !== undefined && object.taxiLights !== null) {
      message.taxiLights = object.taxiLights;
    } else {
      message.taxiLights = false;
    }
    if (object.strobeLights !== undefined && object.strobeLights !== null) {
      message.strobeLights = object.strobeLights;
    } else {
      message.strobeLights = false;
    }
    if (object.frequencyKhz !== undefined && object.frequencyKhz !== null) {
      message.frequencyKhz = object.frequencyKhz;
    } else {
      message.frequencyKhz = 0;
    }
    if (object.squawk !== undefined && object.squawk !== null) {
      message.squawk = object.squawk;
    } else {
      message.squawk = "";
    }
    if (object.modeC !== undefined && object.modeC !== null) {
      message.modeC = object.modeC;
    } else {
      message.modeC = false;
    }
    if (object.modeS !== undefined && object.modeS !== null) {
      message.modeS = object.modeS;
    } else {
      message.modeS = false;
    }
    return message;
  },
  toJSON(message: Aircraft_Situation): unknown {
    const obj: any = {};
    message.location !== undefined && (obj.location = message.location ? Vector3d.toJSON(message.location) : undefined);
    message.attitude !== undefined && (obj.attitude = message.attitude ? Attitude.toJSON(message.attitude) : undefined);
    message.velocity !== undefined && (obj.velocity = message.velocity ? Vector3d.toJSON(message.velocity) : undefined);
    message.acceleration !== undefined && (obj.acceleration = message.acceleration ? Vector3d.toJSON(message.acceleration) : undefined);
    message.isOnGround !== undefined && (obj.isOnGround = message.isOnGround);
    message.flapRatio !== undefined && (obj.flapRatio = message.flapRatio);
    message.spoilerRatio !== undefined && (obj.spoilerRatio = message.spoilerRatio);
    message.gearRatio !== undefined && (obj.gearRatio = message.gearRatio);
    message.noseWheelAngle !== undefined && (obj.noseWheelAngle = message.noseWheelAngle);
    message.landingLights !== undefined && (obj.landingLights = message.landingLights);
    message.taxiLights !== undefined && (obj.taxiLights = message.taxiLights);
    message.strobeLights !== undefined && (obj.strobeLights = message.strobeLights);
    message.frequencyKhz !== undefined && (obj.frequencyKhz = message.frequencyKhz);
    message.squawk !== undefined && (obj.squawk = message.squawk);
    message.modeC !== undefined && (obj.modeC = message.modeC);
    message.modeS !== undefined && (obj.modeS = message.modeS);
    return obj;
  },
};

export const TaxiPath = {
  encode(message: TaxiPath, writer: Writer = Writer.create()): Writer {
    writer.uint32(8).int32(message.fromNodeId);
    writer.uint32(16).int32(message.toNodeId);
    writer.uint32(26).fork();
    for (const v of message.edgeIds) {
      writer.int32(v);
    }
    writer.ldelim();
    return writer;
  },
  decode(input: Uint8Array | Reader, length?: number): TaxiPath {
    const reader = input instanceof Uint8Array ? new Reader(input) : input;
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = { ...baseTaxiPath } as TaxiPath;
    message.edgeIds = [];
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.fromNodeId = reader.int32();
          break;
        case 2:
          message.toNodeId = reader.int32();
          break;
        case 3:
          if ((tag & 7) === 2) {
            const end2 = reader.uint32() + reader.pos;
            while (reader.pos < end2) {
              message.edgeIds.push(reader.int32());
            }
          } else {
            message.edgeIds.push(reader.int32());
          }
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },
  fromJSON(object: any): TaxiPath {
    const message = { ...baseTaxiPath } as TaxiPath;
    message.edgeIds = [];
    if (object.fromNodeId !== undefined && object.fromNodeId !== null) {
      message.fromNodeId = Number(object.fromNodeId);
    } else {
      message.fromNodeId = 0;
    }
    if (object.toNodeId !== undefined && object.toNodeId !== null) {
      message.toNodeId = Number(object.toNodeId);
    } else {
      message.toNodeId = 0;
    }
    if (object.edgeIds !== undefined && object.edgeIds !== null) {
      for (const e of object.edgeIds) {
        message.edgeIds.push(Number(e));
      }
    }
    return message;
  },
  fromPartial(object: DeepPartial<TaxiPath>): TaxiPath {
    const message = { ...baseTaxiPath } as TaxiPath;
    message.edgeIds = [];
    if (object.fromNodeId !== undefined && object.fromNodeId !== null) {
      message.fromNodeId = object.fromNodeId;
    } else {
      message.fromNodeId = 0;
    }
    if (object.toNodeId !== undefined && object.toNodeId !== null) {
      message.toNodeId = object.toNodeId;
    } else {
      message.toNodeId = 0;
    }
    if (object.edgeIds !== undefined && object.edgeIds !== null) {
      for (const e of object.edgeIds) {
        message.edgeIds.push(e);
      }
    }
    return message;
  },
  toJSON(message: TaxiPath): unknown {
    const obj: any = {};
    message.fromNodeId !== undefined && (obj.fromNodeId = message.fromNodeId);
    message.toNodeId !== undefined && (obj.toNodeId = message.toNodeId);
    if (message.edgeIds) {
      obj.edgeIds = message.edgeIds.map(e => e);
    } else {
      obj.edgeIds = [];
    }
    return obj;
  },
};

if (util.Long !== Long as any) {
  util.Long = Long as any;
  configure();
}

export type Builtin = Date | Function | Uint8Array | string | number | undefined;
export type DeepPartial<T> = T extends Builtin
  ? T
  : T extends Array<infer U>
  ? Array<DeepPartial<U>>
  : T extends ReadonlyArray<infer U>
  ? ReadonlyArray<DeepPartial<U>>
  : T extends {}
  ? { [K in keyof T]?: DeepPartial<T[K]> }
  : Partial<T>;