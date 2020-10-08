import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { Provider } from 'react-redux';
import { AppContainer } from 'react-hot-loader';

import Application from './views/application';
import store from './store';
import { AirportService } from './views/airport/airportService';
import { WorldServiceEndpoint } from './endpoints/worldServiceEndpoint';


WorldServiceEndpoint.onMessage('replyConnect', ({replyConnect}) => {
    //AirportService.beginQueryAirport('KJFK');
});

WorldServiceEndpoint.onOpen(() => {
    setTimeout(() => {
        WorldServiceEndpoint.sendMessage({
            connect: {
                token: 'HELLO'
            }
        });
    });
});

// Create main element

const mainElement = document.createElement('div');
document.body.appendChild(mainElement);

// Render components
const render = (Component: () => JSX.Element) => {
    ReactDOM.render(
        <AppContainer>
            <Provider store={store}>
                <Component />
            </Provider>
        </AppContainer>,
        mainElement
    );
};

// const onDocumentReady = (action: () => void) => {
//     if (document.readyState === 'complete' || document.readyState === 'interactive') {
//         action();
//     } else {
//         document.addEventListener('DOMContentLoaded', action);
//     }
// };   

// const initFileDrop = () => {
//     window.ondragover = (e: DragEvent) => {
//         e.preventDefault();
//         if (e.dataTransfer) {
//             e.dataTransfer.dropEffect = 'copy';
//         }
//         return false;
//     };
  
//     window.ondrop = (e: DragEvent) => {
//         e.preventDefault();
//         if (e.dataTransfer && e.dataTransfer.files.length > 0) {
//             const file = e.dataTransfer.files[0];
//             console.log('DRAG-N-DROP> dropped file:', file.path);
//             AirportService.loadLocalJsonFile(file.path);
//         }
//         return false;
//     };
  
//     window.ondragleave = () => {
//       return false;
//     };
// };

if (typeof module.hot !== 'undefined') {
    module.hot.accept('./store', () =>
        store.replaceReducer(require('./store').rootReducer)
    );
}

//initFileDrop();
//onDocumentReady(initFileDrop);

render(Application);
