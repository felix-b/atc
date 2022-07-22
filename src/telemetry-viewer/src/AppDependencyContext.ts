import React from 'react';
import { TraceService } from './services/traceService';

export interface AppDependencies {
    traceService: TraceService
}

export const AppDependencyContext = React.createContext<AppDependencies>({
    traceService: undefined as any
});
