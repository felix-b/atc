import * as React from 'react';
import { Graph, GraphClickEventHandler } from '../../components/graph/graph';
import { AIRPORT_GRAPH_ID, AIRPORT_GRAPH_LAYER_IDS } from './airportGraphData';
import { renderAirportMarker } from './airportMarkers';
import { ToolPanelService } from '../../components/toolPanel/toolPanelService';
import { PinPointTool } from './pinpointTool';
import { TaxiTool } from './taxiTool';
import { AirportLoadTool } from './airportLoadTool';
import { connect } from 'react-redux';
import { AirportService } from './airportService';
import { RootState } from '../../store';

require('./AirportView.scss');

export const AirportView: React.FunctionComponent = () => {
    React.useEffect(() => {
        const airportLoadToolId = ToolPanelService.insertTool({
            id: 'airport/load',
            title: 'Load airport',
            render: () => <AirportLoadTool />
        });
        const pinpointToolId = ToolPanelService.insertTool({
            id: 'airport/pinpoint',
            title: 'Find point',
            render: () => <PinPointTool />
        });
        const taxiToolId = ToolPanelService.insertTool({
            id: 'airport/taxi',
            title: 'Find taxi path',
            render: () => <TaxiTool />
        });
        return () => {
            ToolPanelService.removeTool(airportLoadToolId);
            ToolPanelService.removeTool(pinpointToolId);
            ToolPanelService.removeTool(taxiToolId);
        }
    }, []);

    return (
        <div className="airport">
            <Graph 
                graphId={AIRPORT_GRAPH_ID}
                layerIds={AIRPORT_GRAPH_LAYER_IDS}
                renderMarker={renderAirportMarker}
                onClick={e => AirportService.notifyAirportElementClicked(e)}
            />
        </div>
    );
};
