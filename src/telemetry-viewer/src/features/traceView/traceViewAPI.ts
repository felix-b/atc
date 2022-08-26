import { Dispatch } from "@reduxjs/toolkit";
import { AppStore } from "../../app/store";
import { LogLevel, TraceQuery, TraceService } from "../../services/types";
import { TraceViewActions } from "./traceViewState";

export interface TraceViewAPI {
    connect(): void;
    disconnect(): void;
    swtichFilter(on: boolean): void;
    addQuery(text?: string, logLevels?: LogLevel[]): void;
    removeQuery(id: number): void;
    goToResult(queryId: number, resultIndex: number): void;
}

export function createTraceViewAPI(
    store: AppStore, 
    traceService: TraceService
): TraceViewAPI {

    const { dispatch } = store;

    const scrollTraceViewToNode = (nodeId: string) => {
        window.setTimeout(() => {
            const trId = `tr${nodeId}`;
            document.getElementById(trId)?.scrollIntoView({ block: 'center' });
        }, 50);
    };
    
    const applyFilter = () => {
        const currentState = store.getState().traceView;
        const queries = Object.values(currentState.queries).map(v => v.query);
        const { selectedNodeId } = store.getState().traceView;

        const includeNodeIds = selectedNodeId ? [selectedNodeId] : []
        traceService.setFilter(queries, includeNodeIds);
        dispatch(TraceViewActions.filterSwitch(true));

        if (selectedNodeId) {
            dispatch(TraceViewActions.nodeGoto(selectedNodeId));
            scrollTraceViewToNode(selectedNodeId);
        }
    };

    const clearFilter = () => {
        const currentState = store.getState().traceView;
        const { selectedNodeId } = currentState;
        
        traceService.clearFilter();
        dispatch(TraceViewActions.filterSwitch(false));

        if (selectedNodeId) {
            dispatch(TraceViewActions.nodeGoto(selectedNodeId));
            scrollTraceViewToNode(selectedNodeId);
        }
    };

    return {
        connect() {
            traceService.connect();
            dispatch(TraceViewActions.connectedSet(true));
        },
        disconnect() {
            traceService.disconnect();
            dispatch(TraceViewActions.connectedSet(false));
        },
        swtichFilter(on: boolean) {
            dispatch(TraceViewActions.filterSwitch(on));
            if (on) {
                applyFilter();
            } else {
                clearFilter();
            }
        },
        addQuery(text?: string, logLevels?: LogLevel[]) {
            if (text && text.trim().length === 0) {
                return;
            }
            const query: TraceQuery = { 
                text, 
                logLevels 
            };
            const initialResults = traceService.createQuery(
                query, 
                updatedResults => {
                    dispatch(TraceViewActions.queryResultSet(updatedResults));
                }
            );
            dispatch(TraceViewActions.queryResultSet(initialResults));

            const latestState = store.getState().traceView;
            if (latestState.isFilterActive) {
                applyFilter();
            }
        },
        removeQuery(id: number) {
            traceService.disposeQuery(id);
            dispatch(TraceViewActions.queryResultClear(id));

            const latestState = store.getState().traceView;
            if (latestState.isFilterActive) {
                const hasQueries = Object.keys(latestState.queries).length > 0;
                if (hasQueries) {
                    applyFilter();
                } else {
                    clearFilter();
                    dispatch(TraceViewActions.filterSwitch(false));
                }
            }
        },
        goToResult(queryId: number, resultIndex: number) {
            const latestState = store.getState().traceView;
            dispatch(TraceViewActions.queryResultGoto(queryId, resultIndex));
            scrollTraceViewToNode(latestState.queries[queryId].resultNodeIds[resultIndex]);
        }
    };     
}
