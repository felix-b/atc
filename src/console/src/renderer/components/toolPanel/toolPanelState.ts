import { Action, AnyAction, Reducer } from "redux";

export interface ToolPanelState {
    activeToolIds: number[];
}

export interface InsertToolIdAction extends Action {
    type: 'toolPanel/insertToolId';
    toolId: number;
}

export interface RemoveToolIdAction extends Action {
    type: 'toolPanel/removeToolId';
    toolId: number;
}

export const ToolPanelActions = {
    insertToolId(toolId: number): InsertToolIdAction {
        return ({
            type: 'toolPanel/insertToolId',
            toolId
        });
    },
    removeToolId(toolId: number): RemoveToolIdAction {
        return ({
            type: 'toolPanel/removeToolId',
            toolId
        });
    },
}

const defaultState: ToolPanelState = {
    activeToolIds: []
};

export const toolPanelReducer: Reducer<ToolPanelState> = (state = defaultState, action: AnyAction): ToolPanelState => {
    switch (action.type) {
        case 'toolPanel/insertToolId': 
            const insertAction = action as InsertToolIdAction;
            return {
                ...state,
                activeToolIds: [
                    ...state.activeToolIds.filter(id => id != insertAction.toolId),
                    insertAction.toolId
                ]
            };
        case 'toolPanel/removeToolId': 
            const removeAction = action as RemoveToolIdAction;
            return {
                ...state,
                activeToolIds: state.activeToolIds.filter(id => id != removeAction.toolId),
            };
        default:
            return state;
    }
}


