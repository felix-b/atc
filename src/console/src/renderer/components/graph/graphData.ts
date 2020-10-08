
export interface GraphData {
    id: string;
    title: string;
    layers: LayerData[];
}

export interface LayerData {
    id: string;
    title: string;
    vertices: VertexData[];
    edges: EdgeData[];
    markers: MarkerData[];
}

export interface VertexData {
    id: string;
    name: string;
    type: string;
    x: number;
    y: number;
    rotateDegrees?: number;
    tooltip?: string | null;
    sourceRef?: SourceRefData;
}

export interface EdgeData {
    id: string;
    name: string;
    type: string;
    x1: number;
    y1: number;
    x2: number;
    y2: number;
    tooltip?: string | null;
    sourceRef?: SourceRefData;
}

export interface MarkerData {
    type: string;
    id: string;
    x: number;
    y: number;
    rotateDegrees?: number;
    tooltip?: string | null;
    sourceRef?: SourceRefData;
}

export interface SourceRefData {
    type: string;
    id: string | number;
}
