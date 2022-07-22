import React, { FunctionComponent, useState } from 'react';
import { connect } from 'react-redux';

import { useAppSelector, useAppDispatch } from '../../app/hooks';
import { AppDependencyContext } from '../../AppDependencyContext';
import { LogLevel, TraceNode, TraceNodePresentation, TraceService } from '../../services/traceService';

import styles from './TraceView.module.css';
import { TraceNodeActions, TraceNodeState, TraceViewState } from './traceViewState';

interface PureTraceNodeStateProps {
    node: TraceNodeState;
    time: string;
    logLevel: LogLevel;
    message: string;
    values: TraceNodePresentation['values'];
    traceService: TraceService;
}
interface PureTraceNodeDispatchProps {
    expand: () => void;
    collapse: () => void;
}

interface RootState {
    traceView: TraceViewState;
}

type PureTraceNodeProps = PureTraceNodeStateProps & PureTraceNodeDispatchProps;

const PureTraceNodeExpander: FunctionComponent<PureTraceNodeProps> = (props) => {
    const { node, expand, collapse } = props;

    if (!node.isExpandable) {
        return <span className={styles['trace-node-noexpander']} />;
    }
    else if (!node.isExpanded) {
        return (
            <a className={`${styles['trace-node-expander']} ${styles['collapsed']}`} onClick={expand}>
                [+]
            </a>
        );
    } 
    else {
        return (
            <a className={`${styles['trace-node-expander']} ${styles['expanded']}`} onClick={collapse}>
                [-]
            </a>
        );
    }
};

const PureTraceNodeValuesCell: FunctionComponent<{ values: TraceNodePresentation['values'] }> = (props) => {
    
    const { values } = props;

    return (
        <>
            {Object.keys(values).map((key, index) => (
                <span key={`kv-${index}`}>
                    <span className={styles['trace-node-value-key']}>{key}=</span>
                    <span className={styles['trace-node-value']}>{values[key]}</span>
                </span>
            ))}
        </>
    );
};

const PureTraceNode: FunctionComponent<PureTraceNodeProps> = (props) => {
    const { node, message, time, logLevel, values, traceService } = props;

    return (
        <>
            <tr key={node.id} className={styles[`trace-level-${logLevel}`]}>
                {null /* <td className={styles['trace-col-id']}>{node.id}</td>*/}
                <td className={styles['trace-col-time']}>
                    {time.substring(0, 6)}
                    <span className={styles['time-sec-ms']}>
                        {time.substring(6)}
                    </span>
                </td>
                <td className={`${styles['trace-col-message']} ${styles[`trace-depth${node.depth}`]}`}>
                    <PureTraceNodeExpander {...props} />
                    {message}
                </td>
                <td className={styles['trace-col-duration']}></td>
                <td className={styles['trace-col-values']}>
                    <PureTraceNodeValuesCell values={values} />
                </td>
            </tr>
            {node.childNodeIds
                ? node.childNodeIds.map(id => <ConnectedTraceNode key={id} nodeId={id} traceService={traceService} />)
                : null}
        </>
    );
}

interface ConnectedTraceNodeProps {
    nodeId: string;
    traceService: TraceService;
};

const ConnectedTraceNode = connect<PureTraceNodeStateProps, PureTraceNodeDispatchProps, ConnectedTraceNodeProps, RootState>(
    (state, ownProps) => {
        const { traceService } = ownProps;
        const dataNode = traceService.getNodeById(BigInt(ownProps.nodeId));
        try {
            const presentation = dataNode.getPresentation();
            
            return {
                node: state.traceView.nodeById[ownProps.nodeId],
                time: presentation.timestamp,
                logLevel: presentation.level,
                message: presentation.messageId,
                values: presentation.values,
                traceService,
            }
        } catch (err) {
            //console.error(`TraceService.getPresentation[node-id=${dataNode.id}}]: ${err}`);
            return {
                node: state.traceView.nodeById[ownProps.nodeId],
                time: 'n/a',
                logLevel: LogLevel.debug,
                message: `failed-nodeid-${dataNode.id}`,
                values: {},
                traceService,
            }
        }
    },
    (dispatch, state) => {
        return {
            expand: () => {
                const action = TraceNodeActions.nodeExpand(state.nodeId);
                dispatch(action);
            },
            collapse: () => {
                const action = TraceNodeActions.nodeCollapse(state.nodeId);
                dispatch(action);
            },
        }
    }
)(PureTraceNode);


interface PureTraceViewProps {
    topLevelNodeIds: string[]
}

const PureTraceView = (props: PureTraceViewProps) => {
    //console.log('PureTraceView.render');
    const { topLevelNodeIds } = props;

    // const topLevelNodes = useAppSelector(traceViewSelectors.topLevelNodes);
    // const dispatch = useAppDispatch();
    // //const [incrementAmount, setIncrementAmount] = useState('2');

    return (
        <AppDependencyContext.Consumer>
            {dependencies => (
                <table className={styles['trace-view']}>
                    <thead>
                        <tr>
                            {null /*<th>Id</th>*/}
                            <th>Time</th>
                            <th>Message</th>
                            <th>Duration</th>
                            <th>Values</th>
                        </tr>
                    </thead>
                    <tbody>
                        {topLevelNodeIds.map(id => (
                            <ConnectedTraceNode key={id} nodeId={id} traceService={dependencies.traceService} />
                        ))}
                    </tbody>
                </table>
            )}
        </AppDependencyContext.Consumer>
    );
}

const ConnectedTraceView = connect<PureTraceViewProps, {}, {}, RootState>(
    (state) => {
        return {
            topLevelNodeIds: state.traceView.nodeById['0']?.childNodeIds || []
        };
    }
)(PureTraceView);

export const TraceView = ConnectedTraceView;
