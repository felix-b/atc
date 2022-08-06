import React from 'react';
import './App.css';
import { TraceView } from './features/traceView/TraceView';
import { TraceToolbar } from './features/traceView/TraceViewToolbar';
import { AppDependencyContext } from './AppDependencyContext';


const App = () => (
    <>
        <h1>Trace View</h1>
        <AppDependencyContext.Consumer>
            {dependencies => (
                <TraceToolbar 
                    store={dependencies.store} 
                    traceService={dependencies.traceService} 
                    traceViewAPI={dependencies.traceViewAPI} />
            )}
        </AppDependencyContext.Consumer>
        <hr/>
        <TraceView />
    </>
);

/*
function App() {
  return (
    <div className="App">
      <header className="App-header">
        <img src={logo} className="App-logo" alt="logo" />
        <Counter />
        <p>
          Edit <code>src/App.tsx</code> and save to reload.
        </p>
        <span>
          <span>Learn </span>
          <a
            className="App-link"
            href="https://reactjs.org/"
            target="_blank"
            rel="noopener noreferrer"
          >
            React
          </a>
          <span>, </span>
          <a
            className="App-link"
            href="https://redux.js.org/"
            target="_blank"
            rel="noopener noreferrer"
          >
            Redux
          </a>
          <span>, </span>
          <a
            className="App-link"
            href="https://redux-toolkit.js.org/"
            target="_blank"
            rel="noopener noreferrer"
          >
            Redux Toolkit
          </a>
          ,<span> and </span>
          <a
            className="App-link"
            href="https://react-redux.js.org/"
            target="_blank"
            rel="noopener noreferrer"
          >
            React Redux
          </a>
        </span>
      </header>
    </div>
  );
}
*/
export default App;
