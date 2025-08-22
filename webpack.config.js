const path = require('path');
const webpack = require('webpack');

module.exports = {
    mode: 'production',
    entry: './node_modules/passkey-kit/src/index.ts',
    output: {
        path: path.resolve(__dirname, 'Assets/WebGLTemplates/Passkey/'),
        filename: 'passkey-kit-official.bundle.js',
        library: {
            name: 'PasskeyKit',
            type: 'window',
            export: 'PasskeyKit'
        },
        globalObject: 'this'
    },
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: [
                    {
                        loader: 'ts-loader',
                        options: {
                            transpileOnly: true,
                            compilerOptions: {
                                module: 'esnext',
                                moduleResolution: 'node',
                                target: 'es5',
                                lib: ['dom', 'es2015']
                            }
                        }
                    }
                ],
                exclude: /node_modules\/(?!passkey-kit|passkey-kit-sdk)/
            }
        ]
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
        fallback: {
            "buffer": require.resolve("buffer/"),
            "crypto": require.resolve("crypto-browserify"),
            "stream": require.resolve("stream-browserify"),
            "util": require.resolve("util/"),
            "fs": false,
            "path": require.resolve("path-browserify"),
            "os": require.resolve("os-browserify/browser"),
            "net": false,
            "tls": false
        }
    },
    plugins: [
        new webpack.ProvidePlugin({
            Buffer: ['buffer', 'Buffer'],
            process: 'process/browser'
        })
    ]
}; 