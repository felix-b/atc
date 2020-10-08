import * as React from 'react';
import { GraphProps } from '../../components/graph/graph';
import { AIRPORT_MARKER_TYPE_PINPOINT, AIRPORT_MARKER_TYPE_AIRCRAFT } from './airportGraphData';

require('./AirportView.scss');
const airplanePng = require('../../../icons/airplane.png');


const PinPointMarker: React.FunctionComponent = () => (
    <div className="pinpoint-marker"></div>
);

const AircraftMarker: React.FunctionComponent<{ 
    zoom: number; 
    rotateDegrees?: number; 
}> = ({
    zoom, rotateDegrees
}) => {
    const iconSize = 28 * zoom;
    const iconSizeInt = parseInt(iconSize.toString());
    
    return (
        <img className="aircraft-marker"
            src={airplanePng} 
            style={{
                width:`${iconSizeInt}px`, 
                height:`${iconSizeInt}px`, 
                marginTop: `${-iconSize/2}px`, 
                marginLeft: `${-iconSize/2}px`, 
                transform: `rotate(${rotateDegrees || 0}deg)`
            }} 
        />
    );
};

export const renderAirportMarker: GraphProps['renderMarker'] = (marker, zoom) => {
    switch (marker.type) {
        case AIRPORT_MARKER_TYPE_PINPOINT:
            return (<PinPointMarker />);
        case AIRPORT_MARKER_TYPE_AIRCRAFT:
            return (<AircraftMarker zoom={zoom} rotateDegrees={marker.rotateDegrees} />);
        default:
            return null;
    } 
}

