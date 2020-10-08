import { hot } from 'react-hot-loader/root';
import * as React from 'react';
import { AirportView } from '../airport/airportView';
import { ToolPanel } from '../../components/toolPanel/toolPanel';

require('./application.scss');

const Application = () => (
    <>
        <AirportView />
        <ToolPanel />
    </>
);

export default hot(Application);
