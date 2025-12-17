using System;

namespace Andastra.Web.Backend.Models
{
    /// <summary>
    /// Represents a game session with unique identifier and creation time.
    /// </summary>
    public class GameSession
    {
        public string SessionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public byte[] LastFrame { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public GameSession()
        {
            SessionId = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            Status = "initialized";
            Width = 800;
            Height = 600;
        }
    }
}
