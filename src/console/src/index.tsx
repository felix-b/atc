import React from 'react';
import ReactDOM from 'react-dom';
import './index.css';
import App from './App';
import reportWebVitals from './reportWebVitals';
import { WorldServiceEndpoint } from './worldServiceEndpoint';

const apiKey = process.env.REACT_APP_GM;
console.log('REACT_APP_GM', apiKey);

WorldServiceEndpoint.onOpen(() => {
  setTimeout(() => {
      WorldServiceEndpoint.sendMessage({
          connect: {
              token: 'T12345'
          }
      });
  });
});

WorldServiceEndpoint.onMessage('replyConnect', () => { 
  console.log('CONNECTED TO SERVER!!!'); 
  WorldServiceEndpoint.sendMessage({
    queryTraffic: {
        minLat: 10.0,
        minLon: 10.0,
        maxLat: 31.0,
        maxLon: 31.0
    }});
});

ReactDOM.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
  document.getElementById('root')
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();
