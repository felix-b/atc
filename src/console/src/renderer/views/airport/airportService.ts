import { readFileSync } from 'fs';
import { World } from '../../../proto';
import store from '../../store';
import { VertexData, EdgeData, LayerData, MarkerData, GraphData } from '../../components/graph/graphData';
import { GraphService } from '../../components/graph/graphService';
import { 
    AIRPORT_GRAPH_LAYER_ID_STATIC, 
    AIRPORT_GRAPH_LAYER_ID_PINPOINT, 
    AIRPORT_MARKER_TYPE_PINPOINT, 
    AIRPORT_GRAPH_ID,
    AIRPORT_GRAPH_LAYER_ID_TAXIPATH,
    AIRPORT_GRAPH_LAYER_ID_AIRCRAFT,
    AIRPORT_MARKER_TYPE_AIRCRAFT
} from './airportGraphData';
import { AirportActions } from './airportState';
import { WorldServiceEndpoint } from '../../endpoints/worldServiceEndpoint';
import { TaxiEdge_Type } from '../../../proto/world';
import { GraphClickEventData } from '../../components/graph/graph';

type NodeByIdMap = Record<string, World.TaxiNode>;
type EdgeByIdMap = Record<string, World.TaxiEdge>;
type RunwayByNameMap = Record<string, World.Runway>;
type RunwayByMaskBitMap = Record<number, World.Runway>;

// lat - 111.2
//

const buildNodeByIdMap = (nodes: World.TaxiNode[]): NodeByIdMap => {
    let map: NodeByIdMap = {};
    nodes.forEach(node => map[node.id] = node);
    return map;
};

const buildEdgeByIdMap = (edges: World.TaxiEdge[]): EdgeByIdMap => {
    let map: EdgeByIdMap = {};
    edges.forEach(edge => map[edge.id] = edge);
    return map;
};

const buildRunwayMaps = (runways: World.Runway[]): [RunwayByNameMap, RunwayByMaskBitMap] => {
    let byNameMap: RunwayByNameMap = {};
    let byMaskBitMap: RunwayByMaskBitMap = {};

    runways.forEach(runway => {
        byNameMap[runway.end1?.name || ''] = runway;
        byNameMap[runway.end2?.name || ''] = runway;
        byMaskBitMap[runway.maskBit] = runway;
    });

    return [byNameMap, byMaskBitMap];
};

