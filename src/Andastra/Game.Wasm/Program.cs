using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Andastra.Game.Wasm
{
    /// <summary>
    /// Entry point for the WebAssembly game runtime.
    /// This class provides the bridge between JavaScript and the .NET game engine.
    /// </summary>
    public partial class Program
    {
        private static GameRuntime _gameRuntime;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("[WASM] Andastra Game Engine initializing...");
            
            // Initialize the game runtime
            _gameRuntime = new GameRuntime();
            
            Console.WriteLine("[WASM] Game Engine initialized. Waiting for game data...");
        }

        /// <summary>
        /// JavaScript-callable method to initialize the game with a virtual filesystem path.
        /// Called after the browser mounts game files into the virtual filesystem.
        /// </summary>
        /// <param name="gameDataPath">Path to the root of the mounted game data</param>
        [JSExport]
        public static Task<bool> InitializeGame(string gameDataPath)
        {
            try
            {
                Console.WriteLine($"[WASM] InitializeGame called with path: {gameDataPath}");
                
                if (_gameRuntime == null)
                {
                    Console.WriteLine("[WASM] ERROR: Game runtime not initialized");
                    return Task.FromResult(false);
                }

                // Validate game files exist
                if (!ValidateGameFiles(gameDataPath))
                {
                    Console.WriteLine("[WASM] ERROR: Required game files not found");
                    return Task.FromResult(false);
                }

                // Initialize the game with the provided path
                bool success = _gameRuntime.Initialize(gameDataPath);
                
                if (success)
                {
                    Console.WriteLine("[WASM] Game initialized successfully");
                }
                else
                {
                    Console.WriteLine("[WASM] Game initialization failed");
                }

                return Task.FromResult(success);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WASM] Exception during initialization: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// JavaScript-callable method to start the game loop.
        /// </summary>
        [JSExport]
        public static Task StartGame()
        {
            try
            {
                Console.WriteLine("[WASM] StartGame called");
                
                if (_gameRuntime == null)
                {
                    Console.WriteLine("[WASM] ERROR: Game runtime not initialized");
                    return Task.CompletedTask;
                }

                _gameRuntime.Start();
                Console.WriteLine("[WASM] Game started");
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WASM] Exception during game start: {ex.Message}");
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Validates that required game files exist at the specified path.
        /// </summary>
        private static bool ValidateGameFiles(string path)
        {
            try
            {
                // Check for chitin.key
                string chitinKeyPath = System.IO.Path.Combine(path, "chitin.key");
                if (!System.IO.File.Exists(chitinKeyPath))
                {
                    Console.WriteLine($"[WASM] Missing required file: chitin.key at {chitinKeyPath}");
                    return false;
                }

                // Check for at least one .bif file in the data directory
                string dataPath = System.IO.Path.Combine(path, "data");
                if (System.IO.Directory.Exists(dataPath))
                {
                    var bifFiles = System.IO.Directory.GetFiles(dataPath, "*.bif", System.IO.SearchOption.TopDirectoryOnly);
                    if (bifFiles.Length == 0)
                    {
                        Console.WriteLine($"[WASM] No .bif files found in {dataPath}");
                        return false;
                    }
                    Console.WriteLine($"[WASM] Found {bifFiles.Length} .bif files");
                }

                Console.WriteLine("[WASM] Game files validated successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WASM] Error validating game files: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Represents the game runtime that manages the game lifecycle.
    /// </summary>
    public class GameRuntime
    {
        private string _gameDataPath;
        private bool _isInitialized;

        public bool Initialize(string gameDataPath)
        {
            if (_isInitialized)
            {
                Console.WriteLine("[GameRuntime] Already initialized");
                return true;
            }

            _gameDataPath = gameDataPath;
            
            // TODO: Initialize Stride engine
            // TODO: Load game resources
            // TODO: Initialize game systems
            
            _isInitialized = true;
            return true;
        }

        public void Start()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Game runtime must be initialized before starting");
            }

            // TODO: Start the game loop
            // TODO: Begin rendering
        }
    }
}
