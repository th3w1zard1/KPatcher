namespace Andastra.Web.Backend.Models
{
    /// <summary>
    /// Represents a player input command (keyboard, mouse, etc.).
    /// </summary>
    public class InputCommand
    {
        public string Type { get; set; }  // "keydown", "keyup", "mousemove", "mousedown", "mouseup"
        public string Key { get; set; }
        public int? MouseX { get; set; }
        public int? MouseY { get; set; }
        public int? Button { get; set; }
    }
}
