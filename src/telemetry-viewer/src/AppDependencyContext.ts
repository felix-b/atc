import React from 'react';
import { AppStore } from './app/store';
import { TraceViewAPI } from './features/traceView/traceViewAPI';
import { TraceService } from './services/types';

export interface AppDependencies {
    store: AppStore;
    traceService: TraceService;
    traceViewAPI: TraceViewAPI;
}

export const AppDependencyContext = React.createContext<AppDependencies>({
    store: undefined as any,
    traceService: undefined as any,
    traceViewAPI: undefined as any,
});
