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

interface PrivateTraceNodePresentation extends TraceNodePresentation {
    parseCloseSpan(buffer: BufferReader): void;
}

export type TraceNodeListenerAction = 'received' | 'updated';
export const LISTENER_ACTION_RECEIVED: TraceNodeListenerAction = 'received';
export const LISTENER_ACTION_UPDATED: TraceNodeListenerAction = 'updated';

export type TraceNodeListener = (
    node: TraceNode, 
    action: TraceNodeListenerAction
) => void;

export interface TraceService {
    getTopLevelNodes(): TraceNode[];
    getNodeById(id: BigInt): TraceNode;
    addNodeListener(callback: TraceNodeListener): void;
}

declare global {
    interface Window { 
        traceService: TraceService | undefined; 
    }
}

export const TraceServiceSingleton = {
    get instance(): TraceService | undefined {
        return window.traceService;
    },
    set instance(value: TraceService | undefined) {
        window.traceService = value;
    }
};

const UNIX_EPOCH_TICKS = BigInt('621355968000000000');
const NANOSECx100_IN_MILLISECOND = BigInt('10000');

export function createTraceService(endpointUrl: string): TraceService {
    
    const _endpointUrl = endpointUrl;
    const _rootNode = createRootNode();
    const _nodeById = new Map<BigInt, TraceNode>();
    const _stringByKey = new Map<number, string>();
    const _nodeListeners: TraceNodeListener[] = [];
    const _webSocket: WebSocket = new WebSocket(endpointUrl);// 'http://localhost:3003/telemetry'
    let _nextInternalNodeId = -1; // an ever-decreasing number, to dstinguish from Span Ids which are positive

    _webSocket.binaryType = 'arraybuffer';
    _webSocket.onerror = e => console.log('TraceService.webSocketError', e);
    _webSocket.onclose = e => console.log('TraceService.webSocketClose', e);
    _webSocket.onopen = e => {
        console.log('TraceService.webSocketOpen', e);
        sendEnvelopeMessage({
            connectRequest: { }
        })
    };
    _webSocket.onmessage = e => {
        //console.log('TraceService.webSocketMessageReceived', e.data);
        if (e.data instanceof ArrayBuffer) {
            receiveEnvelopeMessage(e.data);
        }
    };

    const service = {
        getTopLevelNodes() {
            return _rootNode.children;
        },

        getNodeById(id: BigInt): TraceNode {
            const node = _nodeById.get(id);
            if (node) {
                return node;
            }
            throw new Error(`Node id ${id} not found`);
        },

        addNodeListener(callback: TraceNodeListener): void {
            _nodeListeners.push(callback);
        },
    };

    TraceServiceSingleton.instance = service;
    return service;

    function receiveEnvelopeMessage(data: ArrayBuffer) {
        const envelope = CodePathServerToClient.decode(new Uint8Array(data));
        if (envelope.connectReply) {
            console.log('TraceService.receivedConnectReply', envelope.connectReply);
        }
        if (envelope.telemetryBuffer) {
            //console.log('TraceService.receivedTelemetryBuffer', envelope.telemetryBuffer);
            const { buffer } = envelope.telemetryBuffer;
            const dataView = new DataView(buffer.buffer, buffer.byteOffset, buffer.byteLength);
            readTelemetryBuffer(dataView);
        }
    }

    function sendEnvelopeMessage(envelope: DeepPartial<CodePathClientToServer>) {
        console.log('TraceService.webSocketEncodeOutgoing', envelope);

        const writer = CodePathClientToServer.encode(envelope as CodePathClientToServer);
        const byteArray = writer.finish();

        console.log('TraceService.webSocketSendBytes', byteArray);

        _webSocket.send(byteArray);
    }

    function readTelemetryBuffer(dataView: DataView) {
        const buffer = createBufferReader(dataView);

        while (!buffer.isEndOfBuffer) {
            const opCode: TraceOpCode = buffer.readUint8();

            switch (opCode) {
                case TraceOpCode.stringKey:
                    readStringKeyEntry(buffer);
                    break;
                case TraceOpCode.message:
                case TraceOpCode.beginMessage:
                case TraceOpCode.openSpan:
                case TraceOpCode.beginOpenSpan:
                    receiveTraceNode(opCode, buffer);
                    return;
                case TraceOpCode.closeSpan:
                case TraceOpCode.beginCloseSpan:
                    receiveCloseSpan(buffer);
                    return;
                default:
                    console.warn('TraceService.readTelemetryBuffer >>> UNEXPECTED OPCODE!', opCode);
            }
        }
    }

    function receiveTraceNode(opCode: TraceOpCode, buffer: BufferReader) {
        const node = createNode(opCode, buffer);
        _nodeById.set(node.id, node);

        const parentNode = _nodeById.get(node.parentSpanId) || _rootNode;
        parentNode.addChild(node);

        invokeNodeListeners(node, LISTENER_ACTION_RECEIVED);
    }

    function receiveCloseSpan(buffer: BufferReader) {
        const spanId = buffer.readUint64();
        const node = _nodeById.get(spanId);

        if (node) {
            node.addCloseSpan(buffer);
            invokeNodeListeners(node, LISTENER_ACTION_UPDATED);
        }
    }

    function invokeNodeListeners(node: TraceNode, action: TraceNodeListenerAction) {
        _nodeListeners.forEach(callback => {
            try {
                callback(node, action);
            } catch (err) {
                console.log(err);
            }
        });
    } 

    function createNode(opCode: TraceOpCode, buffer: BufferReader | undefined): TraceNode {

        if (!buffer) {
            throw new Error('createNode: buffer was not supplied');
        }

        const _children: TraceNode[] = [];
        const _isSpan = opCode === TraceOpCode.openSpan || opCode === TraceOpCode.beginOpenSpan;
        const _id = _isSpan 
            ? buffer.readUint64() 
            : BigInt(_nextInternalNodeId--);
        const _parentSpanId = buffer.readUint64();
        
        let _closeSpanBuffer: BufferReader | undefined = undefined;
        let _presentation: PrivateTraceNodePresentation | undefined = undefined;

        const node: TraceNode = {
            get id() {
                return _id;
            },
            get parentSpanId() {
                return _parentSpanId;
            }, 
            get isSpan() {
                return _isSpan;
            },
            get opCode() {
                return opCode;
            },
            get unparsed() {
                return buffer;
            },
            get children() {
                return _children;
            },
            getPresentation() {
                if (!_presentation) {
                    _presentation = createNodePresentation(node, buffer!, _closeSpanBuffer);
                    buffer = undefined;
                    _closeSpanBuffer = undefined;
                }
                return _presentation;
            },
            addChild(node: TraceNode) {
                _children.push(node);
            },
            addCloseSpan(closeSpanBuffer: BufferReader) {
                closeSpanBuffer.rewindInt8();
                if (_presentation) {
                    _presentation.parseCloseSpan(closeSpanBuffer)
                } else {
                    _closeSpanBuffer = closeSpanBuffer;
                }
            }
        };   
        
        return node;
    }

    function createRootNode(): TraceNode {
        const _children: TraceNode[] = [];

        return {
            get id() {
                return BigInt(0);
            },
            get parentSpanId() {
                return BigInt(0);
            }, 
            get isSpan() {
                return true;
            },
            get opCode() {
                return TraceOpCode.noop;
            },
            get unparsed() {
                return undefined;
            },
            get children() {
                return _children;
            },
            getPresentation() {
                throw new Error('NotSupported');
            },
            addChild(node: TraceNode) {
                _children.push(node);
            },
            addCloseSpan() {
                throw new Error('NotSupported');
            }
        };
    }

    function createNodePresentation(
        node: TraceNode, 
        buffer: BufferReader, 
        closeSpanBuffer: BufferReader | undefined
    ): PrivateTraceNodePresentation {
        
        if (!buffer) {
            throw new Error('createNodePresentation: buffer was not supplied');
        }
        
        const parseCloseSpan = (buffer: BufferReader) => {
            //TODO
        }

        const timestampTicks = buffer.readUint64();
        const messageId = readStringKey(buffer);
        const level = buffer.readInt8();
        const threadId = buffer.readInt32();
        const values = node.opCode === TraceOpCode.beginMessage || node.opCode === TraceOpCode.beginOpenSpan
            ? readValues(buffer)
            : {};

        return {
            id: node.id.toString(),
            parentSpanId: node.parentSpanId === BigInt(0) 
                ? undefined 
                : node.parentSpanId.toString(),
            timestamp: dotnetTicksToTimestampString(timestampTicks),
            messageId,
            level,
            threadId,
            //depth: node;
            isSpan: node.isSpan,
            isSpanInProgress: node.isSpan && !closeSpanBuffer,
            endTimestamp: undefined,
            duration: undefined,
            error: undefined,
            values,
            parseCloseSpan
        };
    }

    function readValues(buffer: BufferReader): Record<string, string> {
        let values: Record<string, string> = {};

        while (!buffer.isEndOfBuffer) {
            const opCode = buffer.readUint8() as TraceOpCode;
            let key: string = '';
            switch (opCode) {
                case TraceOpCode.int32Value:
                    key = readStringKey(buffer);
                    values[key] = buffer.readInt32().toString();
                    break;
                case TraceOpCode.stringValue:
                    key = readStringKey(buffer);
                    values[key] = buffer.readLengthPrefixedUtf8String();
                    break;
                case TraceOpCode.boolValue:
                    key = readStringKey(buffer);
                    values[key] = buffer.readInt8() === 1 ? 'true' : 'false';
                    break;
                case TraceOpCode.timeSpanValue:
                    key = readStringKey(buffer);
                    values[key] = dotnetTicksToDurationString(buffer.readUint64());
                    break;
                case TraceOpCode.exceptionValue:
                    key = 'exception'
                    const exceptionTypeString = buffer.readLengthPrefixedUtf8String();
                    const exceptionMessageString = buffer.readLengthPrefixedUtf8String();
                    values[key] = `${exceptionTypeString}: ${exceptionMessageString}`;
                    break;
                default:
                    buffer.rewindInt8();
                    return values;
            }
        }

        return values;
    }

    function readStringKeyEntry(buffer: BufferReader) {
        const key = buffer.readInt32();
        const value = buffer.readLengthPrefixedUtf8String();
        _stringByKey.set(key, value);
    }

    function readStringKey(buffer: BufferReader): string {
        const key = buffer.readInt32();
        const stringValue = _stringByKey.get(key);
        return stringValue || `missing-string-${key}`;
    }

    function dotnetTicksToTimestampString(ticks: BigInt): string {
        const unixTicks = (ticks as bigint) - (UNIX_EPOCH_TICKS as bigint);
        const unixMilliseconds = unixTicks / NANOSECx100_IN_MILLISECOND;
        const date = new Date(Number(unixMilliseconds));

        const hourDigits = `${date.getHours()}`.padStart(2, '0');
        const minuteDigits = `${date.getMinutes()}`.padStart(2, '0');
        const secondDigits = `${date.getSeconds()}`.padStart(2, '0');
        const millisecondDigits = `${date.getMilliseconds()}`.padEnd(3, '0');
        return `${hourDigits}:${minuteDigits}:${secondDigits}.${millisecondDigits}`;        
    }

    function dotnetTicksToDurationString(ticks: BigInt): string {
        const totalMilliseconds = Number((ticks as bigint) / NANOSECx100_IN_MILLISECOND);
        const totalSeconds = totalMilliseconds / 1000;
        const totalMinutes = totalSeconds / 60;
        const totalHours = totalMinutes / 24;

        const hourDigits = `${parseInt(totalHours as any)}`.padStart(2, '0');
        const minuteDigits = `${parseInt(totalMinutes as any) % 60}`.padStart(2, '0');
        const secondDigits = `${parseInt(totalSeconds as any) % 60}`.padStart(2, '0');
        const millisecondDigits = `${parseInt(totalMilliseconds as any) % 1000}`.padEnd(3, '0');
        
        return `${hourDigits}:${minuteDigits}:${secondDigits}.${millisecondDigits}`;
    }
}
