import * as React from 'react';
import { AirportService } from './airportService';
import { TaxiToolState, AirportActions } from './airportState';
import { connect } from 'react-redux';
import { RootState } from '../../store';

type TaxiToolStateProps = TaxiToolState;
type TaxiToolDispatchProps = {
    pickFromPressed(): void;
    pickToPressed(): void;
    beginQueryTaxiPath(
        fromLatInput: string | undefined,
        fromLonInput: string | undefined,
        toLatInput: string | undefined,
        toLonInput: string | undefined,
    ): void;
}

const TaxiToolPure: React.FunctionComponent<TaxiToolStateProps & TaxiToolDispatchProps> = (props) => {
    const fromLatInputRef = React.useRef<HTMLInputElement>(null);
    const fromLonInputRef = React.useRef<HTMLInputElement>(null);
    const toLatInputRef = React.useRef<HTMLInputElement>(null);
    const toLonInputRef = React.useRef<HTMLInputElement>(null);

    return (
        <div className="taxi-tool-grid">
            <div className="header-lat">lat</div>
            <div className="header-lon">lon</div>
            <div className="header-from">
                <button className={props.pickingFrom ? 'pressed' : ''} onClick={props.pickFromPressed}>
                    From
                </button>
            </div>
            <div className="header-to">
                <button className={props.pickingTo ? 'pressed' : ''} onClick={props.pickToPressed}>
                    To
                </button>
            </div>
            <div className="input-lat-from">
                <input ref={fromLatInputRef} type="text" name="fromLat" defaultValue={props.fromPoint?.lat || ''} />
            </div>
            <div className="input-lon-from">
                <input ref={fromLonInputRef} type="text" name="fromLon" defaultValue={props.fromPoint?.lon || ''} />
            </div>
            <div className="input-lat-to">
                <input ref={toLatInputRef} type="text" name="toLat" defaultValue={props.toPoint?.lat || ''} />
            </div>
            <div className="input-lon-to">
                <input ref={toLonInputRef} type="text" name="toLon" defaultValue={props.toPoint?.lon || ''} />
            </div>
            <div className="text-from"></div>
            <div className="text-to"></div>
            <div className="footer">
                <button onClick={() => props.beginQueryTaxiPath(
                    fromLatInputRef.current?.value,
                    fromLonInputRef.current?.value,
                    toLatInputRef.current?.value,
                    toLonInputRef.current?.value,
                )}>
                    Find Taxi Path
                </button>
            </div>
        </div>      
    );
};

export const TaxiTool = connect<TaxiToolStateProps, TaxiToolDispatchProps, {}, RootState>(
    state => state.airport.taxiTool,
    dispatch => ({
        pickFromPressed() {
            dispatch(AirportActions.taxiToolAssign({ pickingFrom: true, pickingTo: false }));
        },
        pickToPressed() {
            dispatch(AirportActions.taxiToolAssign({ pickingTo: true, pickingFrom: false }));
        },
        beginQueryTaxiPath(fromLatInput, fromLonInput, toLatInput, toLonInput) {
            const fromLat = parseFloat(fromLatInput || '');
            const fromLon = parseFloat(fromLonInput || '');
            const toLat = parseFloat(toLatInput || '');
            const toLon = parseFloat(toLonInput || '');
            if (!isNaN(fromLat) && !isNaN(fromLon) && !isNaN(toLat) && !isNaN(toLon)) {
                AirportService.beginQueryTaxiPath(
                    { lat: fromLat, lon: fromLon }, 
                    { lat: toLat, lon: toLon }
                );
            }
        }
    })
)(TaxiToolPure);

/*
            <div>
                <label>From:</label>
                <label>lat</label>
                <input ref={fromLatInputRef} type="text" name="fromLat" />
                <label>lon</label>
                <input ref={fromLonInputRef} type="text" name="fromLon" />
                <button 
                    onClick={() => {
                        try {
                            const lat = parseFloat(latInputRef.current?.value || '');
                            const lon = parseFloat(lonInputRef.current?.value || '');
                            AirportService.setPinpoint(lat, lon);
                        } catch {
                        }
                    }}
                >@</button>
            </div>
            <div>
                <label>From:</label>
                <label>lat</label>
                <input ref={fromLatInputRef} type="text" name="fromLat" />
                <label>lon</label>
                <input ref={fromLonInputRef} type="text" name="fromLon" />
                <button 
                    onClick={() => {
                        try {
                            const lat = parseFloat(latInputRef.current?.value || '');
                            const lon = parseFloat(lonInputRef.current?.value || '');
                            AirportService.setPinpoint(lat, lon);
                        } catch {
                        }
                    }}
                >@</button>
            </div>
            <div>
                <label>From:</label>
                <label>lat</label>
                <input ref={fromLatInputRef} type="text" name="fromLat" />
                <label>lon</label>
                <input ref={fromLonInputRef} type="text" name="fromLon" />
                <button 
                    onClick={() => {
                        try {
                            const lat = parseFloat(latInputRef.current?.value || '');
                            const lon = parseFloat(lonInputRef.current?.value || '');
                            AirportService.setPinpoint(lat, lon);
                        } catch {
                        }
                    }}
                >@</button>
            </div>
        </>
    );
};
*/