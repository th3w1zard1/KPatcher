/**
 * Cryptographic utilities for in-browser WASM decryption.
 * Implements AES-256-GCM decryption using Web Crypto API.
 */

class WasmCrypto {
    /**
     * Decrypts the encrypted WASM binary in memory.
     * @param {ArrayBuffer} encryptedData - Encrypted WASM data [nonce(12) + tag(16) + ciphertext]
     * @param {string} keyBase64 - Base64-encoded 32-byte decryption key
     * @returns {Promise<ArrayBuffer>} Decrypted WASM binary
     */
    static async decryptWasm(encryptedData, keyBase64) {
        console.log('[Crypto] Starting WASM decryption...');
        
        try {
            // Decode the base64 key
            const keyBytes = this.base64ToArrayBuffer(keyBase64);
            if (keyBytes.byteLength !== 32) {
                throw new Error('Invalid key size, expected 32 bytes');
            }

            // Extract components from encrypted data
            const encryptedBytes = new Uint8Array(encryptedData);
            const nonceSize = 12;
            const tagSize = 16;
            
            if (encryptedBytes.length < nonceSize + tagSize) {
                throw new Error('Invalid encrypted data format');
            }

            const nonce = encryptedBytes.slice(0, nonceSize);
            const tag = encryptedBytes.slice(nonceSize, nonceSize + tagSize);
            const ciphertext = encryptedBytes.slice(nonceSize + tagSize);

            // Combine ciphertext and tag for Web Crypto API
            const ciphertextWithTag = new Uint8Array(ciphertext.length + tag.length);
            ciphertextWithTag.set(ciphertext, 0);
            ciphertextWithTag.set(tag, ciphertext.length);

            // Import the key
            const cryptoKey = await crypto.subtle.importKey(
                'raw',
                keyBytes,
                { name: 'AES-GCM', length: 256 },
                false,
                ['decrypt']
            );

            // Decrypt
            const decrypted = await crypto.subtle.decrypt(
                {
                    name: 'AES-GCM',
                    iv: nonce,
                    tagLength: 128
                },
                cryptoKey,
                ciphertextWithTag
            );

            console.log('[Crypto] WASM decrypted successfully');
            return decrypted;
        } catch (error) {
            console.error('[Crypto] Decryption failed:', error);
            throw new Error(`Decryption failed: ${error.message}`);
        }
    }

    /**
     * Converts a base64 string to an ArrayBuffer.
     * @param {string} base64 - Base64 encoded string
     * @returns {ArrayBuffer}
     */
    static base64ToArrayBuffer(base64) {
        const binaryString = atob(base64);
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        return bytes.buffer;
    }

    /**
     * Converts an ArrayBuffer to a base64 string.
     * @param {ArrayBuffer} buffer
     * @returns {string}
     */
    static arrayBufferToBase64(buffer) {
        const bytes = new Uint8Array(buffer);
        let binary = '';
        for (let i = 0; i < bytes.length; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return btoa(binary);
    }

    /**
     * Securely wipes a buffer from memory (best effort).
     * @param {ArrayBuffer} buffer
     */
    static secureWipe(buffer) {
        try {
            const view = new Uint8Array(buffer);
            crypto.getRandomValues(view); // Overwrite with random data
            view.fill(0); // Then zero it out
        } catch (error) {
            console.warn('[Crypto] Could not securely wipe buffer:', error);
        }
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = WasmCrypto;
}
