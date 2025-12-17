/**
 * WASM loader that handles fetching encrypted WASM, obtaining decryption keys,
 * and instantiating the .NET WebAssembly runtime.
 */

class WasmLoader {
    constructor(apiBaseUrl = '') {
        this.apiBaseUrl = apiBaseUrl;
        this.wasmModule = null;
        this.dotnetRuntime = null;
    }

    /**
     * Fetches an ephemeral decryption key from the server.
     * @returns {Promise<string>} Base64-encoded decryption key
     */
    async fetchDecryptionKey() {
        console.log('[WASM Loader] Requesting decryption key...');
        
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/runtime/key`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error(`Failed to fetch key: ${response.status} ${response.statusText}`);
            }

            const data = await response.json();
            console.log('[WASM Loader] Decryption key received (valid for', data.validFor, ')');
            
            return data.key;
        } catch (error) {
            console.error('[WASM Loader] Failed to fetch decryption key:', error);
            throw error;
        }
    }

    /**
     * Fetches the encrypted WASM binary from the server.
     * @returns {Promise<ArrayBuffer>}
     */
    async fetchEncryptedWasm() {
        console.log('[WASM Loader] Downloading encrypted WASM...');
        
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/runtime/wasm`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/wasm-encrypted'
                }
            });

            if (!response.ok) {
                throw new Error(`Failed to fetch WASM: ${response.status} ${response.statusText}`);
            }

            const wasmData = await response.arrayBuffer();
            console.log(`[WASM Loader] Downloaded ${(wasmData.byteLength / 1024 / 1024).toFixed(2)} MB encrypted WASM`);
            
            return wasmData;
        } catch (error) {
            console.error('[WASM Loader] Failed to fetch encrypted WASM:', error);
            throw error;
        }
    }

    /**
     * Loads and decrypts the WASM runtime.
     * @returns {Promise<ArrayBuffer>} Decrypted WASM binary
     */
    async loadAndDecryptWasm() {
        console.log('[WASM Loader] Starting WASM load and decrypt sequence...');
        
        try {
            // Fetch key and encrypted WASM in parallel
            const [key, encryptedWasm] = await Promise.all([
                this.fetchDecryptionKey(),
                this.fetchEncryptedWasm()
            ]);

            // Decrypt the WASM in memory
            const decryptedWasm = await WasmCrypto.decryptWasm(encryptedWasm, key);
            
            console.log(`[WASM Loader] Decrypted ${(decryptedWasm.byteLength / 1024 / 1024).toFixed(2)} MB WASM`);

            // Securely wipe the encrypted data and key from memory
            WasmCrypto.secureWipe(encryptedWasm);
            
            return decryptedWasm;
        } catch (error) {
            console.error('[WASM Loader] Failed to load and decrypt WASM:', error);
            throw error;
        }
    }

    /**
     * Instantiates the .NET WASM runtime.
     * @param {ArrayBuffer} wasmBinary - Decrypted WASM binary
     * @returns {Promise<void>}
     */
    async instantiateRuntime(wasmBinary) {
        console.log('[WASM Loader] Instantiating .NET runtime...');
        
        try {
            // For .NET 9+ WebAssembly with Blazor, we need to use the dotnet.js loader
            // This is a simplified version - the actual implementation would use
            // the generated dotnet.js bootstrap file
            
            // Check if dotnet runtime is available
            if (typeof dotnet === 'undefined') {
                throw new Error('.NET runtime loader not found');
            }

            // Create runtime configuration
            const config = {
                configSrc: 'blazor.boot.json',
                onConfigLoaded: (config) => {
                    console.log('[WASM Loader] .NET config loaded:', config);
                },
                onDotnetReady: () => {
                    console.log('[WASM Loader] .NET runtime ready');
                }
            };

            // Initialize .NET runtime
            this.dotnetRuntime = await dotnet
                .withConfig(config)
                .create();

            console.log('[WASM Loader] .NET runtime instantiated successfully');
            
            return this.dotnetRuntime;
        } catch (error) {
            console.error('[WASM Loader] Failed to instantiate runtime:', error);
            throw error;
        }
    }

    /**
     * Calls an exported .NET method from JavaScript.
     * @param {string} methodName - Name of the exported method
     * @param {...any} args - Arguments to pass to the method
     * @returns {Promise<any>}
     */
    async callExportedMethod(methodName, ...args) {
        if (!this.dotnetRuntime) {
            throw new Error('Runtime not initialized');
        }

        try {
            // Get the exported method
            const method = await this.dotnetRuntime.getAssemblyExports('Andastra.Game.Wasm.dll');
            
            if (!method[methodName]) {
                throw new Error(`Method ${methodName} not found in exports`);
            }

            // Call the method
            return await method[methodName](...args);
        } catch (error) {
            console.error(`[WASM Loader] Error calling ${methodName}:`, error);
            throw error;
        }
    }

    /**
     * Initializes the game with the given data path.
     * @param {string} gameDataPath - Path to mounted game data
     * @returns {Promise<boolean>}
     */
    async initializeGame(gameDataPath) {
        console.log(`[WASM Loader] Initializing game with path: ${gameDataPath}`);
        
        try {
            return await this.callExportedMethod('InitializeGame', gameDataPath);
        } catch (error) {
            console.error('[WASM Loader] Failed to initialize game:', error);
            throw error;
        }
    }

    /**
     * Starts the game loop.
     * @returns {Promise<void>}
     */
    async startGame() {
        console.log('[WASM Loader] Starting game...');
        
        try {
            await this.callExportedMethod('StartGame');
        } catch (error) {
            console.error('[WASM Loader] Failed to start game:', error);
            throw error;
        }
    }

    /**
     * Fetches version information from the API.
     * @returns {Promise<object>}
     */
    async fetchVersionInfo() {
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/version`);
            if (!response.ok) {
                throw new Error('Failed to fetch version info');
            }
            return await response.json();
        } catch (error) {
            console.error('[WASM Loader] Failed to fetch version info:', error);
            return {
                api: { version: 'unknown' },
                wasm: { version: 'unknown' }
            };
        }
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = WasmLoader;
}