const createStaticGraphLayer: (airport: World.Airport) => LayerData = (airport) => {

    const nodeById = buildNodeByIdMap(airport.taxiNodes);
    const [runwayByName, runwayByMasKBit] = buildRunwayMaps(airport.runways);
    const taxiVertices = mapTaxiNodesToVertexProps(airport.taxiNodes);
    const gateVertices = mapGateNodesToVertexProps(airport.parkingStands);
    const edges = mapTaxiEdgesToEdgeProps(airport.taxiEdges, nodeById);

    return {
        id: AIRPORT_GRAPH_LAYER_ID_STATIC,
        title: 'Airport',
        vertices: [
            ...taxiVertices,
            ...gateVertices
        ],
        edges,
        markers: []
    };

    function mapBitmaskToRunways(bitmask: number): World.Runway[] {
        return Array
            .from({length: 32}, (_, index) => index)
            .map(bitIndex => 1 << bitIndex)
            .filter(maskBit => (bitmask & maskBit) === maskBit)
            .map(maskBit => runwayByMasKBit[maskBit])
            .filter(runway => !!runway);
    }

    function mapBitmaskToRunwayNames(bitmask: number): string[] {
        return mapBitmaskToRunways(bitmask).map(
            runway => `${runway.end1?.name}/${runway.end2?.name}`
        );
    }

    function mapTaxiNodesToVertexProps(
        nodes: World.TaxiNode[], 
    ): VertexData[] {
        return nodes.map(node => ({
            id: `t_${node.id}`,
            name: '',
            type: 'taxiNode',
            x: node.location?.lon || 0,
            y: -(node.location?.lat || 0),
            tooltip: `[${node.id}] ${node.location?.lat}, ${node.location?.lon}`,
            sourceRef: {
                type: 'TaxiNode',
                id: node.id
            }
        }));
    };

    function getGateTooltipText(gate: World.ParkingStand): string {
        const text = 
            `[${gate.id}] ${gate.name} [${gate.location?.lat}, ${gate.location?.lon}] ` +
            `[${World.ParkingStand_Type.toJSON(gate.type)}] ` + 
            `AC[${gate.categories.map(World.Aircraft_Category.toJSON).join(',')}] ` +
            `OP[${gate.operationTypes.map(World.Aircraft_OperationType.toJSON).join(',')}] ` + 
            `AL[${gate.airlineIcaos.join(',')}]`;
        return text;
    };

    function mapGateNodesToVertexProps(
        gates: World.ParkingStand[], 
    ): VertexData[] {
        return gates.map(gate => ({
            id: `g_${gate.id}`,
            name: gate.name,
            type: World.ParkingStand_Type.toJSON(gate.type).toLowerCase(),
            x: gate.location?.lon || 0,
            y: -(gate.location?.lat || 0),
            rotateDegrees: gate.heading,
            tooltip: getGateTooltipText(gate),
            sourceRef: {
                type: 'Gate',
                id: gate.id
            }
        }));
    };

    function getEdgeTooltipText(edge: World.TaxiEdge) {

        const getActiveZoneText = (zones: World.TaxiEdge_ActiveZoneMatrix) => {
            return (
                (zones.departure ? ` depart[${mapBitmaskToRunwayNames(zones.departure).join(',')}]` : '') + 
                (zones.arrival ? ` arrive[${mapBitmaskToRunwayNames(zones.arrival).join(',')}]` : '') + 
                (zones.ils ? ` ILS[${mapBitmaskToRunwayNames(zones.ils).join(',')}]` : '')
            );
        };

        return (
            `${edge.name} [${edge.id}: ${edge.nodeId1}->${edge.nodeId2}] ${edge.type}` +
            (edge.isOneWay ? ' [1way]' : '') +
            (edge.activeZones && edge.activeZones != null ? getActiveZoneText(edge.activeZones) : '')
        );
    };

    function mapTaxiEdgesToEdgeProps(
        edges: World.TaxiEdge[], 
        nodeById: NodeByIdMap, 
    ): EdgeData[] {
        const isActiveZone = ({activeZones}: World.TaxiEdge) => {
            return (
                activeZones && 
                ((activeZones.arrival | activeZones.departure | activeZones.ils) !== 0));
        }

        return edges.map(edge => {
            const node1 = nodeById[edge.nodeId1];
            const node2 = nodeById[edge.nodeId2];
            return {
                id: `t_${edge.id}`,
                name: edge.name,
                type: `${TaxiEdge_Type.toJSON(edge.type).toLowerCase()}${(isActiveZone(edge) ? ' active' : '')}`,
                x1: node1.location?.lon || 0,
                y1: -(node1.location?.lat || 0),
                x2: node2.location?.lon || 0,
                y2: -(node2.location?.lat || 0),
                tooltip: getEdgeTooltipText(edge),
                sourceRef: {
                    type: 'TaxiEdge',
                    id: edge.id
                }
            };
        });
    };
};


const createPinpointGraphLayer = (point?: { lat: number, lon: number }): LayerData => {
    const markers: MarkerData[] = point 
        ?   [{
                id: 'pinpoint',
                type: AIRPORT_MARKER_TYPE_PINPOINT,
                x: point.lon,
                y: -point.lat
            }]
        :   [];
    
    return {
        id: AIRPORT_GRAPH_LAYER_ID_PINPOINT,
        title: 'Pinpoint',
        vertices: [],
        edges: [],
        markers
    };
};

const createAircraftGraphLayer = (airport: World.Airport): LayerData => {
    // const gateT420 = airport.parkingStands.find(p => p.name == 'T4 20');
    // if (!gateT420) {
    //     throw new Error('Gate T4 20 not found');
    // }
    // const aircraft1: MarkerData = {
    //     type: AIRPORT_MARKER_TYPE_AIRCRAFT,
    //     id: 'aircraft/1',
    //     x: gateT420.location?.lon || 0,
    //     y: -(gateT420.location?.lat || 0),
    //     rotateDegrees: gateT420.heading,
    //     tooltip: `Test A/C at ${gateT420.location?.lat}, ${gateT420.location?.lon}`,
    //     sourceRef: {
    //         type: 'Aircraft',
    //         id: '1'
    //     }
    // };

    return {
        id: AIRPORT_GRAPH_LAYER_ID_AIRCRAFT,
        title: 'Pinpoint',
        vertices: [],
        edges: [],
        markers: [
            //aircraft1
        ]
    };
};

const mapTaxiPathGraphEdges = (airport: World.Airport, taxiPath: World.TaxiPath): EdgeData[] => {
    const nodeById = buildNodeByIdMap(airport.taxiNodes);
    const edgeById = buildEdgeByIdMap(airport.taxiEdges);
    
    const mapTaxiPathEdge = (id: number): EdgeData => {
        const edge = edgeById[id];
        if (!edge) {
            throw new Error(`Taxi path edge not found: id ${id}`);
        }
        const node1 = nodeById[edge.nodeId1];
        const node2 = nodeById[edge.nodeId2];
        const data: EdgeData = {
            id: `t_${id}`,
            name: edge.name,
            type: 'taxi-path',
            tooltip: null,
            x1: (node1.location?.lon || 0),
            y1: -(node1.location?.lat || 0),
            x2: (node2.location?.lon || 0),
            y2: -(node2.location?.lat || 0),
        };
        return data;
    };

    return taxiPath.edgeIds.map(mapTaxiPathEdge);
};

