import { AnyAction, Store } from "@reduxjs/toolkit";
import { LISTENER_ACTION_RECEIVED, LISTENER_ACTION_UPDATED, TraceNode, TraceService } from "../../services/traceService";

export interface TraceViewState {
    nodeById: Record<string, TraceNodeState>;
}

export interface TraceNodeState {
    id: string;
    bigId: BigInt;
    bigParentId: BigInt;
    depth: number;
    isExpandable: boolean;
    isExpanded: boolean;
    version: number;
    childNodeIds: string[] | undefined;
}

const initialState: TraceViewState = {
    nodeById: {
        0: createRootNodeState()
    },
};

export const TraceNodeActionTypes = {
    NODE_ADD: 'traceView/nodeAdd',
    NODE_UPDATE: 'traceView/nodeUpdate',
    NODE_EXPAND: 'traceView/nodeExpand',
    NODE_COLLAPSE: 'traceView/nodeCollapse',
};

export const TraceNodeActions = {
    nodeAdd(node: TraceNode) {
        return {
            type: TraceNodeActionTypes.NODE_ADD,
            node
        };
    },
    nodeUpdate(nodeId: BigInt) {
        return {
            type: TraceNodeActionTypes.NODE_UPDATE,
            nodeId: nodeId.toString()
        };
    },
    nodeExpand(nodeId: string) {
        return {
            type: TraceNodeActionTypes.NODE_EXPAND,
            nodeId
        };
    },
    nodeCollapse(nodeId: string) {
        return {
            type: TraceNodeActionTypes.NODE_COLLAPSE,
            nodeId
        };
    },
};

export function createTraceViewState(traceService: TraceService) {

    const PerActionReducers = {
        
        reduceNodeAdd: (state: TraceViewState, action: ReturnType<typeof TraceNodeActions.nodeAdd>): TraceViewState => {
            const parentNode = state.nodeById[action.node.parentSpanId.toString()];
            if (!parentNode || !parentNode.isExpanded) {
                return state;
            }

            const newNode = createTraceNodeState(action.node, parentNode);

            return {
                ...state,
                nodeById: {
                    ...state.nodeById,
                    [newNode.id]: newNode,
                    [parentNode.id]: {
                        ...parentNode,
                        childNodeIds: parentNode.childNodeIds
                            ? [...parentNode.childNodeIds, newNode.id]
                            : [newNode.id]
                    }
                }
            };
        },
    
        reduceNodeUpdate: (state: TraceViewState, action: ReturnType<typeof TraceNodeActions.nodeUpdate>): TraceViewState => {
            const { nodeId } = action;
            const node = state.nodeById[nodeId];
            if (node) {
                return {
                    ...state,
                    [nodeId]: {
                        ...node,
                        version: node.version + 1
                    }
                };
            }
            return state;
        },
        
        reduceNodeExpand: (state: TraceViewState, action: ReturnType<typeof TraceNodeActions.nodeExpand>): TraceViewState => {
            const { nodeId } = action;
            const node = state.nodeById[nodeId];
            if (!node || node.isExpanded || !node.isExpandable) {
                return state;
            }
            const dataNode = traceService?.getNodeById(BigInt(nodeId));
            const safeChildDataNodes = dataNode.children || [];
            const expandedNode: TraceNodeState = {
                ...node,
                isExpanded: true,
                childNodeIds: safeChildDataNodes.map(dataSubNode => dataSubNode.id.toString())
            }
            let newState: TraceViewState = {
                ...state,
                nodeById: {
                    ...state.nodeById,
                    [nodeId]: expandedNode
                }
            };
            safeChildDataNodes.forEach(childDataNode => {
                newState.nodeById[childDataNode.id.toString()] = createTraceNodeState(childDataNode, expandedNode);
            });
            return newState;
        },
    
        reduceNodeCollapse: (state: TraceViewState, action: ReturnType<typeof TraceNodeActions.nodeCollapse>): TraceViewState => {
            const { nodeId } = action;
            const node = state.nodeById[nodeId];
            
            if (node && node.isExpanded) {
                return {
                    ...state,
                    nodeById: {
                        ...state.nodeById,
                        [nodeId]: {
                            ...node,
                            isExpanded: false,
                            childNodeIds: undefined
                        }
                    }
                };
            }

            return state;
        },
    };

    return {

        reducer: (
            state: TraceViewState = initialState,
            action: AnyAction
        ): TraceViewState => {
            switch (action.type) {
                case TraceNodeActionTypes.NODE_ADD:
                    const nodeAddAction = action as ReturnType<typeof TraceNodeActions.nodeAdd>;
                    const stateAfterNodeAdd = PerActionReducers.reduceNodeAdd(state, nodeAddAction);
                    //console.log('traceViewState.reduceNodeAdd', action, state, stateAfterNodeAdd);
                    return stateAfterNodeAdd;

                case TraceNodeActionTypes.NODE_UPDATE: 
                    const nodeUpdateAction = action as ReturnType<typeof TraceNodeActions.nodeUpdate>;
                    const stateAfterNodeUpdate = PerActionReducers.reduceNodeUpdate(state, nodeUpdateAction);
                    //console.log('traceViewState.reduceNodeUpdate', action, state, stateAfterNodeUpdate);
                    return stateAfterNodeUpdate;

                case TraceNodeActionTypes.NODE_EXPAND: 
                    const nodeExpandAction = action as ReturnType<typeof TraceNodeActions.nodeExpand>;
                    const stateAfterNodeExpand = PerActionReducers.reduceNodeExpand(state, nodeExpandAction);
                    //console.log('traceViewState.reduceNodeExpand', action, state, stateAfterNodeExpand);
                    return stateAfterNodeExpand;

                case TraceNodeActionTypes.NODE_COLLAPSE: 
                    const nodeCollapseAction = action as ReturnType<typeof TraceNodeActions.nodeCollapse>;
                    const stateAfterNodeCollapse = PerActionReducers.reduceNodeCollapse(state, nodeCollapseAction);
                    //console.log('traceViewState.reduceNodeCollapse', action, state, stateAfterNodeCollapse);
                    return stateAfterNodeCollapse;
                
                default:
                    return state;
            }
        },

        startTraceViewUpdates: (store: Store) => {
            traceService.addNodeListener((node, action) => {
                switch (action) {
                    case LISTENER_ACTION_RECEIVED:
                        store.dispatch(TraceNodeActions.nodeAdd(node));
                        break;
                    case LISTENER_ACTION_UPDATED:
                        store.dispatch(TraceNodeActions.nodeUpdate(node.id));
                        break;
                }
            });
        }
    
    }    
}

function createTraceNodeState(data: TraceNode, parentNodeState: TraceNodeState | undefined): TraceNodeState {
    return {
        id: data.id.toString(),
        bigId: data.id,
        bigParentId: data.parentSpanId,
        depth: parentNodeState ? parentNodeState.depth + 1 : 0,
        isExpandable: data.isSpan,
        isExpanded: false,
        version: 1,
        childNodeIds: undefined
    };
}

function createRootNodeState(): TraceNodeState {
    return {
        id: '0',
        bigId: BigInt(0),
        bigParentId: BigInt(0),
        depth: -1,
        isExpandable: true,
        isExpanded: true,
        version: 1,
        childNodeIds: undefined
    };
}
