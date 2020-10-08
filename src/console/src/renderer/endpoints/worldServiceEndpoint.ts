import { DeepPartial } from "redux";
import { ClientToServer, ServerToClient } from "../../proto/world";

export type IncomingPayloadType = keyof Omit<
    ServerToClient, 
    'id'|'sentAt'|'replyToRequestId'|'sentAt'|'requestSentAt'|'requestReceivedAt'
>;
export type IncomingMetaProps = keyof Pick<
    ServerToClient, 
    'id'|'sentAt'|'replyToRequestId'|'sentAt'|'requestSentAt'|'requestReceivedAt'
>;

export type OutgoingPayloadType = keyof Omit<ClientToServer, 'id'>;
export type OutgoingMetaProps = keyof Pick<ClientToServer, 'id'>; 
export type MessageListener = (envelope: ServerToClient) => void;
export type StatusListener = () => void;

const incomingMetaPropNames = new Set<IncomingMetaProps>([
    'id',
    'sentAt',
    'replyToRequestId', 
    'requestSentAt',
    'requestReceivedAt',
]);

const outgoingMetaPropNames = new Set<OutgoingMetaProps>([
    'id',
]);

export const WorldServiceEndpoint = createWorldServiceEndpoint();

function createWorldServiceEndpoint() {
    const listenersByPayloadType = new Map<IncomingPayloadType, MessageListener[]>();
    const openStatusListeners: StatusListener[] = [];

    let nextMessageId = 1;

    const socket = new WebSocket('ws://localhost:9002');
    socket.binaryType = 'blob';
    socket.addEventListener('message', function (event) {
        const reader = new FileReader();
        reader.readAsArrayBuffer(event.data);
        reader.addEventListener('loadend', e => {
            const result = e.target?.result;
            if (result) {
                receiveMessage(result as ArrayBuffer);
            }
        });
    });
    socket.addEventListener('open', () => {
        console.log('ENDPOINT-OPEN>', `invoking ${openStatusListeners.length} status listener(s)`);
        for (const listener of openStatusListeners) {
            listener();
        }
    });

    return {
        onOpen: addOpenStatusListener,
        onMessage: addMessageListener,
        sendMessage,
    }

    function addOpenStatusListener(listener: StatusListener) {
        if (socket.readyState === socket.OPEN) {
            console.log('ENDPOINT-OPEN>', `invoking new listener`);
            listener();
        } else {
            openStatusListeners.push(listener);
        }
    }

    function addMessageListener(payloadType: IncomingPayloadType, listener: MessageListener) {
        const existingListeners = listenersByPayloadType.get(payloadType);
        if (existingListeners) {
            existingListeners.push(listener);
        } else {
            listenersByPayloadType.set(payloadType, [listener]);
        }
    }

    function receiveMessage(dataOnWire: ArrayBuffer) {
        const buffer = new Uint8Array(dataOnWire);
        const byteArray: number[] = Array.prototype.slice.call(buffer.slice());
        //console.log('ENDPOINT-RECV>', byteArray);

        let envelope: ServerToClient = ServerToClient.decode(buffer);
        console.log(`ENDPOINT-RECV> payload[${getIncomingPayloadType(envelope)}]`, envelope);

        dispatchMessage(envelope);
    }

    function sendMessage(message: DeepPartial<ClientToServer>) {
        message.id = nextMessageId++;
        console.log(`ENDPOINT-SEND> payload[${getOutgoingPayloadType(message as ClientToServer)}]`, message);
        const writer = ClientToServer.encode(message as ClientToServer);
        const byteArray = writer.finish();
        //console.log('ENDPOINT-SEND>', byteArray);
        socket.send(byteArray);
    }

    function dispatchMessage(envelope: ServerToClient) {
        const payloadType = getIncomingPayloadType(envelope);
        if (!payloadType) {
            console.error('ENDPOINT-RECV>', 'ERROR: payload type not recognized');
            return;
        }

        const listeners = listenersByPayloadType.get(payloadType);
        if (!listeners) {
            console.warn('ENDPOINT-RECV>', `WARNING: no listeners for payload ${payloadType}`);
            return;
        }

        console.debug('ENDPOINT-RECV>', `invoking ${listeners.length} listener(s) for payload ${payloadType}`);
        for (const listener of listeners) {
            listener(envelope);
        }
    }


    // function beginConnect() {
    //     sendMessage({
    //         connect: {
    //             token: 'HELLO'
    //         }
    //     });
    // }

    function getIncomingPayloadType(message: ServerToClient): IncomingPayloadType | undefined {
        for (const prop in message) {
            if (!incomingMetaPropNames.has(prop as IncomingMetaProps)) {
                return prop as IncomingPayloadType;
            }
        }
    }

    function getOutgoingPayloadType(message: ClientToServer): OutgoingPayloadType | undefined {
        for (const prop in message) {
            if (!outgoingMetaPropNames.has(prop as OutgoingMetaProps)) {
                return prop as OutgoingPayloadType;
            }
        }
    }
};
