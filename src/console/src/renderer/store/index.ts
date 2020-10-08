import { applyMiddleware, combineReducers, createStore, Store } from 'redux';
import { composeWithDevTools } from 'redux-devtools-extension';

import { GraphStoreState, graphReducer } from '../components/graph/graphState';
import { AirportState, airportReducer } from '../views/airport/airportState';
import { ToolPanelState, toolPanelReducer } from '../components/toolPanel/toolPanelState';

export interface RootState {
    airport: AirportState;
    graph: GraphStoreState;
    tools: ToolPanelState;
}

export const rootReducer = combineReducers<RootState | undefined>({
    airport: airportReducer,
    graph: graphReducer,
    tools: toolPanelReducer
});

const configureStore = (initialState?: RootState): Store<RootState | undefined> => {
    const middlewares: any[] = [];
    const enhancer = composeWithDevTools(applyMiddleware(...middlewares));
    return createStore(rootReducer, initialState, enhancer);
};

const store = configureStore();

export default store;

