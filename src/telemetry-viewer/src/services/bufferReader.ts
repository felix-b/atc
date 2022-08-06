
export interface BufferReader {
    readUint8(): number;
    readInt8(): number;
    readInt32(): number;
    readInt64(): BigInt;
    readUint64(): BigInt;
    readUtf8String(byteLength: number): string;
    readLengthPrefixedUtf8String(): string;
    rewindInt8(): void;
    printBuffer(): void;
    isEndOfBuffer: boolean;
}

export function createBufferReader(dataView: DataView): BufferReader {

    let _offset = 0;

    function readUint8(): number {
        const result = dataView.getUint8(_offset);
        _offset += 1;
        return result;
    }

    function readInt8(): number {
        const result = dataView.getInt8(_offset);
        _offset += 1;
        return result;
    }

    function readInt32(): number {
        const result = dataView.getInt32(_offset, true);
        _offset += 4;
        return result;
    }
        
    function readInt64(): BigInt {
        const result = dataView.getBigInt64(_offset, true);
        _offset += 8;
        return result;
    }

    function readUint64(): BigInt {
        const result = dataView.getBigUint64(_offset, true);
        _offset += 8;
        return result;
    }

    function readUtf8String(byteLength: number): string {
        const charCodes = [];
        for (let i = 0 ; i < byteLength ; i++) {
            charCodes.push(dataView.getUint8(_offset + i));
        }
        _offset += byteLength;
        const value = String.fromCharCode(...charCodes);
        return value;
    }

    function readLengthPrefixedUtf8String(): string {
        const byteLength = readUint8();
        return readUtf8String(byteLength);
    }

    function rewindInt8() {
        _offset -= 1;
    }

    function printBuffer() {
        let printOffset = 0;
        let printLine = '';

        while (printOffset < dataView.byteLength) {
            const byte = dataView.getUint8(printOffset);
            printOffset++;
            
            printLine += `${byte.toString(16).padStart(2,'0')} `;
        
            if ((printOffset % 16) === 0) {
                console.log(printLine);
                printLine = '';
            } else if ((printOffset % 8) === 0) {
                printLine += '   ';
            } 
        }

        console.log(printLine);
    }

    return {
        readUint8,
        readInt8,
        readInt32,
        readInt64,
        readUint64,
        readUtf8String,
        readLengthPrefixedUtf8String,
        rewindInt8,
        printBuffer,
        
        get isEndOfBuffer() {
            return _offset >= dataView.byteLength;
        }
    };

}

//TODO: use code from https://stackoverflow.com/questions/17191945/conversion-between-utf-8-arraybuffer-and-string
/*
function utf8ArrayToString(aBytes) {
    var sView = "";
    
    for (var nPart, nLen = aBytes.length, nIdx = 0; nIdx < nLen; nIdx++) {
        nPart = aBytes[nIdx];
        
        sView += String.fromCharCode(
            nPart > 251 && nPart < 254 && nIdx + 5 < nLen ? // six bytes 
                // (nPart - 252 << 30) may be not so safe in ECMAScript! So...: 
                (nPart - 252) * 1073741824 + (aBytes[++nIdx] - 128 << 24) + (aBytes[++nIdx] - 128 << 18) + (aBytes[++nIdx] - 128 << 12) + (aBytes[++nIdx] - 128 << 6) + aBytes[++nIdx] - 128
            : nPart > 247 && nPart < 252 && nIdx + 4 < nLen ? // five bytes 
                (nPart - 248 << 24) + (aBytes[++nIdx] - 128 << 18) + (aBytes[++nIdx] - 128 << 12) + (aBytes[++nIdx] - 128 << 6) + aBytes[++nIdx] - 128
            : nPart > 239 && nPart < 248 && nIdx + 3 < nLen ? // four bytes
                (nPart - 240 << 18) + (aBytes[++nIdx] - 128 << 12) + (aBytes[++nIdx] - 128 << 6) + aBytes[++nIdx] - 128
            : nPart > 223 && nPart < 240 && nIdx + 2 < nLen ? // three bytes 
                (nPart - 224 << 12) + (aBytes[++nIdx] - 128 << 6) + aBytes[++nIdx] - 128
            : nPart > 191 && nPart < 224 && nIdx + 1 < nLen ? // two bytes
                (nPart - 192 << 6) + aBytes[++nIdx] - 128
            : // nPart < 127 ?  //one byte
                nPart
        );
    }
    return sView;
}
let str = utf8ArrayToString([50,72,226,130,130,32,43,32,79,226,130,130,32,226,135,140,32,50,72,226,130,130,79]);
console.log(str);        
*/

