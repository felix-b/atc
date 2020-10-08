import * as React from 'react';
import { AirportService } from './airportService';

export const AirportLoadTool: React.FunctionComponent = () => {
    const icaoInputRef = React.useRef<HTMLInputElement>(null);
    return (
        <div>
            <label>Airport ICAO code</label>
            <input ref={icaoInputRef} type="text" name="icao" />
            <button 
                onClick={() => {
                    try {
                        const icao = icaoInputRef.current?.value || '';
                        AirportService.beginQueryAirport(icao);
                    } catch {
                    }
                }}
            >Load</button>
        </div>
    );
};
