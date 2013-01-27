#region --- Using Statements ---
using Microsoft.Xna.Framework;
#endregion

namespace IconArena
{
    /// <summary>
    /// Interface to be used by emitter shapes for particle creation.
    /// </summary>
    public interface IEmitterShape
    {
        Vector2 Position { get; set; }
        /// <summary>
        /// Gets a point from the shape as specified via the shape's properties and settings.
        /// </summary>
        Vector2 GetPointFromShape();
    }
}