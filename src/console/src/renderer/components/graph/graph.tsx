import * as React from 'react';
import { LayerData, EdgeData, VertexData, MarkerData, SourceRefData } from './graphData';
import { zoomable } from './zoomable';
import { connect } from 'react-redux';
import { RootState } from '../../store';
import { ToolPanelService } from '../toolPanel/toolPanelService';
import { ConnectedZoomTool } from './zoomTool';
import { ViewPort } from './graphState';

require('./graph.scss');

export interface GraphProps {
    graphId: string;
    layerIds: string[];
    renderMarker?: (marker: MarkerData, zoom: number) => JSX.Element | null;
    onClick?: GraphClickEventHandler;
}

export interface GraphClickEventData {
    sourceRef?: SourceRefData;
    edge?: EdgeData;
    vertex?: VertexData;
    marker?: MarkerData;
    dataX?: number;
    dataY?: number;
    pageX: number;
    pageY: number;
    button: number;
}

export type GraphClickEventHandler = (event: GraphClickEventData) => void;

export interface LayerOwnProps {
    graphId: string;
    layerId: string;
    zIndex: number;
    renderMarker: GraphProps['renderMarker'];
    onClick?: GraphClickEventHandler;
}

export interface MarkerOwnProps {
    renderMarker: GraphProps['renderMarker'];
    onClick?: GraphClickEventHandler;
}

interface PureLayerProps {
    data: LayerData;
    zoom: number;
    zIndex: number;
    renderMarker: GraphProps['renderMarker'];
    onClick?: GraphClickEventHandler;
}

interface  PureEdgeProps {
    data: EdgeData;
    onClick?: GraphClickEventHandler;
}

interface PureVertexProps {
    data: VertexData;
    onClick?: GraphClickEventHandler;
}

interface PureMarkerProps {
    data: MarkerData;
    zoom: number;
    zIndex: number;
    renderMarker: GraphProps['renderMarker'];
    onClick?: GraphClickEventHandler;
}

const PureEdge: React.FunctionComponent<PureEdgeProps> = ({
    data, onClick
}) => {
    const { tooltip, type, name, id, x1, x2, y1, y2 } = data;
    const centerX = x1 + (x2-x1)/2;
    const centerY = y1 + (y2-y1)/2;
    const length = Math.sqrt(Math.pow(x2-x1, 2) + Math.pow(y2-y1, 2));
    const left = centerX - length/2;
    const top = centerY;
    const angle = x1 !== x2 ? Math.atan((y2-y1)/(x2-x1)) : 0;

    return <div 
        className={`edge ${type}`} 
        style={{
            top: `${top}px`,
            left: `${left}px`,
            width: `${length}px`,
            transform: `rotate(${angle}rad)`
        }}
        title={tooltip || `[${id}] ${name}`}
        onClick={e => onClick && onClick({
            ...mouseEventToGraphClick(e),
            edge: data,
            sourceRef: data.sourceRef
        })}
    />;
};

const PureVertex: React.FunctionComponent<PureVertexProps> = ({ 
    data, onClick
}) => {
    const { x, y, name, type, id, tooltip, rotateDegrees } = data;
    return (
        <div 
            className={`vertex ${type}`} 
            style={{
                top: `${y}px`,
                left: `${x}px`,
                transform: rotateDegrees ? `rotate(${rotateDegrees}deg)` : undefined
            }}
            title={tooltip || `[${id}] ${name}`}
            onClick={e => onClick && onClick({
                ...mouseEventToGraphClick(e),
                vertex: data,
                sourceRef: data.sourceRef
            })}
        />
    );
};

