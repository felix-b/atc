import { 
    LISTENER_ACTION_RECEIVED, 
    LISTENER_ACTION_UPDATED, 
    TraceNode, 
    TraceNodeListener, 
    TraceNodeListenerAction, 
    TraceNodePresentation, 
    TraceOpCode, 
    TraceQuery, 
    TraceQueryObserver, 
    TraceQueryResultCallback, 
    TraceQueryResults, 
    TraceService, 
    TraceTreeLayer, 
    TraceViewChangedListener
} from "./types";

import { BufferReader, createBufferReader } from "./bufferReader";
import { CodePathClientToServer, CodePathServerToClient, DeepPartial } from "../proto/codepath";
import { createTraceFilter } from "./traceFilter";
import { createTraceQueryObserver } from "./traceQueryObserver";

const UNIX_EPOCH_TICKS = BigInt('621355968000000000');
const NANOSECx100_IN_MILLISECOND = BigInt('10000');

interface PrivateTraceNodePresentation extends TraceNodePresentation {
    parseCloseSpan(buffer: BufferReader): void;
}

export const TraceServiceSingleton = {
    get instance(): TraceService | undefined {
        return window.traceService;
    },
    set instance(value: TraceService | undefined) {
        window.traceService = value;
    }
};

export function createTraceService(endpointUrl: string): TraceService {
    
    const _endpointUrl = endpointUrl;
    const _rootNode = createRootNode();
    const _nodeById = new Map<BigInt, TraceNode>();
    const _stringByKey = new Map<number, string>();
    const _nodeListeners: TraceNodeListener[] = [];
    const _viewChangedListeners: TraceViewChangedListener[] = [];
    const _queryObservers = new Map<number, TraceQueryObserver>();
    let _webSocket: WebSocket | undefined = undefined;
    let _connectedStateRequested: boolean = false;
    let _nextInternalNodeId = -1; // an ever-decreasing number, to dstinguish from Span Ids which are positive
    let _nextQueryId = 1;
    let _filterLayer: TraceTreeLayer | undefined = undefined;

    _nodeById.set(_rootNode.id, _rootNode);


    const service: TraceService & TraceTreeLayer = {
        connect() {
            _connectedStateRequested = true;
            if (!_webSocket) {
                _webSocket = initWebSocket();
            }
        },

        disconnect() {
            _connectedStateRequested = false;
            if (_webSocket) {
                _webSocket.close();
                _webSocket = undefined;
            }
        },

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

        tryGetNodeById(id: BigInt): TraceNode | undefined {
            return _nodeById.get(id);
        },

        addNodeListener(callback: TraceNodeListener): void {
            _nodeListeners.push(callback);
        },

        removeNodeListener(callback: TraceNodeListener): void {
            const index = _nodeListeners.indexOf(callback);
            if (index >= 0) {
                _nodeListeners.splice(index, 1);
            }
        },

        addViewChangedListener(callback: TraceViewChangedListener) {
            _viewChangedListeners.push(callback);
            callback(this.getCurrentView());
        },

        removeViewChangedListener(callback: TraceViewChangedListener) {
            const index = _viewChangedListeners.indexOf(callback);
            if (index >= 0) {
                _viewChangedListeners.splice(index, 1);
            }
        },

        getCurrentView(): TraceTreeLayer {
            return _filterLayer || service;
        },

        setFilter(queries: TraceQuery[], includeNodeIds: string[]) {
            _filterLayer?.dispose();
            _filterLayer = createTraceFilter(service, queries, includeNodeIds);
            invokeViewChangedListeners(_filterLayer);
        },
        
        clearFilter() {
            _filterLayer?.dispose();
            _filterLayer = undefined;
            invokeViewChangedListeners(service);
        },

        dispose() {
            throw new Error('NotSupported');
        },

        createQuery(query: TraceQuery, onUpdate: TraceQueryResultCallback): TraceQueryResults {
            const newQueryId = _nextQueryId++;
            const observer = createTraceQueryObserver(service, onUpdate, newQueryId, query);
            _queryObservers.set(newQueryId, observer);
            return observer.runQuery();
        },

        disposeQuery(id: number): void {
            const observer = _queryObservers.get(id);
            observer?.dispose();
            _queryObservers.delete(id);
        }
    };

    (service as any).getWebSocket = () => _webSocket;

    TraceServiceSingleton.instance = service;
    return service;

    function retryConnect() {
        console.log('TraceService.retryConnect', 'will retry in 1s');
        window.setTimeout(() => {
            _webSocket = initWebSocket();
        }, 1000);
    }

    function initWebSocket(): WebSocket {
        const socket: WebSocket = new WebSocket(endpointUrl);// 'http://localhost:3003/telemetry'
        socket.binaryType = 'arraybuffer';
        socket.onerror = e => console.log('TraceService.webSocketError', e);
        socket.onclose = e => {
            console.log('TraceService.webSocketClose', e);
            if (_connectedStateRequested) {
                retryConnect();
            } else {
                _webSocket = undefined;
            }
        };
        socket.onopen = e => {
            console.log('TraceService.webSocketOpen', e);
            sendEnvelopeMessage({
                connectRequest: { }
            })
        };
        socket.onmessage = e => {
            //console.log('TraceService.webSocketMessageReceived', e.data);
            if (e.data instanceof ArrayBuffer) {
                receiveEnvelopeMessage(e.data);
            }
        };
        return socket;
    };

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
        if (!_webSocket) {
            console.error('TraceService.sendEnvelopeMessage: not connected');
            return;
        }

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

    function invokeViewChangedListeners(newView: TraceTreeLayer) {
        _viewChangedListeners.forEach(callback => {
            try {
                callback(newView);
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
                    //buffer = undefined;
                    //_closeSpanBuffer = undefined;
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
            },
            printBuffer() {
                buffer!.printBuffer();
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
            },
            printBuffer() { }
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
                case TraceOpCode.dateTimeValue:
                    key = readStringKey(buffer);
                    values[key] = dotnetTicksToTimestampString(buffer.readUint64());
                    break;
                    case TraceOpCode.uInt64Value:
                    key = readStringKey(buffer);
                    values[key] = buffer.readUint64().toString();
                    break;
                case TraceOpCode.int64Value:
                    key = readStringKey(buffer);
                    values[key] = buffer.readInt64().toString();
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
