import { GraphData, LayerData, VertexData, EdgeData, MarkerData } from "./graphData";
import store from '../../store';
import { GraphActions, ViewPort } from "./graphState";

const translateX = (x: number, viewPort: ViewPort): number => {
    return (x - viewPort.left) * viewPort.scale;
};

const translateY = (y: number, viewPort: ViewPort): number => {
    return (y - viewPort.top) * viewPort.scale;
};

const translateVertex = (vertex: VertexData, viewPort: ViewPort): VertexData => {
    return {
        ...vertex,
        x: translateX(vertex.x, viewPort),
        y: translateY(vertex.y, viewPort),
    };
};

const translateEdge = (edge: EdgeData, viewPort: ViewPort): EdgeData => {
    return {
        ...edge,
        x1: translateX(edge.x1, viewPort),
        y1: translateY(edge.y1, viewPort),
        x2: translateX(edge.x2, viewPort),
        y2: translateY(edge.y2, viewPort),
    };
};

const translateMarker = (marker: MarkerData, viewPort: ViewPort): MarkerData => {
    return {
        ...marker,
        x: translateX(marker.x, viewPort),
        y: translateY(marker.y, viewPort),
    };
};

const translateLayer = (layer: LayerData, viewPort: ViewPort): LayerData => {
    return {
        ...layer,
        vertices: layer.vertices.map(vertex => translateVertex(vertex, viewPort)),
        edges: layer.edges.map(edge => translateEdge(edge, viewPort)),
        markers: layer.markers.map(marker => translateMarker(marker, viewPort)),
    };
};

const translateGraph = (graph: GraphData, viewPort: ViewPort): GraphData => {
    return {
        ...graph,
        layers: graph.layers.map(layer => translateLayer(layer, viewPort)),
    };
};

const calculateGraphViewPort = (layers: LayerData[]): ViewPort => {
    const allVerticeArrays = layers.map(l => l.vertices);
    const allVertices = ([] as VertexData[]).concat(...allVerticeArrays);
    const allXes = allVertices.map(v => v.x);
    const allYs = allVertices.map(v => v.y);
    const minY = Math.min(...allYs);
    const minX = Math.min(...allXes);
    const maxX = Math.max(...allXes);
    const width = (maxX - minX);
    const scale = width >= 1000 && width <= 2000 
        ? 1.0 
        : 2000 / width;
    
    return {
        top: minY,
        left: minX,
        scale,
    };
};

export const GraphService = {
    setGraphData(graph: GraphData) {
        const viewPort = calculateGraphViewPort(graph.layers);
        const translatedGraph = translateGraph(graph, viewPort);
        store.dispatch(GraphActions.setGraphData(translatedGraph, viewPort));
    },
    setGraphDataLayer(graphId: string, layer: LayerData) {
        const graphEntry = store.getState()!.graph.graphById[graphId];
        const { viewPort } = graphEntry;
        const translatedLayer = translateLayer(layer, viewPort);
        store.dispatch(GraphActions.setGraphLayer(graphId, translatedLayer));
    },
};