const PureMarker: React.FunctionComponent<PureMarkerProps> = (props) => { 
    const { renderMarker, onClick, zoom, zIndex, data } = props;
    const { id, x, y, rotateDegrees, tooltip } = data;
    
    return (
        <div 
            style={{
                top: `${y}px`,
                left: `${x}px`,
                position: 'relative',
                zIndex,
            }}
            title={tooltip || `[${id}]`}
            onClick={e => onClick && onClick({
                ...mouseEventToGraphClick(e),
                marker: data,
                sourceRef: data.sourceRef
            })}
        >
            {renderMarker && renderMarker(data, zoom)}
        </div>
    );
};

const ZoomableVertex = zoomable(PureVertex);
const ZoomableEdge = zoomable(PureEdge);
const ZoomableMarker = zoomable(PureMarker);

const PureLayer: React.FunctionComponent<PureLayerProps> = ({ 
    data, zoom, zIndex, renderMarker, onClick
}) => {
    const { edges, vertices, markers } = data;
    return (
        <div className="graph-layer" z-index={zIndex}>
            {edges.map(e => <ZoomableEdge 
                key={`e_${e.id}_${e.type}`} 
                zoom={zoom} 
                onClick={onClick}
                data={e}
            />)}
            {vertices.map(v => <ZoomableVertex 
                key={`v_${v.id}_${v.type}`} 
                zoom={zoom} 
                onClick={onClick}
                data={v} 
            />)}
            {markers.map(m => <ZoomableMarker
                key={`m_${m.id}_${m.type}`} 
                zoom={zoom} 
                zIndex={zIndex}
                renderMarker={renderMarker}
                onClick={onClick}
                data={m} 
            />)}
        </div>
    );
};

const createEmptyLayerData = (id: string): LayerData => ({
    id,
    title: '',
    vertices: [],
    edges: [],
    markers: []
});

const createDefaultViewPort = (): ViewPort => ({
    top: 0,
    left: 0,
    scale: 1,
});

const ConnectedLayer = connect<PureLayerProps, {}, LayerOwnProps, RootState>(
    ({graph}, {graphId, layerId, zIndex, renderMarker, onClick}) => {
        const graphEntry = graph.graphById[graphId];
        const layerData = graphEntry?.data.layers.find(l => l.id === layerId);
        const viewPort = graphEntry?.viewPort || createDefaultViewPort();

        const enrichedOnClick: GraphClickEventHandler | undefined = onClick
            ? (event) => onClick(enrichGraphClickEvent(event, viewPort))
            : undefined;

        return {
            data: layerData || createEmptyLayerData(layerId),
            zoom: graphEntry?.zoom || 1.0,
            zIndex,
            renderMarker,
            enrichedOnClick,
        };
    }
)(PureLayer);

export const Graph: React.FunctionComponent<GraphProps> = (
    { graphId, layerIds, renderMarker, onClick }
) => {
    React.useEffect(() => ToolPanelService.attachToolToLifecycle({
        id: `graph[${graphId}]/zoom`,
        title: 'Zoom',
        render: () => <ConnectedZoomTool graphId={graphId} />
    }), []);
        
    return (
        <div className="graph">
            {layerIds.map((layerId, index) => <ConnectedLayer 
                key={layerId}
                graphId={graphId}
                layerId={layerId}
                zIndex={index * 100}
                renderMarker={renderMarker}
                onClick={onClick}
            />)}
        </div>
    );
};

function mouseEventToGraphClick<T, E>(source: React.MouseEvent<T, E>): GraphClickEventData {
    return {
        pageX: source.pageX,
        pageY: source.pageY,
        button: source.button,
    };
}

function enrichGraphClickEvent(event: GraphClickEventData, viewPort: ViewPort): GraphClickEventData {
    const getXY = (): { x: number; y: number; } => {
        return (
            event.marker || 
            event.vertex || 
            { x: event.pageX, y: event.pageY }
        );
    }

    const { x, y } = getXY();
    const enrichedEvent = {
        ...event,
        dataX: x / viewPort.scale + viewPort.left,
        dataY: y / viewPort.scale + viewPort.top,
    };

    return enrichedEvent;
};
