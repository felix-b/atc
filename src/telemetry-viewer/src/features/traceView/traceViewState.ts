import { AnyAction, bindActionCreators, Store } from "@reduxjs/toolkit";
import { trace } from "console";
import { LISTENER_ACTION_RECEIVED, LISTENER_ACTION_UPDATED, TraceNode, TraceNodeListener, TraceQuery, TraceQueryResults, TraceService, TraceTreeLayer } from "../../services/types";

export interface RootState {
    traceView: TraceViewState;
}

export interface TraceViewState {
    nodeById: Record<string, TraceNodeState>;
    selectedNodeId?: string;
    queries: Record<string, TraceQueryResults>;
    isFilterActive: boolean;
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
    queries: { },
    isFilterActive: false,
};

export const TraceViewActionTypes = {
    NODE_ADD: 'traceView/nodeAdd',
    NODE_UPDATE: 'traceView/nodeUpdate',
    NODE_EXPAND: 'traceView/nodeExpand',
    NODE_COLLAPSE: 'traceView/nodeCollapse',
    NODE_SELECT: 'traceView/nodeSelect',
    NODE_GOTO: 'traceView/nodeGoto',
    VIEW_CHANGE_NOTIFY: 'traceView/changeNotify',
    FILTER_SWITCH: 'traceView/filterSwitch',
    QUERY_RESULT_SET: 'traceView/queryResultSet',
    QUERY_RESULT_CLEAR: 'traceView/queryResultClear',
    QUERY_RESULT_GOTO: 'traceView/queryResultGoto',
    
    getAllActionTypes: () => [
        'traceView/nodeAdd',
        'traceView/nodeUpdate',
        'traceView/nodeExpand',
        'traceView/nodeCollapse',
        'traceView/nodeSelect',
        'traceView/nodeGoto',
        'traceView/changeNotify',
        'traceView/filterSwitch',
        'traceView/queryResultSet',
        'traceView/queryResultClear',
        'traceView/queryResultGoto',
    ]
};

export const TraceViewActions = {
    nodeAdd(node: TraceNode) {
        return {
            type: TraceViewActionTypes.NODE_ADD,
            node
        };
    },
    nodeUpdate(nodeId: BigInt) {
        return {
            type: TraceViewActionTypes.NODE_UPDATE,
            nodeId: nodeId.toString()
        };
    },
    nodeExpand(nodeId: string) {
        return {
            type: TraceViewActionTypes.NODE_EXPAND,
            nodeId
        };
    },
    nodeCollapse(nodeId: string) {
        return {
            type: TraceViewActionTypes.NODE_COLLAPSE,
            nodeId
        };
    },
    nodeSelect(nodeId: string) {
        return {
            type: TraceViewActionTypes.NODE_SELECT,
            nodeId
        };
    },
    nodeGoto(nodeId: string) {
        return {
            type: TraceViewActionTypes.NODE_GOTO,
            nodeId
        };
    },
    viewChangeNotify() {
        return {
            type: TraceViewActionTypes.VIEW_CHANGE_NOTIFY,
        };
    },
    filterSwitch(switchOn: boolean) {
        return {
            type: TraceViewActionTypes.FILTER_SWITCH,
            switchOn
        };
    },
    queryResultSet(results: TraceQueryResults) {
        return {
            type: TraceViewActionTypes.QUERY_RESULT_SET,
            results,
        };
    },
    queryResultClear(id: number) {
        return {
            type: TraceViewActionTypes.QUERY_RESULT_CLEAR,
            id,
        };
    },
    queryResultGoto(resultId: number, resultIndex: number) {
        return {
            type: TraceViewActionTypes.QUERY_RESULT_GOTO,
            resultId,
            resultIndex,
        };
    },
};

