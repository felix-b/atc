import { 
    LISTENER_ACTION_RECEIVED, 
    LISTENER_ACTION_UPDATED, 
    TraceNode, 
    TraceNodeListener, 
    TraceNodeListenerAction, 
    TraceQuery, 
    TraceTreeLayer 
} from "./types";
import { traceQueryPredicate } from "./traceQueryPredicate";

export function createTraceFilter(source: TraceTreeLayer, queries: TraceQuery[], includeNodeIds: string[]): TraceTreeLayer {

    const _nodeById = new Map<BigInt, TraceNode>();
    const _nodeListeners: TraceNodeListener[] = [];
    const _rootNode = withFilteredChildren(source.getNodeById(BigInt(0)), true)!;

    function predicate(sourceNode: TraceNode): boolean {
        if (includeNodeIds.some(id => sourceNode.id.toString() === id)) {
            return true;
        }

        return queries.some(query => traceQueryPredicate(query, sourceNode));
    }

    function withFilteredChildren(sourceNode: TraceNode, forceInclude: boolean): TraceNode | undefined {
        const sourceChildren = sourceNode.children;
        const filteredChildren: TraceNode[] = [];

        for (let i = 0 ; i < sourceChildren.length ; i++) {
            const filteredChild = withFilteredChildren(sourceChildren[i], false);
            if (filteredChild) {
                filteredChildren.push(filteredChild);
            }
        }

        if (filteredChildren.length === 0 && !predicate(sourceNode) && !forceInclude) {
            return undefined;
        }

        const filteredNode = createFilteredNode(sourceNode, filteredChildren);
        return filteredNode;
    }

    function ensureAncestorNodesIncluded(filteredChildNode: TraceNode) {
        const parentId = filteredChildNode.parentSpanId;

        if (parentId === BigInt(0) || _nodeById.has(parentId)) {
            const existingParent = _nodeById.get(parentId);
            existingParent?.addChild(filteredChildNode);
            return;
        }
        
        const sourceParentNode = source.getNodeById(parentId);
        const filteredParentNode = createFilteredNode(sourceParentNode, [filteredChildNode]);

        ensureAncestorNodesIncluded(filteredParentNode);

        invokeNodeListeners(filteredParentNode, LISTENER_ACTION_RECEIVED);
    }

    function createFilteredNode(sourceNode: TraceNode, filteredChildren: TraceNode[]): TraceNode {
        const _children = filteredChildren;
        const filteredNode: TraceNode = {
            ...sourceNode,
            children: _children,
            addChild: node => {
                _children.push(node);
            }
        };
        _nodeById.set(filteredNode.id, filteredNode);
        return filteredNode;
    }

    function invokeNodeListeners(node: TraceNode, action: TraceNodeListenerAction) {
        _nodeListeners.forEach(callback => {
            try {
                callback(node, action);
            } catch (err) {
                console.log(err);
            }
        });
    }
    
    const sourceNodeListener: TraceNodeListener = (node, action) => {
        switch (action) {
            case LISTENER_ACTION_RECEIVED:
                if (predicate(node)) {
                    const filteredNode = createFilteredNode(node, []);
                    ensureAncestorNodesIncluded(filteredNode);
                    invokeNodeListeners(filteredNode, LISTENER_ACTION_RECEIVED);
                }
                break;
            case LISTENER_ACTION_UPDATED:
                if (_nodeById.has(node.id)) {
                    const filterNode = _nodeById.get(node.id);
                    if (filterNode) {
                        invokeNodeListeners(filterNode, action);
                    }
                }
                break;
        }
    };

    source.addNodeListener(sourceNodeListener);

    return {
        getTopLevelNodes() {
            return _rootNode.children;
        },

        getNodeById(id: BigInt) {
            const node = _nodeById.get(id);
            if (node) {
                return node;
            }
            throw new Error(`TraceFilter: node id ${id} not found`);
        },

        tryGetNodeById(id: BigInt): TraceNode | undefined {
            return _nodeById.get(id);
        },

        addNodeListener(callback: TraceNodeListener) {
            _nodeListeners.push(callback);
        },

        removeNodeListener(callback: TraceNodeListener) {
            const index = _nodeListeners.indexOf(callback);
            if (index >= 0) {
                _nodeListeners.splice(index, 1);
            }
        },

        dispose() {
            source.removeNodeListener(sourceNodeListener);
        }
    }
}
