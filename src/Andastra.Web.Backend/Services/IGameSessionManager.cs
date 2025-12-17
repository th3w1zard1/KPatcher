using System.Threading.Tasks;
using Andastra.Web.Backend.Models;

namespace Andastra.Web.Backend.Services
{
    /// <summary>
    /// Interface for managing game sessions.
    /// </summary>
    public interface IGameSessionManager
    {
        Task<GameSession> CreateSession();
        Task<GameSession> GetSession(string sessionId);
        Task ProcessInput(string sessionId, InputCommand input);
        Task<byte[]> GetCurrentFrame(string sessionId);
    }
}
