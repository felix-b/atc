export {}
/*

import { createSlice, configureStore, PayloadAction, Store } from '@reduxjs/toolkit'
import { RootState } from '../../app/store';
import { LISTENER_ACTION_RECEIVED, LISTENER_ACTION_UPDATED, TraceNode, TraceNodeListenerAction, TraceService } from '../../services/traceService';

export interface TraceViewState {
    readonly topLevelNodes: TraceViewStateNode[];
    nodeById: Record<string, TraceViewStateNode>;
}

export interface TraceViewStateNode {
    readonly id: BigInt;
    readonly idString: string;
    readonly isExpandable: boolean;
    readonly depth: number;
    children?: TraceViewStateNode[];
    isExpanded: boolean;
    version: number;
}

const initialState: TraceViewState = {
    topLevelNodes: [],
    nodeById: {}
};

export const traceViewSlice = createSlice({
    name: 'traceView',
    initialState,
    reducers: {
        nodeAdded: (state, action: PayloadAction<TraceNode>) => {
            const { payload } = action;
            console.log('traceViewSlice.nodeAdded', payload);
            
            const parentNode = state.nodeById[payload.parentSpanId.toString()];
            const node = createTraceViewStateNode(payload, parentNode);
            state.nodeById[node.idString] = node;

            if (!parentNode) {
                state.topLevelNodes.push(node);
            } else if (parentNode.isExpanded) {
                if (parentNode.children) {
                    parentNode.children.push(node);
                } else {
                    parentNode.children = [node];
                }
            }
        },
        nodeUpdated: (state, action: PayloadAction<TraceNode>) => {
            const { payload } = action;
            console.log('traceViewSlice.nodeUpdated', action.payload);

            const node = state.nodeById[payload.id.toString()];
            if (node) {
                node.version++;
            }
        },
        nodeExpanded: (state, action: PayloadAction<BigInt>) => {
            console.log('traceViewSlice.nodeExpanded', action.payload);
            const nodeId = action.payload;
            const stateNode = state.nodeById[nodeId.toString()];
            const dataNode = window.traceService?.getNodeById(nodeId);

            if (!dataNode) {
                console.error('Could not get data for node:', nodeId);
                return;
            }

            if (stateNode && stateNode.isExpandable && !stateNode.isExpanded) {
                stateNode.isExpanded = true;
                stateNode.children = dataNode
                    .children
                    .map(childData => createTraceViewStateNode(childData, stateNode));
            }
        },
        nodeCollapsed: (state, action: PayloadAction<BigInt>) => {
            console.log('traceViewSlice.nodeCollapsed', action.payload);
            const nodeId = action.payload;
            const stateNode = state.nodeById[nodeId.toString()];
            if (stateNode && stateNode.isExpanded) {
                stateNode.isExpanded = false;
                stateNode.children = undefined
            }
        },
        nodeSelected: (state, action: PayloadAction<BigInt>) => {
        },
    },
});

export const traceViewSelectors = {
    topLevelNodes(state: RootState): TraceViewStateNode[] {
        return state.traceView.topLevelNodes;
    } 
};

export function startTraceViewUpdates(service: TraceService, store: Store) {
    const { nodeAdded, nodeUpdated } = traceViewSlice.actions;

    service.addNodeListener((node, action) => {
        switch (action) {
            case LISTENER_ACTION_RECEIVED:
                store.dispatch(nodeAdded(node));
                break;
            case LISTENER_ACTION_UPDATED:
                store.dispatch(nodeUpdated(node));
                break;
        }
    });
}

function createTraceViewStateNode(data: TraceNode, parent: TraceViewStateNode | undefined): TraceViewStateNode {
    return {
        id: data.id,
        idString: data.id.toString(),
        isExpandable: data.isSpan,
        depth: parent ? parent.depth + 1 : 0,
        children: undefined,
        isExpanded: false,
        version: 1,
    };
}
*/
