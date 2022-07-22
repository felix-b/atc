import { configureStore, ThunkAction, Action } from '@reduxjs/toolkit';
import counterReducer from '../features/counter/counterSlice';
//import { traceViewSlice } from '../features/traceView/traceViewSlice';

export const store = configureStore({
    reducer: {
        counter: counterReducer,
        //traceView: traceViewSlice.reducer
    },
    middleware: (getDefaultMiddleware) => getDefaultMiddleware({
        serializableCheck: {
            // Ignore these action types
            ignoredActions: ['traceView/nodeAdded', 'traceView/nodeUpdated', 'traceView/nodeExpanded', 'traceView/nodeCollapsed'],
            // Ignore these field paths in all actions
            //ignoredActionPaths: ['meta.arg', 'payload.timestamp'],
            // Ignore these paths in the state
            ignoredPaths: ['traceView'],
        },
    }),    
});

export type AppDispatch = typeof store.dispatch;
export type RootState = ReturnType<typeof store.getState>;
export type AppThunk<ReturnType = void> = ThunkAction<
    ReturnType,
    RootState,
    unknown,
    Action<string>
>;
