/**
 * Main application controller that orchestrates the entire workflow:
 * 1. File selection and validation
 * 2. WASM loading and decryption
 * 3. Virtual filesystem mounting
 * 4. Game initialization and execution
 */

class Application {
    constructor() {
        this.vfs = new VirtualFileSystem();
        this.wasmLoader = new WasmLoader();
        this.isInitialized = false;
        
        // UI elements
        this.elements = {
            selectFolderBtn: document.getElementById('select-folder-btn'),
            startGameBtn: document.getElementById('start-game-btn'),
            retryBtn: document.getElementById('retry-btn'),
            loadingIndicator: document.getElementById('loading-indicator'),
            loadingMessage: document.getElementById('loading-message'),
            filePickerContainer: document.getElementById('file-picker-container'),
            gameContainer: document.getElementById('game-container'),
            errorContainer: document.getElementById('error-container'),
            errorMessage: document.getElementById('error-message'),
            fileValidation: document.getElementById('file-validation'),
            validationList: document.getElementById('validation-list'),
            consoleOutput: document.getElementById('console-output'),
            versionInfo: document.getElementById('version-info')
        };

        this.bindEvents();
        this.checkBrowserSupport();
        this.loadVersionInfo();
    }

    /**
     * Binds event handlers to UI elements.
     */
    bindEvents() {
        this.elements.selectFolderBtn.addEventListener('click', () => this.handleSelectFolder());
        this.elements.startGameBtn.addEventListener('click', () => this.handleStartGame());
        this.elements.retryBtn.addEventListener('click', () => this.handleRetry());
    }

    /**
     * Checks if the browser supports required features.
     */
    checkBrowserSupport() {
        const missingFeatures = [];

        if (!VirtualFileSystem.isSupported()) {
            missingFeatures.push('File System Access API');
        }

        if (!window.crypto || !window.crypto.subtle) {
            missingFeatures.push('Web Crypto API');
        }

        if (!window.WebAssembly) {
            missingFeatures.push('WebAssembly');
        }

        if (missingFeatures.length > 0) {
            this.showError(
                `Your browser does not support required features:\n` +
                missingFeatures.join(', ') +
                `\n\nPlease use a modern browser like Chrome, Edge, or Safari.`
            );
            this.elements.selectFolderBtn.disabled = true;
        }
    }

    /**
     * Loads and displays version information.
     */
    async loadVersionInfo() {
        try {
            const versionInfo = await this.wasmLoader.fetchVersionInfo();
            this.elements.versionInfo.textContent = 
                `API ${versionInfo.api.version} | WASM ${versionInfo.wasm.version}`;
        } catch (error) {
            this.log('Failed to load version info', 'warning');
        }
    }

    /**
     * Handles folder selection and validation.
     */
    async handleSelectFolder() {
        this.log('Requesting game folder selection...');
        this.elements.selectFolderBtn.disabled = true;

        try {
            const result = await this.vfs.selectGameDirectory();
            
            // Display validation results
            this.displayValidation(result.validation);

            if (result.success) {
                this.log('Game files validated successfully', 'success');
                await this.loadWasmRuntime();
            } else {
                this.log('Game file validation failed', 'error');
                this.elements.selectFolderBtn.disabled = false;
            }
        } catch (error) {
            if (error.name !== 'AbortError') {
                this.showError(`Failed to select folder: ${error.message}`);
            }
            this.elements.selectFolderBtn.disabled = false;
        }
    }

    /**
     * Displays file validation results.
     */
    displayValidation(validations) {
        this.elements.validationList.innerHTML = '';
        
        validations.forEach(validation => {
            const li = document.createElement('li');
            li.className = validation.valid ? 'success' : 'error';
            li.textContent = validation.message;
            this.elements.validationList.appendChild(li);
        });

        this.elements.fileValidation.classList.remove('hidden');
    }

