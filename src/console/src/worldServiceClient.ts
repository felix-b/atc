import { 
    IncomingMetaProps, 
    IncomingPayloadType, 
    MessageListener, 
    OutgoingMetaProps, 
    OutgoingPayloadType, 
    RemoveMessageListener, 
    StatusListener, 
    WorldServiceClient 
} from "./appServices";

import { 
    ClientToServer, 
    ServerToClient, 
    DeepPartial 
} from "./proto/atc";

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

export function createWorldServiceClient(): WorldServiceClient {
    console.log('ENDPOINT-INIT>', `createWorldServiceEndpoint()`);

    const listenersByPayloadType = new Map<IncomingPayloadType, MessageListener[]>();
    const openStatusListeners: StatusListener[] = [];

    let nextMessageId = 1;

    const socket = new WebSocket('ws://localhost:9002/ws');
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
        tuneRadio,
    }

    function addOpenStatusListener(listener: StatusListener) {
        if (socket.readyState === socket.OPEN) {
            console.log('ENDPOINT-OPEN>', `invoking new listener`);
            listener();
        } else {
            openStatusListeners.push(listener);
        }
    }

    function addMessageListener(payloadType: IncomingPayloadType, listener: MessageListener): RemoveMessageListener {
        const existingListeners = listenersByPayloadType.get(payloadType);
        if (existingListeners) {
            existingListeners.push(listener);
        } else {
            listenersByPayloadType.set(payloadType, [listener]);
        }

        return () => {
            const currentListeners = listenersByPayloadType.get(payloadType);
            if (currentListeners) {
                const newListeners = currentListeners.filter(l => l !== listener);
                listenersByPayloadType.set(payloadType, newListeners);
            }
        };
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

    function tuneRadio(khz: number) {
        const situation = {
            "altitudeFeetMsl":0,
            "isOnGround":false,
            "heading":20,
            "groundSpeedKt":0,
            "pitch":0,
            "roll":0,
            "flapRatio":0,
            "spoilerRatio":0,
            "gearRatio":0,
            "noseWheelAngle":0,
            "landingLights":false,
            "taxiLights":false,
            "strobeLights":false,
            "monitoringFrequencyKhz":[khz],
            "transmittingFrequencyKhz":0,
            "squawk":"",
            "modeC":false,
            "modeS":false,
            "location":{"lat":32.179537,"lon":34.835333}
        };
        sendMessage({ 
            userUpdateAircraftSituation: { 
                aircraftId: 1, 
                situation,
            }
        });
    }
};