const createTaxiPathGraphLayer = (airport: World.Airport, taxiPath?: World.TaxiPath): LayerData => {
    const edges = taxiPath 
        ? mapTaxiPathGraphEdges(airport, taxiPath)
        : [];
    
    return {
        id: AIRPORT_GRAPH_LAYER_ID_TAXIPATH,
        title: 'Pinpoint',
        vertices: [],
        edges,
        markers: []
    };
};

const loadAirport = (airport: World.Airport) => {
    const taxiPathLayer = createTaxiPathGraphLayer(airport);
    const pinpointLayer = createPinpointGraphLayer();
    const staticLayer = createStaticGraphLayer(airport);
    const aircraftLayer = createAircraftGraphLayer(airport);

    store.dispatch(AirportActions.airportLoaded(airport));

    GraphService.setGraphData({
        id: AIRPORT_GRAPH_ID,
        title: 'Airport',
        layers: [
            taxiPathLayer,
            pinpointLayer,
            staticLayer,
            aircraftLayer
        ]
    });
};

const loadTaxiPath = (taxiPath: World.TaxiPath) => {
    const airport = store.getState()?.airport?.static;
    if (!airport) {
        throw new Error('Airport static data unavailable');
    }

    store.dispatch(AirportActions.taxiPathLoaded(taxiPath));

    const taxiPathLayer = createTaxiPathGraphLayer(airport, taxiPath);
    GraphService.setGraphDataLayer(AIRPORT_GRAPH_ID, taxiPathLayer);
};

function createAirportService() {
    WorldServiceEndpoint.onMessage('replyQueryAirport', ({replyQueryAirport}) => {
        replyQueryAirport?.airport && loadAirport(replyQueryAirport.airport);
    });

    WorldServiceEndpoint.onMessage('replyQueryTaxiPath', ({replyQueryTaxiPath}) => {
        replyQueryTaxiPath?.taxiPath && loadTaxiPath(replyQueryTaxiPath.taxiPath);
    });

    return {
        beginQueryAirport(icaoCode: string) {
            WorldServiceEndpoint.sendMessage({
                queryAirport: {
                    icaoCode
                }
            });
        },

        beginQueryTaxiPath(fromPoint: {lat: number, lon: number}, toPoint: {lat: number, lon: number}) {
            const airport = getAirportOrThrow();
            
            WorldServiceEndpoint.sendMessage({
                queryTaxiPath: {
                    airportIcao: airport.icao,
                    aircraftModelIcao: 'B738', //TODO: receive from caller,
                    fromPoint,
                    toPoint
                }
            });
        },

        notifyAirportElementClicked(event: GraphClickEventData) {
            const taxiTool = store.getState()?.airport.taxiTool;
            if (!taxiTool) {
                return;
            }

            const point = findPointFromClick(event);
            const action = taxiTool.pickingTo 
                ? AirportActions.taxiToolAssign({ 
                    toPoint: point, 
                    toLabel: '',
                    pickingFrom: false,
                    pickingTo: false
                })
                : AirportActions.taxiToolAssign({ 
                    fromPoint: point, 
                    fromLabel: '', 
                    pickingFrom: false, 
                    pickingTo: true 
                });
            store.dispatch(action);
        },

        setPinpoint(lat: number, lon: number) {
            GraphService.setGraphDataLayer(
                AIRPORT_GRAPH_ID, 
                createPinpointGraphLayer({ lat, lon }));
        }
    };

    function getAirportOrThrow(): World.Airport {
        const airport = store.getState()?.airport.static;
        if (!airport) {
            throw new Error('Cannot query taxi path, no airport loaded');
        }
        return airport;
    }

    function findPointFromClick(event: GraphClickEventData): World.GeoPoint {
        const airport = getAirportOrThrow();
        const { sourceRef } = event;
        
        if (sourceRef) {
            switch (sourceRef.type) {
                case 'Gate':
                    const gateId = parseInt(sourceRef.id as string);
                    const gate = airport.parkingStands.find(p => p.id === gateId);
                    if (gate && gate.location) {
                        return gate.location;
                    }
                    break;
                case 'TaxiNode':
                    const nodeId = parseInt(sourceRef.id as string);
                    const node = airport.taxiNodes.find(n => n.id === nodeId);
                    if (node && node.location) {
                        return node.location;
                    }
                    break;
            }
        }

        return { 
            lat: -(event.dataY || 0),
            lon: event.dataX || 0
        };
    };
}

export const AirportService = createAirportService();
