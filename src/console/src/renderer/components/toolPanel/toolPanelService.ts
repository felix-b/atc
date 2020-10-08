import * as React from 'react';

import store from '../../store';
import { ToolPanelActions } from './toolPanelState';

export type ToolId = number;

export interface ToolDescriptor {
    id: string;
    title?: string;
    render: () => JSX.Element;
};

function createToolPanelService() {
    let nextToolId = 1;
    let toolById = new Map<ToolId, ToolDescriptor>();

    return {
        insertTool(tool: ToolDescriptor): ToolId {
            const toolId = nextToolId++;
            toolById.set(toolId, tool);
            store.dispatch(ToolPanelActions.insertToolId(toolId));
            return toolId;
        },
        removeTool(id: ToolId) {
            toolById.delete(id);
            store.dispatch(ToolPanelActions.removeToolId(id));
        },
        getToolByIdOrThrow(id: ToolId) {
            const tool = toolById.get(id);
            if (!tool) {
                throw new Error(`Tool with id ${id} not found`);
            }
            return tool;
        },
        attachToolToLifecycle(tool: ToolDescriptor) {
            const toolId = ToolPanelService.insertTool(tool);
            return () => {
                ToolPanelService.removeTool(toolId);
            }
        }
    };
}

export const ToolPanelService = createToolPanelService();