using Microsoft.AspNetCore.Mvc;
using Andastra.Web.Backend.Services;

namespace Andastra.Web.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly IGameSessionManager _sessionManager;

        public GameController(IGameSessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

        [HttpGet("session/{sessionId}")]
        public async Task<IActionResult> GetSession(string sessionId)
        {
            var session = await _sessionManager.GetSession(sessionId);
            if (session == null)
            {
                return NotFound();
            }
            
            return Ok(new 
            { 
                session.SessionId, 
                session.Status, 
                session.CreatedAt,
                session.Width,
                session.Height
            });
        }
    }
}
