/* eslint-disable */
import _m0 from "protobufjs/minimal";

export const protobufPackage = "atc_telemetry_codepath_proto";

export interface CodePathClientToServer {
  connectRequest: CodePathClientToServer_ConnectRequest | undefined;
  disconnectRequest: CodePathClientToServer_DisconnectRequest | undefined;
}

export interface CodePathClientToServer_ConnectRequest {}

export interface CodePathClientToServer_DisconnectRequest {}

export interface CodePathServerToClient {
  connectReply: CodePathServerToClient_ConnectReply | undefined;
  telemetryBuffer: CodePathServerToClient_TelemetryBuffer | undefined;
}

export interface CodePathServerToClient_ConnectReply {
  success: boolean;
  error: string;
}

export interface CodePathServerToClient_TelemetryBuffer {
  buffer: Uint8Array;
}

function createBaseCodePathClientToServer(): CodePathClientToServer {
  return { connectRequest: undefined, disconnectRequest: undefined };
}

export const CodePathClientToServer = {
  encode(
    message: CodePathClientToServer,
    writer: _m0.Writer = _m0.Writer.create()
  ): _m0.Writer {
    if (message.connectRequest !== undefined) {
      CodePathClientToServer_ConnectRequest.encode(
        message.connectRequest,
        writer.uint32(810).fork()
      ).ldelim();
    }
    if (message.disconnectRequest !== undefined) {
      CodePathClientToServer_DisconnectRequest.encode(
        message.disconnectRequest,
        writer.uint32(818).fork()
      ).ldelim();
    }
    return writer;
  },

  decode(
    input: _m0.Reader | Uint8Array,
    length?: number
  ): CodePathClientToServer {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseCodePathClientToServer();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 101:
          message.connectRequest = CodePathClientToServer_ConnectRequest.decode(
            reader,
            reader.uint32()
          );
          break;
        case 102:
          message.disconnectRequest =
            CodePathClientToServer_DisconnectRequest.decode(
              reader,
              reader.uint32()
            );
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): CodePathClientToServer {
    return {
      connectRequest: isSet(object.connectRequest)
        ? CodePathClientToServer_ConnectRequest.fromJSON(object.connectRequest)
        : undefined,
      disconnectRequest: isSet(object.disconnectRequest)
        ? CodePathClientToServer_DisconnectRequest.fromJSON(
            object.disconnectRequest
          )
        : undefined,
    };
  },

  toJSON(message: CodePathClientToServer): unknown {
    const obj: any = {};
    message.connectRequest !== undefined &&
      (obj.connectRequest = message.connectRequest
        ? CodePathClientToServer_ConnectRequest.toJSON(message.connectRequest)
        : undefined);
    message.disconnectRequest !== undefined &&
      (obj.disconnectRequest = message.disconnectRequest
        ? CodePathClientToServer_DisconnectRequest.toJSON(
            message.disconnectRequest
          )
        : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<CodePathClientToServer>, I>>(
    object: I
  ): CodePathClientToServer {
    const message = createBaseCodePathClientToServer();
    message.connectRequest =
      object.connectRequest !== undefined && object.connectRequest !== null
        ? CodePathClientToServer_ConnectRequest.fromPartial(
            object.connectRequest
          )
        : undefined;
    message.disconnectRequest =
      object.disconnectRequest !== undefined &&
      object.disconnectRequest !== null
        ? CodePathClientToServer_DisconnectRequest.fromPartial(
            object.disconnectRequest
          )
        : undefined;
    return message;
  },
};

function createBaseCodePathClientToServer_ConnectRequest(): CodePathClientToServer_ConnectRequest {
  return {};
}

export const CodePathClientToServer_ConnectRequest = {
  encode(
    _: CodePathClientToServer_ConnectRequest,
    writer: _m0.Writer = _m0.Writer.create()
  ): _m0.Writer {
    return writer;
  },

  decode(
    input: _m0.Reader | Uint8Array,
    length?: number
  ): CodePathClientToServer_ConnectRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseCodePathClientToServer_ConnectRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(_: any): CodePathClientToServer_ConnectRequest {
    return {};
  },

  toJSON(_: CodePathClientToServer_ConnectRequest): unknown {
    const obj: any = {};
    return obj;
  },

  fromPartial<
    I extends Exact<DeepPartial<CodePathClientToServer_ConnectRequest>, I>
  >(_: I): CodePathClientToServer_ConnectRequest {
    const message = createBaseCodePathClientToServer_ConnectRequest();
    return message;
  },
};

function createBaseCodePathClientToServer_DisconnectRequest(): CodePathClientToServer_DisconnectRequest {
  return {};
}

export const CodePathClientToServer_DisconnectRequest = {
  encode(
    _: CodePathClientToServer_DisconnectRequest,
    writer: _m0.Writer = _m0.Writer.create()
  ): _m0.Writer {
    return writer;
  },

  decode(
    input: _m0.Reader | Uint8Array,
    length?: number
  ): CodePathClientToServer_DisconnectRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseCodePathClientToServer_DisconnectRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(_: any): CodePathClientToServer_DisconnectRequest {
    return {};
  },

  toJSON(_: CodePathClientToServer_DisconnectRequest): unknown {
    const obj: any = {};
    return obj;
  },

  fromPartial<
    I extends Exact<DeepPartial<CodePathClientToServer_DisconnectRequest>, I>
  >(_: I): CodePathClientToServer_DisconnectRequest {
    const message = createBaseCodePathClientToServer_DisconnectRequest();
    return message;
  },
};

function createBaseCodePathServerToClient(): CodePathServerToClient {
  return { connectReply: undefined, telemetryBuffer: undefined };
}

export const CodePathServerToClient = {
  encode(
    message: CodePathServerToClient,
    writer: _m0.Writer = _m0.Writer.create()
  ): _m0.Writer {
    if (message.connectReply !== undefined) {
      CodePathServerToClient_ConnectReply.encode(
        message.connectReply,
        writer.uint32(1610).fork()
      ).ldelim();
    }
    if (message.telemetryBuffer !== undefined) {
      CodePathServerToClient_TelemetryBuffer.encode(
        message.telemetryBuffer,
        writer.uint32(1618).fork()
      ).ldelim();
    }
    return writer;
  },

  decode(
    input: _m0.Reader | Uint8Array,
    length?: number
  ): CodePathServerToClient {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseCodePathServerToClient();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 201:
          message.connectReply = CodePathServerToClient_ConnectReply.decode(
            reader,
            reader.uint32()
          );
          break;
        case 202:
          message.telemetryBuffer =
            CodePathServerToClient_TelemetryBuffer.decode(
              reader,
              reader.uint32()
            );
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): CodePathServerToClient {
    return {
      connectReply: isSet(object.connectReply)
        ? CodePathServerToClient_ConnectReply.fromJSON(object.connectReply)
        : undefined,
      telemetryBuffer: isSet(object.telemetryBuffer)
        ? CodePathServerToClient_TelemetryBuffer.fromJSON(
            object.telemetryBuffer
          )
        : undefined,
    };
  },

  toJSON(message: CodePathServerToClient): unknown {
    const obj: any = {};
    message.connectReply !== undefined &&
      (obj.connectReply = message.connectReply
        ? CodePathServerToClient_ConnectReply.toJSON(message.connectReply)
        : undefined);
    message.telemetryBuffer !== undefined &&
      (obj.telemetryBuffer = message.telemetryBuffer
        ? CodePathServerToClient_TelemetryBuffer.toJSON(message.telemetryBuffer)
        : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<CodePathServerToClient>, I>>(
    object: I
  ): CodePathServerToClient {
    const message = createBaseCodePathServerToClient();
    message.connectReply =
      object.connectReply !== undefined && object.connectReply !== null
        ? CodePathServerToClient_ConnectReply.fromPartial(object.connectReply)
        : undefined;
    message.telemetryBuffer =
      object.telemetryBuffer !== undefined && object.telemetryBuffer !== null
        ? CodePathServerToClient_TelemetryBuffer.fromPartial(
            object.telemetryBuffer
          )
        : undefined;
    return message;
  },
};

function createBaseCodePathServerToClient_ConnectReply(): CodePathServerToClient_ConnectReply {
  return { success: false, error: "" };
}

export const CodePathServerToClient_ConnectReply = {
  encode(
    message: CodePathServerToClient_ConnectReply,
    writer: _m0.Writer = _m0.Writer.create()
  ): _m0.Writer {
    if (message.success === true) {
      writer.uint32(8).bool(message.success);
    }
    if (message.error !== "") {
      writer.uint32(18).string(message.error);
    }
    return writer;
  },

  decode(
    input: _m0.Reader | Uint8Array,
    length?: number
  ): CodePathServerToClient_ConnectReply {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseCodePathServerToClient_ConnectReply();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.success = reader.bool();
          break;
        case 2:
          message.error = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): CodePathServerToClient_ConnectReply {
    return {
      success: isSet(object.success) ? Boolean(object.success) : false,
      error: isSet(object.error) ? String(object.error) : "",
    };
  },

  toJSON(message: CodePathServerToClient_ConnectReply): unknown {
    const obj: any = {};
    message.success !== undefined && (obj.success = message.success);
    message.error !== undefined && (obj.error = message.error);
    return obj;
  },

  fromPartial<
    I extends Exact<DeepPartial<CodePathServerToClient_ConnectReply>, I>
  >(object: I): CodePathServerToClient_ConnectReply {
    const message = createBaseCodePathServerToClient_ConnectReply();
    message.success = object.success ?? false;
    message.error = object.error ?? "";
    return message;
  },
};

function createBaseCodePathServerToClient_TelemetryBuffer(): CodePathServerToClient_TelemetryBuffer {
  return { buffer: new Uint8Array() };
}

export const CodePathServerToClient_TelemetryBuffer = {
  encode(
    message: CodePathServerToClient_TelemetryBuffer,
    writer: _m0.Writer = _m0.Writer.create()
  ): _m0.Writer {
    if (message.buffer.length !== 0) {
      writer.uint32(10).bytes(message.buffer);
    }
    return writer;
  },

  decode(
    input: _m0.Reader | Uint8Array,
    length?: number
  ): CodePathServerToClient_TelemetryBuffer {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseCodePathServerToClient_TelemetryBuffer();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.buffer = reader.bytes();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): CodePathServerToClient_TelemetryBuffer {
    return {
      buffer: isSet(object.buffer)
        ? bytesFromBase64(object.buffer)
        : new Uint8Array(),
    };
  },

  toJSON(message: CodePathServerToClient_TelemetryBuffer): unknown {
    const obj: any = {};
    message.buffer !== undefined &&
      (obj.buffer = base64FromBytes(
        message.buffer !== undefined ? message.buffer : new Uint8Array()
      ));
    return obj;
  },

  fromPartial<
    I extends Exact<DeepPartial<CodePathServerToClient_TelemetryBuffer>, I>
  >(object: I): CodePathServerToClient_TelemetryBuffer {
    const message = createBaseCodePathServerToClient_TelemetryBuffer();
    message.buffer = object.buffer ?? new Uint8Array();
    return message;
  },
};

declare var self: any | undefined;
declare var window: any | undefined;
declare var global: any | undefined;
var globalThis: any = (() => {
  if (typeof globalThis !== "undefined") return globalThis;
  if (typeof self !== "undefined") return self;
  if (typeof window !== "undefined") return window;
  if (typeof global !== "undefined") return global;
  throw "Unable to locate global object";
})();

const atob: (b64: string) => string =
  globalThis.atob ||
  ((b64) => globalThis.Buffer.from(b64, "base64").toString("binary"));
function bytesFromBase64(b64: string): Uint8Array {
  const bin = atob(b64);
  const arr = new Uint8Array(bin.length);
  for (let i = 0; i < bin.length; ++i) {
    arr[i] = bin.charCodeAt(i);
  }
  return arr;
}

const btoa: (bin: string) => string =
  globalThis.btoa ||
  ((bin) => globalThis.Buffer.from(bin, "binary").toString("base64"));
function base64FromBytes(arr: Uint8Array): string {
  const bin: string[] = [];
  arr.forEach((byte) => {
    bin.push(String.fromCharCode(byte));
  });
  return btoa(bin.join(""));
}

type Builtin =
  | Date
  | Function
  | Uint8Array
  | string
  | number
  | boolean
  | undefined;

export type DeepPartial<T> = T extends Builtin
  ? T
  : T extends Array<infer U>
  ? Array<DeepPartial<U>>
  : T extends ReadonlyArray<infer U>
  ? ReadonlyArray<DeepPartial<U>>
  : T extends {}
  ? { [K in keyof T]?: DeepPartial<T[K]> }
  : Partial<T>;

type KeysOfUnion<T> = T extends T ? keyof T : never;
export type Exact<P, I extends P> = P extends Builtin
  ? P
  : P & { [K in keyof P]: Exact<P[K], I[K]> } & Record<
        Exclude<keyof I, KeysOfUnion<P>>,
        never
      >;

function isSet(value: any): boolean {
  return value !== null && value !== undefined;
}
