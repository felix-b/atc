import * as React from 'react';
import { RootState } from '../../store';
import { connect } from 'react-redux';
import { GraphActions } from './graphState';

export interface ZoomToolOwnProps {
    graphId: string;
}

interface ZoomToolStateProps {
    graphId: string;
    zoom: number;
}

interface ZoomToolDispatchProps {
    updateZoom: (graphId: string, newZoom: number) => void;
}

const PureZoomTool: React.FunctionComponent<ZoomToolStateProps & ZoomToolDispatchProps> = ({
    graphId, zoom, updateZoom
}) => (
    <div>
        <button 
            onClick={() => updateZoom(graphId, Math.max(0.1, zoom - 0.1))}
        >[-]</button>
        <span>{parseInt((zoom * 100) as any)} %</span>
        <button 
            onClick={() => updateZoom(graphId, zoom + 0.1)}
        >[+]</button>
        <button 
            onClick={() => updateZoom(graphId, 1.0)}
        >100%</button>
    </div>
);

export const ConnectedZoomTool = connect<
    ZoomToolStateProps, 
    ZoomToolDispatchProps, 
    ZoomToolOwnProps, 
    RootState
>(
    (state, ownProps) => {
        const { graphId } = ownProps;
        const graphEntry = state.graph.graphById[graphId];
        return {
            graphId,
            zoom: graphEntry?.zoom || 1.0
        }
    },
    (dispatch) => {
        return {
            updateZoom(graphId: string, newZoom: number) {
                dispatch(GraphActions.setZoom(graphId, newZoom));
            }
        };
    }
)(PureZoomTool);
