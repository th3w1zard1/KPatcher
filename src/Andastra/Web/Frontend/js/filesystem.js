/**
 * Virtual filesystem layer for mounting browser file handles to POSIX-like paths.
 * Uses the File System Access API to provide game files to the WASM runtime.
 */

class VirtualFileSystem {
    constructor() {
        this.rootHandle = null;
        this.mountedFiles = new Map();
        this.fileHandles = new Map();
    }

    /**
     * Checks if File System Access API is supported.
     * @returns {boolean}
     */
    static isSupported() {
        return 'showDirectoryPicker' in window;
    }

    /**
     * Prompts user to select a directory and validates game files.
     * @returns {Promise<{success: boolean, validation: Array}>}
     */
    async selectGameDirectory() {
        console.log('[VFS] Opening directory picker...');
        
        try {
            // Request directory access
            this.rootHandle = await window.showDirectoryPicker({
                mode: 'read',
                startIn: 'documents'
            });

            console.log('[VFS] Directory selected:', this.rootHandle.name);

            // Validate required files
            const validation = await this.validateGameFiles();
            
            return {
                success: validation.every(v => v.valid),
                validation: validation
            };
        } catch (error) {
            if (error.name === 'AbortError') {
                console.log('[VFS] User cancelled directory selection');
            } else {
                console.error('[VFS] Error selecting directory:', error);
            }
            throw error;
        }
    }

    /**
     * Validates that required game files exist.
     * @returns {Promise<Array<{name: string, valid: boolean, message: string}>>}
     */
    async validateGameFiles() {
        const validations = [];

        // Check for chitin.key
        try {
            const chitinKey = await this.rootHandle.getFileHandle('chitin.key');
            validations.push({
                name: 'chitin.key',
                valid: true,
                message: 'Found chitin.key'
            });
            this.fileHandles.set('chitin.key', chitinKey);
        } catch (error) {
            validations.push({
                name: 'chitin.key',
                valid: false,
                message: 'chitin.key not found - this file is required'
            });
        }

        // Check for data directory and .bif files
        try {
            const dataDir = await this.rootHandle.getDirectoryHandle('data');
            let bifCount = 0;
            
            for await (const entry of dataDir.values()) {
                if (entry.kind === 'file' && entry.name.toLowerCase().endsWith('.bif')) {
                    bifCount++;
                    this.fileHandles.set(`data/${entry.name}`, entry);
                }
            }

            if (bifCount > 0) {
                validations.push({
                    name: '.bif files',
                    valid: true,
                    message: `Found ${bifCount} .bif file(s) in data directory`
                });
            } else {
                validations.push({
                    name: '.bif files',
                    valid: false,
                    message: 'No .bif files found in data directory'
                });
            }
        } catch (error) {
            validations.push({
                name: 'data directory',
                valid: false,
                message: 'data directory not found or not accessible'
            });
        }

        return validations;
    }

    /**
     * Mounts the selected directory into the WASM virtual filesystem.
     * This creates a bridge between browser file handles and WASM file I/O.
     * @param {string} mountPoint - Virtual path where files should be mounted
     * @returns {Promise<string>} The mount point path
     */
    async mountToWasm(mountPoint = '/gamedata') {
        console.log(`[VFS] Mounting filesystem at ${mountPoint}...`);

        if (!this.rootHandle) {
            throw new Error('No directory selected');
        }

        // In a real implementation, this would use Emscripten's FS API
        // to create a custom filesystem that bridges to File System Access API
        // For now, we'll create a simple mapping structure

        const mounted = {
            path: mountPoint,
            handle: this.rootHandle,
            files: this.fileHandles
        };

        this.mountedFiles.set(mountPoint, mounted);

        console.log(`[VFS] Filesystem mounted at ${mountPoint}`);
        return mountPoint;
    }

    /**
     * Reads a file from the virtual filesystem.
     * @param {string} path - Virtual path to the file
     * @returns {Promise<ArrayBuffer>}
     */
    async readFile(path) {
        console.log(`[VFS] Reading file: ${path}`);
        
        // Find the file handle
        let fileHandle = this.fileHandles.get(path);
        
        if (!fileHandle) {
            // Try to resolve from root
            const parts = path.split('/').filter(p => p);
            let currentHandle = this.rootHandle;
            
            for (let i = 0; i < parts.length - 1; i++) {
                currentHandle = await currentHandle.getDirectoryHandle(parts[i]);
            }
            
            fileHandle = await currentHandle.getFileHandle(parts[parts.length - 1]);
        }

        if (!fileHandle) {
            throw new Error(`File not found: ${path}`);
        }

        const file = await fileHandle.getFile();
        return await file.arrayBuffer();
    }

    /**
     * Gets the root path of the mounted filesystem.
     * @returns {string}
     */
    getRootPath() {
        return '/gamedata';
    }

    /**
     * Unmounts the filesystem.
     */
    unmount() {
        console.log('[VFS] Unmounting filesystem...');
        this.mountedFiles.clear();
        this.fileHandles.clear();
        this.rootHandle = null;
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = VirtualFileSystem;
}
