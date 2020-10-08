import { AnyAction, Reducer, Action } from 'redux';
import { World } from '../../../proto';

export interface AirportState {
    static: World.Airport;
    taxiPath?: World.TaxiPath;
    taxiTool: TaxiToolState;
}

export interface TaxiToolState {
    fromPoint?: World.GeoPoint;
    fromLabel?: string;
    toPoint?: World.GeoPoint;
    toLabel?: string;
    pickingFrom?: boolean;
    pickingTo?: boolean;
}

const emptyAirport: World.Airport = {
    icao: '',
    location: { lat: 0, lon: 0 },
    runways: [],
    parkingStands: [],
    taxiNodes: [],
    taxiEdges: []
};

const defaultState: AirportState = {
    static: emptyAirport,
    taxiTool: {
        pickingFrom: true
    }
};

export interface AirportLoadedAction extends Action {
    type: 'airport/airportLoaded';
    airport: World.Airport;
}

export interface TaxiPathLoadedAction extends Action {
    type: 'airport/taxiPathLoaded';
    taxiPath: World.TaxiPath;
}

export interface TaxiToolAssignAction extends Action {
    type: 'airport/taxiToolAssign';
    assign: Partial<TaxiToolState>;
}

export const AirportActions = {
    airportLoaded(airport: World.Airport): AirportLoadedAction {
        return ({
            type: 'airport/airportLoaded',
            airport
        });
    },
    taxiPathLoaded(taxiPath: World.TaxiPath): TaxiPathLoadedAction {
        return ({
            type: 'airport/taxiPathLoaded',
            taxiPath
        });
    },
    taxiToolAssign(assign: Partial<TaxiToolState>): TaxiToolAssignAction {
        return ({
            type: 'airport/taxiToolAssign',
            assign
        });
    }
};

export const airportReducer: Reducer<AirportState> = (state = defaultState, action: AnyAction): AirportState => {
    switch (action.type) {
        case 'airport/airportLoaded': 
            const airportLoadedAction = action as AirportLoadedAction;
            return {
                ...state,
                static: airportLoadedAction.airport
            };
        case 'airport/taxiPathLoaded': 
            const taxiPathLoadedAction = action as TaxiPathLoadedAction;
            return {
                ...state,
                taxiPath: taxiPathLoadedAction.taxiPath
            };
        case 'airport/taxiToolAssign': 
            const taxiToolAssignAction = action as TaxiToolAssignAction;
            return {
                ...state,
                taxiTool: {
                    ...(state.taxiTool || {}),
                    ...taxiToolAssignAction.assign
                }
            };
        default:
            return state;
    }
}
