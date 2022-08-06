import { TraceNode, TraceQuery } from "./types";

export function traceQueryPredicate(query: TraceQuery, node: TraceNode): boolean {
    if (node.id === BigInt(0)) {
        return true;
    }
    
    const { messageId, values, timestamp, error, level } = node.getPresentation();
    const { text, logLevels } = query;

    if (logLevels && logLevels.indexOf(level) < 0) {
        return false;
    }

    if (!text) {
        return true;
    }
    if (messageId.indexOf(text) >= 0) {
        return true;
    }
    if (timestamp.indexOf(text) >= 0) {
        return true;
    }
    if (Object.keys(values).some(key => key.indexOf(text) >= 0 || values[key].indexOf(text) >= 0)) {
        return true;
    }
    if (error && error.indexOf(text) >= 0) {
        return true;
    }

    return false;
}
