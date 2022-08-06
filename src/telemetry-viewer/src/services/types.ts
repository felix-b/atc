import { 
    CodePathClientToServer, 
    CodePathServerToClient, 
    DeepPartial 
} from "../proto/codepath";
import { BufferReader, createBufferReader } from "./bufferReader";

export enum LogLevel {
    quiet = -1,
    audit = 0,
    critical = 1,
    error = 2,
    warning = 3,
    info = 4,
    verbose = 5,
    debug = 6,
}

export enum TraceOpCode {
    noop = 0,
    // stream op-codes
    beginStreamChunk = 0xA0,
    endStreamChunk = 0xA1,
    stringKey = 0xA2,
    // message op-codes
    message = 0xB1,
    beginMessage = 0xB2,
    endMessage = 0xB3,
    openSpan = 0xB4,
    beginOpenSpan = 0xB5,
    endOpenSpan = 0xB6,
    closeSpan = 0xB7,
    beginCloseSpan = 0xB8,
    endCloseSpan = 0xB9,  
    // value op-codes
    exceptionValue = 0xC0,
    boolValue = 0xC1,
    int8Value = 0xC2,
    int16Value = 0xC3,
    int32Value = 0xC4,
    int64Value = 0xC5,
    uInt64Value = 0xC6,
    floatValue = 0xC7,
    doubleValue = 0xC8,
    decimalValue = 0xC9,
    stringValue = 0xCA,
    timeSpanValue = 0xCB,
    dateTimeValue = 0xCC,
}

export interface TraceNode {
    readonly id: BigInt; // positive if holds span id; negative if holds viewer-generated id for message node
    readonly parentSpanId: BigInt; // parent span id (0 for top-level nodes)
    readonly isSpan: boolean;
    readonly opCode: TraceOpCode;
    readonly unparsed?: BufferReader;
    readonly children: TraceNode[];

    getPresentation(): TraceNodePresentation;
    addChild(node: TraceNode): void;
    addCloseSpan(closeSpanBuffer: BufferReader): void;
    printBuffer(): void;
}

export interface TraceNodePresentation {
    id: string;
    parentSpanId: string | undefined;
    timestamp: string;
    messageId: string;
    level: LogLevel;
    threadId: number;
    //depth: number;
    isSpan: boolean;
    isSpanInProgress: boolean;
    endTimestamp?: string;
    duration?: string;
    error?: string;
    values: Record<string, string>;
}

export type TraceNodeListenerAction = 'received' | 'updated';
export const LISTENER_ACTION_RECEIVED: TraceNodeListenerAction = 'received';
export const LISTENER_ACTION_UPDATED: TraceNodeListenerAction = 'updated';

export type TraceNodeListener = (
    node: TraceNode, 
    action: TraceNodeListenerAction
) => void;

export type TraceViewChangedListener = (
    newView: TraceTreeLayer
) => void;

export interface TraceTreeLayer {
    getTopLevelNodes(): TraceNode[];
    getNodeById(id: BigInt): TraceNode;
    tryGetNodeById(id: BigInt): TraceNode | undefined;
    addNodeListener(callback: TraceNodeListener): void;
    removeNodeListener(callback: TraceNodeListener): void;
    dispose(): void;
}

export interface TraceService { 
    getCurrentView(): TraceTreeLayer;
    setFilter(queries: TraceQuery[], includeNodeIds: string[]): void;
    clearFilter(): void;
    addViewChangedListener(callback: TraceViewChangedListener): void;
    removeViewChangedListener(callback: TraceViewChangedListener): void;
    createQuery(query: TraceQuery, onUpdate: TraceQueryResultCallback): TraceQueryResults;
    disposeQuery(id: number): void;
}

export interface TraceQuery {
    text?: string; //TODO: add more criteria
    logLevels?: LogLevel[];
}

export interface TraceQueryResults {
    id: number;
    query: TraceQuery;
    resultNodeIds: string[];
    resultIndex: number;
}

export type TraceQueryResultCallback = (results: TraceQueryResults) => void;

export interface TraceQueryObserver {
    readonly id: number;
    runQuery(): TraceQueryResults;
    dispose(): void;
}

declare global {
    interface Window { 
        traceService: TraceService | undefined; 
    }
}
