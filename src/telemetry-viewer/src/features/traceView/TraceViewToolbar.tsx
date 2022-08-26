import { useRef } from "react";
import { connect } from "react-redux";
import { AppStore } from "../../app/store";
import { AppDependencyContext } from "../../AppDependencyContext";
import { LogLevel, TraceQuery, TraceQueryResults, TraceService } from "../../services/types";
import { RootState, TraceViewActions, TraceViewState } from "./traceViewState";

import styles from './TraceView.module.css';
import { TraceViewAPI } from "./traceViewAPI";

interface TraceViewToolbarStateProps {
    connected: TraceViewState['connected']; 
    queries: TraceViewState['queries'];
    isFilterActive: TraceViewState['isFilterActive'];
}

type TraceViewToolbarDispatchProps = TraceViewAPI;

type PureTraceToolbarProps = TraceViewToolbarStateProps & TraceViewToolbarDispatchProps;

const LogLevelLabels: Record<LogLevel, string> = {
    [LogLevel.debug]: 'Debug',
    [LogLevel.verbose]: 'Verbose',
    [LogLevel.info]: 'Info',
    [LogLevel.warning]: 'Warning',
    [LogLevel.error]: 'Error',
    [LogLevel.critical]: 'Critical',
    [LogLevel.audit]: 'Audit',
    [LogLevel.quiet]: '',
};

const getTraceQueryLabel = (query: TraceQuery): string => {
    const logLevelsLabel = query.logLevels?.map(value => LogLevelLabels[value]).join('|');
    let result = `${logLevelsLabel || ''}${query.text && logLevelsLabel ? '|' : ''}${query.text || ''}`;
    return result;
}

interface PureTraceQueryCursorProps extends TraceViewToolbarDispatchProps {
    results: TraceQueryResults;
}

const PureTraceQueryCursor = (props: PureTraceQueryCursorProps) => {
    const { results, goToResult, removeQuery } = props;
    
    return (
        <div key={results.id} className={styles['trace-toolbar-query-control']}>
            <span className={styles['trace-toolbar-query-cursor-text']}>{getTraceQueryLabel(results.query)}</span>
            <button 
                className={styles['trace-toolbar-query-cursor-btn']}
                onClick={() => goToResult(results.id, results.resultIndex - 1)} 
                disabled={results.resultIndex <= 0}
            >
                &lt; prev
            </button>
            <button 
                className={styles['trace-toolbar-query-cursor-btn']}
                onClick={() => goToResult(results.id, results.resultIndex)} 
                disabled={results.resultIndex < 0}
            >
                {results.resultIndex + 1} / {results.resultNodeIds.length}
            </button>
            <button 
                className={styles['trace-toolbar-query-cursor-btn']}
                onClick={() => goToResult(results.id, results.resultIndex + 1)} 
                disabled={results.resultIndex >= results.resultNodeIds.length - 1}
            >
                next &gt;
            </button>
            <button 
                className={styles['trace-toolbar-query-cursor-btn']}
                onClick={() => removeQuery(results.id)} 
            >
                X
            </button>
        </div>
    );
};

const PureTraceToolbar = (props: PureTraceToolbarProps) => {
    console.log('PureTraceToolbar.render');

    const { 
        connected, 
        queries, 
        isFilterActive, 
        addQuery, 
        goToResult, 
        removeQuery, 
        swtichFilter,
        connect,
        disconnect,
    } = props;

    const inputRef = useRef<HTMLInputElement>(null);
    const hasQueries = Object.keys(queries).length > 0;

    return (
        <div className={styles['trace-toolbar-container']}>
            <div>
                {!connected && <span>Inactive&nbsp;<button onClick={connect}>Connect</button></span>}
                {connected && <span>Active&nbsp;<button onClick={disconnect}>[X] Disconnect</button></span>}
            </div>
            <div>
                <input ref={inputRef} type="text" id="inputQueryText" name="inputQueryText" className={styles['trace-toolbar-query-input-text']} />
                <button onClick={() => addQuery(inputRef.current?.value || '')} className={styles['trace-toolbar-query-input-add']}>Add</button>
                {isFilterActive && <button onClick={() => swtichFilter(false)} className={styles['trace-toolbar-query-input-filteroff']}>Clear Filter</button>}
                {!isFilterActive && <button onClick={() => swtichFilter(true)} className={styles['trace-toolbar-query-input-filteron']} disabled={!hasQueries}>Apply Filter</button>}
            </div>
            {Object.values(queries).map(results => (
                <PureTraceQueryCursor key={results.id} results={results} {...props} />
            ))}
        </div>
    );
};

interface ConnectedTraceToolbarProps {
    store: AppStore;
    traceService: TraceService;
    traceViewAPI: TraceViewAPI;
};

const ConnectedTraceToolbar = connect<TraceViewToolbarStateProps, TraceViewToolbarDispatchProps, ConnectedTraceToolbarProps, RootState>(
    (state) => {
        return {
            connected: state.traceView.connected, 
            queries: state.traceView.queries,
            isFilterActive: state.traceView.isFilterActive,
        };
    },
    (dispatch, ownProps) => {
        const { traceViewAPI } = ownProps;
        return traceViewAPI;        
    }
)(PureTraceToolbar);

export const TraceToolbar = ConnectedTraceToolbar;
