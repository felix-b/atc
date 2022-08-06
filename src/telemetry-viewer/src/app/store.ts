import { configureStore, ThunkAction, Action } from '@reduxjs/toolkit';
import { AppDependencies } from '../AppDependencyContext';
import { createTraceViewState, TraceViewActionTypes } from '../features/traceView/traceViewState';

export function createAppStore(
    traceViewState: ReturnType<typeof createTraceViewState>, 
    appDependencies: AppDependencies
) {
    return configureStore({
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
}

export type AppStore = ReturnType<typeof createAppStore>;
export type AppDispatch = AppStore['dispatch'];
export type RootState = ReturnType<AppStore['getState']>;
export type AppThunk<ReturnType = void> = ThunkAction<
    ReturnType,
    RootState,
    unknown,
    Action<string>
>;
