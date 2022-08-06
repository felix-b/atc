import { TraceNode, TraceNodeListener, TraceQuery, TraceQueryObserver, TraceQueryResultCallback, TraceQueryResults, TraceService, TraceViewChangedListener } from "./types";
import { traceQueryPredicate } from "./traceQueryPredicate";

export function createTraceQueryObserver(
    traceService: TraceService, 
    onResults: TraceQueryResultCallback, 
    id: number, 
    query: TraceQuery
): TraceQueryObserver {

    const _resultNodeIds: string[] = [];
    let _queryWasRun = false;

    const onNodeNotification: TraceNodeListener = (node, action) => {
        if (action === 'received' && traceQueryPredicate(query, node)) {
            _resultNodeIds.push(node.id.toString());
            onResults({
                id,
                query,
                resultNodeIds: _resultNodeIds,
                resultIndex: -1            
            })
        }
    }

    const onViewChanged: TraceViewChangedListener = newView => {
        newView.addNodeListener(onNodeNotification);
    };

    traceService.addViewChangedListener(onViewChanged); // invokes the callback here

    return {
        id,
        runQuery,
        dispose: () => {
            traceService.getCurrentView().removeNodeListener(onNodeNotification);
            traceService.removeViewChangedListener(onViewChanged);
        }
    };

    function runQuery(): TraceQueryResults {
        if (!_queryWasRun) {
            const topLevelNodes = traceService.getCurrentView().getTopLevelNodes();
            topLevelNodes.forEach(node => searchNode(node));
        }
        return {
            id,
            query,
            resultNodeIds: _resultNodeIds,
            resultIndex: -1
        };
    }

    function searchNode(node: TraceNode) {
        if (traceQueryPredicate(query, node)) {
            _resultNodeIds.push(node.id.toString());
        }
        node.children?.forEach(childNode => searchNode(childNode));
    }
}
