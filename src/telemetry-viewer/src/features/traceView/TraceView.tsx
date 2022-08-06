import React, { FunctionComponent, useState } from 'react';
import { connect } from 'react-redux';

import { useAppSelector, useAppDispatch } from '../../app/hooks';
import { AppDependencyContext } from '../../AppDependencyContext';
import { LogLevel, TraceNode, TraceNodePresentation, TraceService } from '../../services/types';

import styles from './TraceView.module.css';
import { TraceViewActions, TraceNodeState, TraceViewState, RootState } from './traceViewState';

interface PureTraceNodeStateProps {
    nodeId: string;
    node: TraceNodeState;
    time: string;
    logLevel: LogLevel;
    message: string;
    values: TraceNodePresentation['values'];
    traceService: TraceService;
    selected: boolean;
}
interface PureTraceNodeDispatchProps {
    expand: () => void;
    collapse: () => void;
    select: () => void;
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
    const { 
        node, 
        message, 
        time, 
        logLevel, 
        values, 
        traceService, 
        selected, 
        select 
    } = props;

    if (!node) {
        return (<span>MISSING NODE ID {}</span>);
    }

    const trClassNames = [styles[`trace-level-${logLevel}`]];
    if (selected) {
        trClassNames.push(styles['selected']);
    }

    return (
        <>
            <tr key={node.id} id={`tr${node.id}`} className={trClassNames.join(' ')}>
                {null /* <td className={styles['trace-col-id']}>{node.id}</td>*/}
                <td className={styles['trace-col-time']}  onClick={select}>
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
        const dataNode = traceService.getCurrentView().getNodeById(BigInt(ownProps.nodeId));
        try {
            const presentation = dataNode.getPresentation();
            const selected = state.traceView.selectedNodeId === ownProps.nodeId;
            const nodeState = state.traceView.nodeById[ownProps.nodeId];
            if (!nodeState) {
                throw new Error(`state.traceView.nodeById[${ownProps.nodeId}] does not exist`);
            }

            return {
                nodeId: ownProps.nodeId,
                node: nodeState,
                time: presentation.timestamp,
                logLevel: presentation.level,
                message: presentation.messageId,
                values: presentation.values,
                traceService,
                selected,
            }
        } catch (err) {
            //console.error(`TraceService.getPresentation[node-id=${dataNode.id}}]: ${err}`);
            return {
                nodeId: ownProps.nodeId,
                node: state.traceView.nodeById[ownProps.nodeId] || { id: ownProps.nodeId, depth: 0 },
                time: 'n/a',
                logLevel: LogLevel.debug,
                message: `failed-nodeid-${dataNode.id}`,
                values: {'error': (err as any).message || err},
                selected: false,
                traceService,
            }
        }
    },
    (dispatch, ownProps) => {
        return {
            expand: () => {
                const action = TraceViewActions.nodeExpand(ownProps.nodeId);
                dispatch(action);
            },
            collapse: () => {
                const action = TraceViewActions.nodeCollapse(ownProps.nodeId);
                dispatch(action);
            },
            select: () => {
                const action = TraceViewActions.nodeSelect(ownProps.nodeId);
                dispatch(action);
                
                const nodeData = ownProps.traceService.getCurrentView().getNodeById(BigInt(ownProps.nodeId));
                const nodePresentation = nodeData.getPresentation();
                console.log(nodePresentation);
                nodeData.printBuffer();
            }
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
                <table id="tableTraceView" className={styles['trace-view']}>
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

