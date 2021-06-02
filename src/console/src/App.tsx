import './App.css';
import WorldMapView from './worldMapView';
import { AppServices } from './appServices';

function App(services: AppServices) {
    return (
        <WorldMapView {...services} />
    );
}

export default App;
