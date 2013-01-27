#region --- Using Statements ---
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace IconArena
{
    /// <summary>
    /// Represents a texture object used by the particle.
    /// </summary>
    public struct ParticleTextureData
    {
        public Texture2D Texture;
        public Vector2 TextureOrigin;
    }

    /// <summary>
    /// Represents an object that pulls particles towards its position based on their distance from the attractor and the attractor's force.
    /// </summary>
    public struct ParticleAttractor
    {
        public Vector2 Position;
        public float Force;
        public float MaxDistance;
    }

    /// <summary>
    /// A function specifying a color alpha value of a particle given its current lifespan fraction (percent of its lifespan that has passed).
    /// </summary>
    /// <param name="t">Value in the range [0, 1] representing the current lifespan fraction of the particle.</param>
    /// <returns>Color alpha value. (Range [0, 1])</returns>
    public delegate float AlphaValueDelegate(float currentLifespanFraction);

    /// <summary>
    /// Small, rotating sprite to be used by a ParticleEngine.
    /// </summary>
    public class Particle
    {
        #region Fields

        private ParticleTextureData TextureData { get; set; }
        private Vector2 Position { get; set; }
        private Vector2 Velocity { get; set; }
        private float AngleRads { get; set; }
        private float AngularVelocityRads { get; set; }
        private Color Color { get; set; }
        private float Size { get; set; }
        /// <summary>
        /// The growth rate of the particle.
        /// Initial size is multiplied by the rate every second (smoothly).
        /// </summary>
        private float SizeChangeRatePerSecond { get; set; }
        private float TimeToLive;
        private float TimeAlive { get; set; }
        public bool IsExpired 
        {
            get { return TimeAlive >= TimeToLive; } 
        }

        /// <summary>
        /// Function defining the alpha value of the particle throughout the particle lifespan.
        /// See Particle.AlphaValueDelegate definition for more info.
        /// </summary>
        private AlphaValueDelegate alphaValueFunction; // Alpha value function to use. If null, uses alpha value of 1.0f.

        #endregion Fields

        #region Initialization
        
        /// <summary>
        /// Creates a new Particle.
        /// </summary>
        /// <param name="alphaValueFunc">Alpha value function to use. If null, uses alpha value of 1.0f.</param>
        public Particle(ParticleTextureData textureData, Vector2 position, Vector2 velocity, float angleRads, float angularVelocity, 
            Color color, float size, float sizeChangeRatePerSecond, float timeToLive, AlphaValueDelegate alphaValueFunc = null)
        {
            Reset(textureData, position, velocity, angleRads, angularVelocity, color, size, sizeChangeRatePerSecond, timeToLive, alphaValueFunc);
        }

        /// <summary>
        /// Resets a Particle for reuse.
        /// </summary>
        /// /// <param name="alphaValueFunc">Alpha value function to use. If null, uses alpha value of 1.0f.</param>
        public void Reset(ParticleTextureData textureData, Vector2 position, Vector2 velocity, float angleRads, float angularVelocity, 
            Color color, float size, float sizeChangeRatePerSecond, float timeToLive, AlphaValueDelegate alphaValueFunc = null)
        {
            this.TextureData = textureData;
            this.Position = position;
            this.Velocity = velocity;
            this.AngleRads = angleRads;
            this.AngularVelocityRads = angularVelocity;
            this.Color = color;
            this.Size = size;
            this.SizeChangeRatePerSecond = sizeChangeRatePerSecond;
            this.TimeToLive = timeToLive;
            this.alphaValueFunction = alphaValueFunc;

            TimeAlive = 0.0f;
        }

        #endregion Initialization

        #region Update

        /// <summary>
        /// Updates the Particle.
        /// </summary>
        /// <param name="uniformVectorFieldAcceleration">Acceleration applied to particle for this update.</param>
        public void Update(GameTime gameTime, Vector2 uniformVectorFieldAcceleration, List<ParticleAttractor> attractors)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            TimeAlive += elapsed;

            Vector2 totalAcceleration = uniformVectorFieldAcceleration;

            // Add acceleration due to attractors
            for (int i = 0; i < attractors.Count; i++)
            {
                Vector2 particleToAttractor = attractors[i].Position - Position;
                float distanceToAttractorSquared = particleToAttractor.LengthSquared();
                if (distanceToAttractorSquared <= (attractors[i].MaxDistance * attractors[i].MaxDistance)) // If it's within the max distance of the attractor
                {
                    float distanceToAttractor = particleToAttractor.Length();
                    // How strong the force of the attractor is based on inverse linear distance between attractor and particle. Range [0, 1]
                    // Zero if they share positions, one if the particle is at the attractor's max distance
                    float strengthOfForce = (1.0f - (distanceToAttractor / attractors[i].MaxDistance)); 
                    float forceFromAttractor = attractors[i].Force * strengthOfForce; // Multiply normalized force by the attractor's force

                    Vector2 particleToAttractorNorm = particleToAttractor / distanceToAttractor; // Calculate normalized using already calculated vector length
                    totalAcceleration += forceFromAttractor * particleToAttractorNorm;
                }
            }

            Vector2 accelMultTime = totalAcceleration * elapsed;
            Velocity += accelMultTime / 2.0f; // Make velocity the average velocity between this and last frame (because of acceleration)
            Position += Velocity * elapsed; // Move based on average velocity
            Velocity += accelMultTime / 2.0f; // Set velocity to final value (value at this frame)

            AngleRads += AngularVelocityRads * elapsed;
        }

        #endregion Update

        #region Draw

        /// <summary>
        /// Draws the Particle.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            float alpha = 1.0f;
            if (alphaValueFunction != null)
            {
                float currentLifespanFraction = TimeAlive / TimeToLive;
                alpha = alphaValueFunction(currentLifespanFraction);
            }
            float currentSize = Size * (float)Math.Pow(SizeChangeRatePerSecond, TimeAlive);
            spriteBatch.Draw(TextureData.Texture, Position, null, Color * alpha, AngleRads, TextureData.TextureOrigin, currentSize, SpriteEffects.None, 0f);
        }

        #endregion Draw

        #region AlphaValueFunctions

        /// <summary>
        /// Fade out linearly over time.
        /// </summary>
        /// 1 - x
        public static float LinearFadeOutAlphaValue(float currentLifespanFraction)
        {
            return 1.0f - currentLifespanFraction;
        }

        /// <summary>
        /// Fade out using normalized sigmoid function (S-curve) over time.
        /// </summary>
        /// 1 - (1 / (1 + e^(-12x + 6)))
        public static float SigmoidFadeOutAlphaValue(float currentLifespanFraction)
        {
            float sig = (1.0f / (1.0f + (float)Math.Pow(Math.E, (-12.0f * currentLifespanFraction) + 6.0f))); 
            return 1.0f - sig;
        }

        /// <summary>
        /// Fade out quickly at first, and then slowly (lingers at low alpha).
        /// </summary>
        /// -.1log (x)
        public static float FadeOutQuickThenSlowAlphaValue(float currentLifespanFraction)
        {
            return (float)(-0.1f * Math.Log10(currentLifespanFraction));
        }

        /// <summary>
        /// Fade from no alpha to full alpha, and then to no alpha again (quadratic).
        /// </summary>
        /// -4 * (x - .5)^2 + 1
        public static float FadeInThenOutAlphaValue(float currentLifespanFraction)
        {
            return (-4.0f * (float)Math.Pow(currentLifespanFraction - .5f, 2.0f) + 1.0f);
        }

        /// <summary>
        /// Fade from no alpha to .5 alpha, and then to no alpha again (quadratic).
        /// </summary>
        /// -2 * (x - .5)^2 + .5
        public static float HalfFadeInThenOutAlphaValue(float currentLifespanFraction)
        {
            return (-2.0f * (float)Math.Pow(currentLifespanFraction - .5f, 2.0f) + .5f);
        }

        /// <summary>
        /// Fade in and out many times.
        /// </summary>
        /// .5 sin(PI * (f * 2) * x) + .5
        public static float PulseFadeInAndOutAlphaValue(float currentLifespanFraction)
        {
            float timesToFadeInAndOut = 5.0f;
            return (.5f * (float)Math.Sin(Math.PI * (timesToFadeInAndOut * 2.0f) * currentLifespanFraction)) + .5f;
        }

        /// <summary>
        /// Fade in and out many times. Not visible half the time. 
        /// After each fade out, alpha remains zero for a time.
        /// </summary>
        /// sin(PI * (f * 2) * x)
        public static float HalfVisiblePulseFadeInAndOutAlphaValue(float currentLifespanFraction)
        {
            float timesToFadeInAndOut = 5;
            return (float)Math.Max(0.0f, Math.Sin(Math.PI * (timesToFadeInAndOut * 2.0f) * currentLifespanFraction));
        }        

        #endregion AlphaValueFunctions
    }
}