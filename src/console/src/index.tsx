import React from "react";
import ReactDOM from "react-dom";
import "./index.css";
import App from "./App";
import reportWebVitals from "./reportWebVitals";
import { AppServices } from "./appServices";
import { createTrafficService } from "./trafficService";
import { createWorldServiceClient } from "./worldServiceClient";

const apiKey = process.env.REACT_APP_GM;
console.log("REACT_APP_GM", apiKey);

const worldService = createWorldServiceClient();
const trafficService = createTrafficService(worldService);

const appServices: AppServices = {
    worldService,
    trafficService,
};

(window as any).appServices = appServices; //TODO: add correct typing

worldService.onOpen(() => {
    setTimeout(() => {
        worldService.sendMessage({
            connect: {
                token: "T12345",
            },
        });
    });
});

worldService.onMessage("replyConnect", () => {
    console.log("CONNECTED TO SERVER!!!");

    ReactDOM.render(
        <React.StrictMode>
            <App {...appServices} />
        </React.StrictMode>,
        document.getElementById("root")
    );

    // trafficService.start();
    // trafficService.beginQuery(10, 10, 31, 31);
});

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();


