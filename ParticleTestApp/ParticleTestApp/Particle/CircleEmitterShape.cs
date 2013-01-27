#region --- Using Statements ---
using Microsoft.Xna.Framework;
using System;
#endregion

namespace IconArena
{
    /// <summary>
    /// Circle emitter shape for use by particle emitter to get particle initial positions.
    /// Used to make circle, ring, and point shape emitters.
    /// </summary>
    public class CircleEmitterShape : IEmitterShape
    {
        #region Fields

        public Vector2 Position { get; set; }
        private float Radius { get; set; }
        private float MinRadius { get; set; }

        private Random random = new Random();

        #endregion Fields

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="radius">Max distance from position to return particle position point.</param>
        /// <param name="minRadius">Min distance from position to return particle position point. Used to create rings.</param>
        public CircleEmitterShape(float radius, float minRadius = 0.0f)
        {
            if (minRadius > radius)
                throw new ArgumentOutOfRangeException("MinRadius must be less than or equal to Radius.");
            this.Radius = radius;
            this.MinRadius = minRadius;
        }

        #endregion Initialization

        #region Public Methods

        /// <summary>
        /// See IEmitterShape.GetPointFromShape().
        /// </summary>
        public Vector2 GetPointFromShape()
        {
            Vector2 randPoint = new Vector2((float)random.NextDouble() * 2.0f - 1.0f, (float)random.NextDouble() * 2.0f - 1.0f);
            if (randPoint != Vector2.Zero)
                randPoint.Normalize();

            float magnitude = ((float)random.NextDouble() * (Radius - MinRadius)) + MinRadius;
            randPoint *= magnitude;

            return Position + randPoint;
        }

        #endregion Public Methods
    }
}