import { Action, AnyAction, Reducer } from "redux";
import { GraphData, LayerData } from "./graphData";

export interface GraphStoreState {
    graphById: Record<string, GraphStoreEntry>;
}

export interface GraphStoreEntry {
    id: string;
    data: GraphData;
    viewPort: ViewPort;
    zoom: number;
}

export interface ViewPort {
    top: number;
    left: number;
    scale: number;
}

export interface SetGraphDataAction extends Action {
    type: 'graph/setData';
    graph: GraphData;
    viewPort: ViewPort;
}

export interface SetGraphLayerAction extends Action {
    type: 'graph/setLayer';
    graphId: string;
    layer: LayerData;
}

export interface SetZoomAction extends Action {
    type: 'graph/setZoom';
    graphId: string;
    zoom: number;
}

export const GraphActions = {
    setGraphData(graph: GraphData, viewPort: ViewPort): SetGraphDataAction {
        return ({
            type: 'graph/setData',
            graph,
            viewPort
        });
    },
    setGraphLayer(graphId: string, layer: LayerData): SetGraphLayerAction {
        return ({
            type: 'graph/setLayer',
            graphId,
            layer
        });
    },
    setZoom(graphId: string, zoom: number): SetZoomAction {
        return ({
            type: 'graph/setZoom',
            graphId,
            zoom
        });
    },
}

const defaultStoreState: GraphStoreState = {
    graphById: {}
};

export const graphReducer: Reducer<GraphStoreState> = (state = defaultStoreState, action: AnyAction): GraphStoreState => {
    let existingEntry: GraphStoreEntry;
    switch (action.type) {
        case 'graph/setData': {
            const dataAction = action as SetGraphDataAction;
            return {
                ...state,
                graphById: {
                    ...state.graphById,
                    [dataAction.graph.id]: {
                        id: dataAction.graph.id,
                        data: dataAction.graph,
                        viewPort: dataAction.viewPort,
                        zoom: 1.0
                    },
                }
            };
        }
        case 'graph/setLayer': {
            const layerAction = action as SetGraphLayerAction;
            const existingEntry = state.graphById[layerAction.graphId];
            return {
                ...state,
                graphById: {
                    ...state.graphById,
                    [layerAction.graphId]: {
                        ...existingEntry,
                        data: {
                            ...existingEntry.data,
                            layers: [
                                ...existingEntry.data.layers.filter(l => l.id !== layerAction.layer.id),
                                layerAction.layer                   
                            ]
                        }
                    }
                }
            };
        }
        case 'graph/setZoom': {
            const zoomAction = action as SetZoomAction;
            const existingEntry = state.graphById[zoomAction.graphId];
            return {
                ...state,
                graphById: {
                    ...state.graphById,
                    [zoomAction.graphId]: {
                        ...existingEntry,
                        zoom: zoomAction.zoom
                    }
                }
            };
        }
        default:
            return state;
    }
}


