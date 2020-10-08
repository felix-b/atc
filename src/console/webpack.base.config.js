'use strict';

const path = require('path');

module.exports = {
    mode: 'development',
    output: {
        path: path.resolve(__dirname, 'dist'),
        filename: '[name].js'
    },
    node: {
        __dirname: false,
        __filename: false
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js', '.json'],
        alias: {
            'world-js.node$': path.resolve(__dirname, '../libworld-js/build/Release/world-js.node')
        }
    },
    devtool: 'source-map',
    plugins: [
    ],
    module: {
        rules: [
            {
                test: /\.node$/,
                use: 'node-loader'
            }
        ]
    },
};
