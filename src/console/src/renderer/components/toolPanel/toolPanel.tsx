import * as React from 'react';
import { connect } from 'react-redux';
import { ToolDescriptor, ToolPanelService } from './toolPanelService';
import { RootState } from '../../store';

require('./toolPanel.scss');

export interface ToolPanelProps {
    tools: ToolDescriptor[];
}

const PureToolPanel: React.FunctionComponent<ToolPanelProps> = ({ tools }) => {
    return (
        <div className="tool-panel">
            {tools.map(tool => (
                <div key={tool.id} className="tool-frame">
                    {tool.render()}
                </div>
            ))}
        </div>
    );
};

export const ToolPanel = connect<ToolPanelProps, {}, {}, RootState>(
    (state) => {
        const { activeToolIds } = state.tools;
        const tools = activeToolIds.map(ToolPanelService.getToolByIdOrThrow);
        return {
            tools
        };
    }
)(PureToolPanel);
