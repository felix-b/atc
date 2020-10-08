import * as React from 'react';
import { AirportService } from './airportService';

export const PinPointTool: React.FunctionComponent = () => {
    const latInputRef = React.useRef<HTMLInputElement>(null);
    const lonInputRef = React.useRef<HTMLInputElement>(null);
    return (
        <div>
            <label>lat</label>
            <input ref={latInputRef} type="text" name="findLat" />
            <label>lon</label>
            <input ref={lonInputRef} type="text" name="findLon" />
            <button 
                onClick={() => {
                    try {
                        const lat = parseFloat(latInputRef.current?.value || '');
                        const lon = parseFloat(lonInputRef.current?.value || '');
                        AirportService.setPinpoint(lat, lon);
                    } catch {
                    }
                }}
            >Find</button>
        </div>
    );
};
