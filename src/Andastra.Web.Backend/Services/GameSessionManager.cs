using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Andastra.Web.Backend.Models;
using Andastra.Web.Backend.Hubs;

namespace Andastra.Web.Backend.Services
{
    /// <summary>
    /// Manages game sessions, rendering, and input processing.
    /// This is a simplified implementation that creates a mock game frame.
    /// In a full implementation, this would integrate with the actual game engine.
    /// </summary>
    public class GameSessionManager : IGameSessionManager
    {
        private readonly ConcurrentDictionary<string, GameSession> _sessions;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly ConcurrentDictionary<string, InputState> _inputStates;

        public GameSessionManager(IHubContext<GameHub> hubContext)
        {
            _sessions = new ConcurrentDictionary<string, GameSession>();
            _hubContext = hubContext;
            _inputStates = new ConcurrentDictionary<string, InputState>();
        }

        public Task<GameSession> CreateSession()
        {
            var session = new GameSession();
            _sessions.TryAdd(session.SessionId, session);
            _inputStates.TryAdd(session.SessionId, new InputState());
            
            session.Status = "running";
            
            // Start the game loop for this session
            _ = Task.Run(async () => await GameLoop(session.SessionId));
            
            return Task.FromResult(session);
        }

        public Task<GameSession> GetSession(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session);
        }

        public Task ProcessInput(string sessionId, InputCommand input)
        {
            if (_inputStates.TryGetValue(sessionId, out var state))
            {
                switch (input.Type)
                {
                    case "keydown":
                        state.PressedKeys.Add(input.Key);
                        break;
                    case "keyup":
                        state.PressedKeys.Remove(input.Key);
                        break;
                    case "mousemove":
                        state.MouseX = input.MouseX ?? 0;
                        state.MouseY = input.MouseY ?? 0;
                        break;
                    case "mousedown":
                        state.MouseButtonDown = true;
                        break;
                    case "mouseup":
                        state.MouseButtonDown = false;
                        break;
                }
            }
            
            return Task.CompletedTask;
        }

        public Task<byte[]> GetCurrentFrame(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session?.LastFrame);
        }

        private async Task GameLoop(string sessionId)
        {
            const int targetFps = 30;
            const int frameDelay = 1000 / targetFps;
            
            while (_sessions.TryGetValue(sessionId, out var session))
            {
                var startTime = DateTime.UtcNow;
                
                // Generate a frame (mock implementation)
                var frame = GenerateFrame(session);
                session.LastFrame = frame;
                
                // Send frame to connected clients
                await _hubContext.Clients.Group(sessionId).SendAsync("ReceiveFrame", Convert.ToBase64String(frame));
                
                // Frame rate limiting
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                var delay = Math.Max(0, frameDelay - (int)elapsed);
                
                if (delay > 0)
                {
                    await Task.Delay(delay);
                }
            }
        }

        private byte[] GenerateFrame(GameSession session)
        {
            // Create a simple mock frame with a message
            // In a real implementation, this would render the actual game
            using var image = new Image<Rgba32>(session.Width, session.Height);
            
            image.Mutate(ctx =>
            {
                // Fill with a dark blue background
                ctx.BackgroundColor(Color.FromRgb(20, 30, 50));
                
                // Add a simple text overlay (ImageSharp doesn't have easy text rendering, so we'll just return a colored frame)
                // In production, you would render the actual game content here
            });
            
            using var ms = new System.IO.MemoryStream();
            image.SaveAsJpeg(ms);
            return ms.ToArray();
        }

        private class InputState
        {
            public System.Collections.Generic.HashSet<string> PressedKeys { get; set; } = new();
            public int MouseX { get; set; }
            public int MouseY { get; set; }
            public bool MouseButtonDown { get; set; }
        }
    }
}
