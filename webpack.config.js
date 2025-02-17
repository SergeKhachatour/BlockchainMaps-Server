const path = require('path');

module.exports = {
    mode: 'production',
    entry: './node_modules/passkey-kit/packages/passkey-kit-sdk/src/index.ts',
    output: {
        path: path.resolve(__dirname, 'Assets/WebGLTemplates/Passkey/'),
        filename: 'passkey-kit.bundle.js',
        library: 'PasskeyKit',
        libraryTarget: 'window'
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
                exclude: /node_modules\/(?!passkey-kit)/
            }
        ]
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
        fallback: {
            "buffer": require.resolve("buffer/"),
            "crypto": require.resolve("crypto-browserify"),
            "stream": require.resolve("stream-browserify"),
            "util": require.resolve("util/")
        }
    }
}; 