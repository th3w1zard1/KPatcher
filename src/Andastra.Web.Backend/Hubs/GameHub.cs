using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Andastra.Web.Backend.Models;
using Andastra.Web.Backend.Services;

namespace Andastra.Web.Backend.Hubs
{
    /// <summary>
    /// SignalR hub for real-time game communication between client and server.
    /// </summary>
    public class GameHub : Hub
    {
        private readonly IGameSessionManager _sessionManager;

        public GameHub(IGameSessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public async Task<string> CreateSession()
        {
            var session = await _sessionManager.CreateSession();
            await Groups.AddToGroupAsync(Context.ConnectionId, session.SessionId);
            return session.SessionId;
        }

        public async Task JoinSession(string sessionId)
        {
            var session = await _sessionManager.GetSession(sessionId);
            if (session != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            }
        }

        public async Task SendInput(string sessionId, InputCommand input)
        {
            await _sessionManager.ProcessInput(sessionId, input);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