    /**
     * Loads and decrypts the WASM runtime.
     */
    async loadWasmRuntime() {
        this.showLoading('Loading and decrypting game engine...');

        try {
            // Load and decrypt WASM
            const wasmBinary = await this.wasmLoader.loadAndDecryptWasm();
            this.log(`Decrypted WASM runtime (${(wasmBinary.byteLength / 1024 / 1024).toFixed(2)} MB)`, 'success');

            // Instantiate runtime
            this.showLoading('Initializing .NET runtime...');
            await this.wasmLoader.instantiateRuntime(wasmBinary);
            this.log('.NET runtime initialized', 'success');

            // Mount filesystem
            this.showLoading('Mounting virtual filesystem...');
            const mountPoint = await this.vfs.mountToWasm();
            this.log(`Virtual filesystem mounted at ${mountPoint}`, 'success');

            // Initialize game
            this.showLoading('Initializing game engine...');
            const initialized = await this.wasmLoader.initializeGame(mountPoint);

            if (initialized) {
                this.log('Game engine initialized successfully', 'success');
                this.isInitialized = true;
                this.showGameContainer();
            } else {
                throw new Error('Game initialization failed');
            }
        } catch (error) {
            this.log(`Initialization failed: ${error.message}`, 'error');
            this.showError(`Failed to initialize: ${error.message}`);
        }
    }

    /**
     * Handles game start.
     */
    async handleStartGame() {
        if (!this.isInitialized) {
            this.showError('Game not initialized');
            return;
        }

        this.log('Starting game...');
        this.elements.startGameBtn.disabled = true;

        try {
            await this.wasmLoader.startGame();
            this.log('Game started successfully', 'success');
        } catch (error) {
            this.log(`Failed to start game: ${error.message}`, 'error');
            this.showError(`Failed to start game: ${error.message}`);
            this.elements.startGameBtn.disabled = false;
        }
    }

    /**
     * Handles retry after error.
     */
    handleRetry() {
        this.hideError();
        this.hideLoading();
        this.elements.filePickerContainer.classList.remove('hidden');
        this.elements.selectFolderBtn.disabled = false;
        this.vfs.unmount();
        this.isInitialized = false;
    }

    /**
     * Shows the loading indicator with a message.
     */
    showLoading(message) {
        this.elements.loadingMessage.textContent = message;
        this.elements.loadingIndicator.classList.remove('hidden');
        this.elements.filePickerContainer.classList.add('hidden');
        this.elements.gameContainer.classList.add('hidden');
        this.elements.errorContainer.classList.add('hidden');
    }

    /**
     * Hides the loading indicator.
     */
    hideLoading() {
        this.elements.loadingIndicator.classList.add('hidden');
    }

    /**
     * Shows the game container.
     */
    showGameContainer() {
        this.hideLoading();
        this.elements.gameContainer.classList.remove('hidden');
    }

    /**
     * Shows an error message.
     */
    showError(message) {
        this.hideLoading();
        this.elements.filePickerContainer.classList.add('hidden');
        this.elements.gameContainer.classList.add('hidden');
        this.elements.errorMessage.textContent = message;
        this.elements.errorContainer.classList.remove('hidden');
    }

    /**
     * Hides the error container.
     */
    hideError() {
        this.elements.errorContainer.classList.add('hidden');
    }

    /**
     * Logs a message to the console output.
     */
    log(message, type = 'info') {
        const timestamp = new Date().toLocaleTimeString();
        const entry = document.createElement('div');
        entry.className = `log-entry log-${type}`;
        entry.textContent = `[${timestamp}] ${message}`;
        this.elements.consoleOutput.appendChild(entry);
        this.elements.consoleOutput.scrollTop = this.elements.consoleOutput.scrollHeight;

        // Also log to browser console
        const consoleMethod = type === 'error' ? 'error' : type === 'warning' ? 'warn' : 'log';
        console[consoleMethod](`[App] ${message}`);
    }
}

// Initialize application when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    console.log('[App] Initializing Andastra Web Application...');
    window.app = new Application();
});
