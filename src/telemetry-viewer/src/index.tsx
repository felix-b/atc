import React from 'react';
import { createRoot } from 'react-dom/client';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';

import App from './App';
import reportWebVitals from './reportWebVitals';
import './index.css';
import { createTraceService } from './services/traceService';
import { createTraceViewState, TraceViewActionTypes } from './features/traceView/traceViewState';
import { AppDependencies, AppDependencyContext } from './AppDependencyContext';
import { createAppStore } from './app/store';
import { createTraceViewAPI } from './features/traceView/traceViewAPI';
import { LogLevel } from './services/types';

const container = document.getElementById('root')!;
const root = createRoot(container);

const traceService = createTraceService('ws://localhost:3003/telemetry');
const traceViewState = createTraceViewState(traceService);
let appDependencies: AppDependencies = {
    traceService,
    traceViewAPI: undefined as any,
    store: undefined as any,
};
const store = createAppStore(traceViewState, appDependencies);
const traceViewAPI = createTraceViewAPI(store, traceService);

appDependencies.store = store;
appDependencies.traceViewAPI = traceViewAPI;

traceViewState.startTraceViewUpdates(store);
traceViewAPI.addQuery(undefined, [LogLevel.error]);

/*
export const store = configureStore({
    reducer: {
        traceView: traceViewState.reducer
    },
    middleware: (getDefaultMiddleware) => getDefaultMiddleware({
        serializableCheck: {
            ignoredActions: TraceViewActionTypes.getAllActionTypes(),
            ignoredPaths: ['traceView'],
        },
        thunk: {
            extraArgument: appDependencies
        }
    }),
});

traceViewState.startTraceViewUpdates(store);
*/

root.render(
    <React.StrictMode>
        <Provider store={store}>
            <AppDependencyContext.Provider value={appDependencies}>
                <App />
            </AppDependencyContext.Provider>
        </Provider>
    </React.StrictMode>
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();