export function createTraceViewState(traceService: TraceService) {

    const PerActionReducers: Record<string, (state: TraceViewState, action: any) => TraceViewState> = {
        
        [TraceViewActionTypes.NODE_ADD]: 
        (state: TraceViewState, action: ReturnType<typeof TraceViewActions.nodeAdd>): TraceViewState => {
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
    
        [TraceViewActionTypes.NODE_UPDATE]: 
        (state: TraceViewState, action: ReturnType<typeof TraceViewActions.nodeUpdate>): TraceViewState => {
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
        
        [TraceViewActionTypes.NODE_EXPAND]: 
        (state: TraceViewState, action: ReturnType<typeof TraceViewActions.nodeExpand>): TraceViewState => {
            const { nodeId } = action;
            const node = state.nodeById[nodeId];
            if (!node || node.isExpanded || !node.isExpandable) {
                return state;
            }
            const dataNode = traceService?.getCurrentView().getNodeById(BigInt(nodeId));
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
    
        [TraceViewActionTypes.NODE_COLLAPSE]: 
        (state: TraceViewState, action: ReturnType<typeof TraceViewActions.nodeCollapse>): TraceViewState => {
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

        [TraceViewActionTypes.NODE_SELECT]: 
        (state: TraceViewState, action: ReturnType<typeof TraceViewActions.nodeSelect>): TraceViewState => {
            const { nodeId } = action;

            return {
                ...state,
                selectedNodeId: state.selectedNodeId === nodeId
                    ? undefined 
                    : nodeId
            };
        },

        [TraceViewActionTypes.VIEW_CHANGE_NOTIFY]: 
        (state: TraceViewState): TraceViewState => {
            const topLevelNodes = traceService.getCurrentView().getTopLevelNodes();
            const rootNodeState = {
                ...createRootNodeState(),
                childNodeIds: topLevelNodes.map(node => node.id.toString())
            };
            let newNodeById: Record<string, TraceNodeState> = {
                ['0']: rootNodeState
            };
            const topLevelNodeStates = topLevelNodes.map(node => createTraceNodeState(node, rootNodeState));
            topLevelNodeStates.forEach(nodeState => {
                newNodeById[nodeState.id] = nodeState;
            });
            return {
                ...state,
                nodeById: newNodeById,
                selectedNodeId: undefined
            };
        },

        [TraceViewActionTypes.FILTER_SWITCH]: 
        (state: TraceViewState, action: ReturnType<typeof TraceViewActions.filterSwitch>): TraceViewState => {
            return {
                ...state,
                isFilterActive: action.switchOn
            };
        },

        [TraceViewActionTypes.QUERY_RESULT_SET]: 
        (state: TraceViewState, action: ReturnType<typeof TraceViewActions.queryResultSet>): TraceViewState => {
            const existingResults = state.queries[action.results.id];
            return {
                ...state,
                queries: {
                    ...state.queries,
                    [action.results.id.toString()]: {
                        ...action.results,
                        resultNodeIds: [...action.results.resultNodeIds],
                        resultIndex: existingResults?.resultIndex || -1,
                    },
                },
            };
        },

        [TraceViewActionTypes.QUERY_RESULT_CLEAR]: 
        (state: TraceViewState, action: ReturnType<typeof TraceViewActions.queryResultClear>): TraceViewState => {
            const newQueries = {...state.queries};
            delete newQueries[action.id.toString()];
            
            return {
                ...state,
                queries: newQueries,
            };
        },

        [TraceViewActionTypes.NODE_GOTO]: 
        (state: TraceViewState, action: ReturnType<typeof TraceViewActions.nodeGoto>): TraceViewState => {
            const destinationNodeId = action.nodeId;
            const destinationDataNode = traceService.getCurrentView().getNodeById(BigInt(destinationNodeId));
            const ancestorNodeIdsBottomUp: string[] = [];

            for (
                let dataNode = traceService.getCurrentView().getNodeById(destinationDataNode.parentSpanId) ; 
                dataNode.id !== BigInt(0) ; 
                dataNode = traceService.getCurrentView().getNodeById(dataNode.parentSpanId))
            {
                ancestorNodeIdsBottomUp.push(dataNode.id.toString());
            }

            let nextState = state;
            const expandReducer = PerActionReducers[TraceViewActionTypes.NODE_EXPAND];
            
            for (let i = ancestorNodeIdsBottomUp.length - 1 ; i >= 0 ; i--) {
                nextState = expandReducer(nextState, TraceViewActions.nodeExpand(ancestorNodeIdsBottomUp[i]));
            }

            return {
                ...nextState,
                selectedNodeId: destinationNodeId,
            }
        },

        [TraceViewActionTypes.QUERY_RESULT_GOTO]: 
        (state: TraceViewState, action: ReturnType<typeof TraceViewActions.queryResultGoto>): TraceViewState => {
            const queryResult = state.queries ? state.queries[action.resultId] : undefined;
            if (!queryResult) {
                return state;
            }

            const resultNodeId = queryResult.resultNodeIds[action.resultIndex];

            const gotoReducer = PerActionReducers[TraceViewActionTypes.NODE_GOTO];
            const nextState = gotoReducer(state, TraceViewActions.nodeGoto(resultNodeId));

            return {
                ...nextState,
                selectedNodeId: resultNodeId,
                queries: {
                    ...state.queries,
                    [action.resultId]: {
                        ...queryResult,
                        resultIndex: action.resultIndex
                    }
                }
            }
        },
    };

    return {

        reducer: (
            state: TraceViewState = initialState,
            action: AnyAction
        ): TraceViewState => {

            const reducerFunction = PerActionReducers[action.type];
            if (!!reducerFunction) {
                return reducerFunction(state, action);
            } else {
                return state;
            }

            /*
            switch (action.type) {
                case TraceViewActionTypes.NODE_ADD:
                    const nodeAddAction = action as ReturnType<typeof TraceViewActions.nodeAdd>;
                    const stateAfterNodeAdd = PerActionReducers.reduceNodeAdd(state, nodeAddAction);
                    //console.log('traceViewState.reduceNodeAdd', action, state, stateAfterNodeAdd);
                    return stateAfterNodeAdd;

                case TraceViewActionTypes.NODE_UPDATE: 
                    const nodeUpdateAction = action as ReturnType<typeof TraceViewActions.nodeUpdate>;
                    const stateAfterNodeUpdate = PerActionReducers.reduceNodeUpdate(state, nodeUpdateAction);
                    //console.log('traceViewState.reduceNodeUpdate', action, state, stateAfterNodeUpdate);
                    return stateAfterNodeUpdate;

                case TraceViewActionTypes.NODE_EXPAND: 
                    const nodeExpandAction = action as ReturnType<typeof TraceViewActions.nodeExpand>;
                    const stateAfterNodeExpand = PerActionReducers.reduceNodeExpand(state, nodeExpandAction);
                    //console.log('traceViewState.reduceNodeExpand', action, state, stateAfterNodeExpand);
                    return stateAfterNodeExpand;

                case TraceViewActionTypes.NODE_COLLAPSE: 
                    const nodeCollapseAction = action as ReturnType<typeof TraceViewActions.nodeCollapse>;
                    const stateAfterNodeCollapse = PerActionReducers.reduceNodeCollapse(state, nodeCollapseAction);
                    //console.log('traceViewState.reduceNodeCollapse', action, state, stateAfterNodeCollapse);
                    return stateAfterNodeCollapse;

                case TraceViewActionTypes.NODE_SELECT: 
                    const nodeSelectAction = action as ReturnType<typeof TraceViewActions.nodeSelect>;
                    const stateAfterNodeSelect = PerActionReducers.reduceNodeSelect(state, nodeSelectAction);
                    //console.log('traceViewState.reduceNodeSelect', action, state, stateAfterNodeSelect);
                    return stateAfterNodeSelect;

                case TraceViewActionTypes.VIEW_CHANGE_NOTIFY:
                    const stateAfterChangeNotify = PerActionReducers.reduceViewChangeNotify(state);
                    //console.log('traceViewState.reduceViewChangeNotify', action, state, stateAfterChangeNotify);
                    return stateAfterChangeNotify;

                case TraceViewActionTypes.FILTER_QUERY_SET: 
                    const filterSetAction = action as ReturnType<typeof TraceViewActions.filterQuerySet>;
                    const stateAfterFilterSet = PerActionReducers.reduceFilterQuerySet(state, filterSetAction);
                    //console.log('traceViewState.reduceFilterQuerySet', action, state, stateAfterFilterSet);
                    return stateAfterFilterSet;

                case TraceViewActionTypes.FILTER_QUERY_CLEAR: 
                    const filterClearAction = action as ReturnType<typeof TraceViewActions.filterQueryClear>;
                    const stateAfterFilterClear = PerActionReducers.reduceFilterQueryClear(state, filterClearAction);
                    //console.log('traceViewState.reduceFilterQueryClear', action, state, stateAfterFilterClear);
                    return stateAfterFilterClear;

                case TraceViewActionTypes.QUERY_RESULT_SET: 
                    const queryResultSetAction = action as ReturnType<typeof TraceViewActions.queryResultSet>;
                    const stateAfterResultSet = PerActionReducers.reduceQueryResultSet(state, queryResultSetAction);
                    //console.log('traceViewState.reduceFilterQueryClear', action, state, queryResultSetAction);
                    return stateAfterResultSet;

                case TraceViewActionTypes.QUERY_RESULT_CLEAR: 
                    const queryResultClearAction = action as ReturnType<typeof TraceViewActions.queryResultClear>;
                    const stateAfterResultClear = PerActionReducers.reduceQueryResultClear(state, queryResultClearAction);
                    //console.log('traceViewState.reduceFilterQueryClear', action, state, queryResultClearAction);
                    return stateAfterResultClear;

                case TraceViewActionTypes.QUERY_RESULT_GOTO: 
                    const queryResultGotoAction = action as ReturnType<typeof TraceViewActions.queryResultGoto>;
                    const stateAfterResultGoto = PerActionReducers.reduceQueryResultGoto(state, queryResultGotoAction);
                    //console.log('traceViewState.reduceQueryResultGoto', action, state, queryResultGotoAction);
                    return stateAfterResultGoto;

                default:
                    return state;
            }
            */
        },

        startTraceViewUpdates: (store: Store) => {
            let oldView: TraceTreeLayer | undefined = undefined;
            
            const nodeListener: TraceNodeListener = (node, action) => {
                switch (action) {
                    case LISTENER_ACTION_RECEIVED:
                        store.dispatch(TraceViewActions.nodeAdd(node));
                        break;
                    case LISTENER_ACTION_UPDATED:
                        store.dispatch(TraceViewActions.nodeUpdate(node.id));
                        break;
                }
            };

            traceService.addViewChangedListener(newView => {
                oldView?.removeNodeListener(nodeListener);
                newView.addNodeListener(nodeListener);
                oldView = newView;
                
                store.dispatch(TraceViewActions.viewChangeNotify());
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
