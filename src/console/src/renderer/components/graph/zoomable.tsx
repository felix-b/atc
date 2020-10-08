import * as React from 'react';

export interface ZoomableProps {
    zoom: number;
    data: {
        x?: number;
        y?: number;
        x1?: number;
        y1?: number;
        x2?: number;
        y2?: number;
    }
}

export function zoomable<P>(inner: React.ComponentType<P>): React.ComponentType<P & ZoomableProps> {
    const translateXY = (props: P & ZoomableProps): P => {
        const { zoom, data } = props;
        const { x, y, x1, y1, x2, y2 } = data;
        return {
            ...props,
            data: {
                ...data,
                x:  x ? x * zoom : undefined,
                y:  y ? y * zoom : undefined,
                x1: x1 ? x1 * zoom : undefined,
                y1: y1 ? y1 * zoom : undefined,
                x2: x2 ? x2 * zoom : undefined,
                y2: y2 ? y2 * zoom : undefined,
            }
        }
    };

    const Inner = inner;
    const outer: React.ComponentType<P & ZoomableProps> = (props) => {
        const translatedProps = translateXY(props);
        return (<Inner {...translatedProps} />);
    }

    return outer;
}

