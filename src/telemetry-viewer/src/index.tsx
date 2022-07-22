import React from 'react';
import { createRoot } from 'react-dom/client';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';

import App from './App';
import reportWebVitals from './reportWebVitals';
import './index.css';
import { createTraceService } from './services/traceService';
import { createTraceViewState } from './features/traceView/traceViewState';
import { AppDependencies, AppDependencyContext } from './AppDependencyContext';

const container = document.getElementById('root')!;
const root = createRoot(container);

const traceService = createTraceService('ws://localhost:3003/telemetry');
const traceViewState = createTraceViewState(traceService);

export const store = configureStore({
    reducer: {
        traceView: traceViewState.reducer
    },
    middleware: (getDefaultMiddleware) => getDefaultMiddleware({
        serializableCheck: {
            ignoredActions: ['traceView/nodeAdd', 'traceView/nodeUpdate', 'traceView/nodeExpand', 'traceView/nodeCollapse'],
            ignoredPaths: ['traceView'],
        },
    }),    
});

traceViewState.startTraceViewUpdates(store);

const appDependencies: AppDependencies = {
    traceService
};

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
