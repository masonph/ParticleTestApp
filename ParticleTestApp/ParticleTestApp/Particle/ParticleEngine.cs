#region --- Using Statements ---
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace IconArena
{
    /// <summary>
    /// Emits particles at specified location.
    /// </summary>
    public class ParticleEngine
    {
        #region Fields

        private Random random = new Random();

        private IEmitterShape emitterShape;
        private Vector2 lastFrameEmitterLocation { get; set; }
        public Vector2 EmitterLocation 
        {
            get { return emitterShape.Position; }
            set { emitterShape.Position = value; }
        }

        private List<Particle> particles = new List<Particle>();
        private LinkedList<Particle> deadParticles = new LinkedList<Particle>();

        private List<ParticleTextureData> particleTextures = new List<ParticleTextureData>();
        public Color ParticleColor { get; set; }

        private int currentParticlesPerSecond;
        public int MinParticlesPerSecond { get; set; }
        public int MaxParticlesPerSecond { get; set; }
        /// <summary>
        /// Maximum number of particles allowed. Defaults to 10000.
        /// </summary>
        public int MaxParticles { get; set; }

        private float timeSinceLastParticle = 0.0f;
        public float MaxMoveSpeed { get; set; }
        public float MaxAngularSpeedRads { get; set; }
        public bool RandomInitialRotationEnabled { get; set; }

        public float MinTimeToLive { get; set; }
        public float MaxTimeToLive { get; set; }

        public float MinSizeScale { get; set; }
        public float MaxSizeScale { get; set; }
        /// <summary>
        /// The growth rate of the particles in the system.
        /// Initial size is multiplied by the rate every second (smoothly).
        /// </summary>
        /// <remarks>For example, if SizeChangeRatePerSecond is 2.0f, size of the particle will double every second (smoothly).</remarks>
        public float SizeChangeRatePerSecond { get; set; }

        public bool ShouldInterpolatePositions { get; set; }

        /// <summary>
        /// Function defining the alpha value of particles created throughout the particle lifespan.
        /// See Particle.AlphaValueDelegate definition for more info.
        /// </summary>
        public AlphaValueDelegate AlphaValueFunction { get; set; }

        /// <summary>
        /// The uniform acceleration applied to all particles each update.
        /// </summary>
        public Vector2 UniformVectorFieldAcceleration { get; set; }

        /// <summary>
        /// The velocity added to each particle upon creation (additive to initial random velocity already applied).
        /// </summary>
        public Vector2 DirectionalVelocity { get; set; }

        private List<ParticleAttractor> attractors = new List<ParticleAttractor>();

        #endregion Fields

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public ParticleEngine(List<Texture2D> textures, Color particleColor, IEmitterShape emitterShape, Vector2 location, float maxRandomSpeed, 
            float maxAngularSpeed, int minParticlesPerSecond, int maxParticlesPerSecond, float minTimeToLive, float maxTimeToLive, float minSizeScale, float maxSizeScale)
        {
            if (minParticlesPerSecond > maxParticlesPerSecond)
                throw new ArgumentOutOfRangeException("MinPPS must be less than or equal to MaxPPS.");
            if (maxParticlesPerSecond <= 0)
                throw new ArgumentOutOfRangeException("MaxPPS must be greater than zero.");
            if (minTimeToLive > maxTimeToLive)
                throw new ArgumentOutOfRangeException("MinTimeToLive must be less than or equal to MaxTimeToLive.");
            if (minSizeScale > maxSizeScale)
                throw new ArgumentOutOfRangeException("MinSizeScale must be less than or equal to MaxSizeScale.");

            RandomInitialRotationEnabled = false;
            ShouldInterpolatePositions = false;
            this.MaxParticles = 10000;
            this.SizeChangeRatePerSecond = 1.0f;
            this.ParticleColor = particleColor;
            this.emitterShape = emitterShape;
            EmitterLocation = location;
            lastFrameEmitterLocation = EmitterLocation;
            this.MaxMoveSpeed = maxRandomSpeed;
            this.MaxAngularSpeedRads = maxAngularSpeed;
            this.MinParticlesPerSecond = minParticlesPerSecond;
            this.MaxParticlesPerSecond = maxParticlesPerSecond;
            this.MinTimeToLive = minTimeToLive;
            this.MaxTimeToLive = maxTimeToLive;
            this.MinSizeScale = minSizeScale;
            this.MaxSizeScale = maxSizeScale;

            foreach (Texture2D texture in textures)
            {
                this.particleTextures.Add(new ParticleTextureData() { Texture = texture, TextureOrigin = new Vector2(texture.Width / 2.0f, texture.Height / 2.0f) } );
            }
        }

        #endregion Initialization

        #region Update

        /// <summary>
        /// Updates the Particle Engine.
        /// </summary>
        public void Update(GameTime gameTime, bool shouldCreateNewParticles)
        {
            Vector2 currFrameEmitterLocation = EmitterLocation; // Store current position so the emitter location can be moved this update for position interpolation
            // To-Do: Old particles should be removed before adding new ones so that particles aren't prevented
            // from being created because an expired particle is being counted
            if (shouldCreateNewParticles && particles.Count < MaxParticles)
            {
                float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
                timeSinceLastParticle += elapsed;
                currentParticlesPerSecond = (int)(((float)random.NextDouble() * (MaxParticlesPerSecond - MinParticlesPerSecond)) + MinParticlesPerSecond);

                float timeBetweenParticles = 1 / (float)currentParticlesPerSecond;
                int particlesToCreateThisUpdate = (int) (timeSinceLastParticle / timeBetweenParticles);
                particlesToCreateThisUpdate = Math.Min(particlesToCreateThisUpdate, MaxParticles - particles.Count);
                timeSinceLastParticle -= (particlesToCreateThisUpdate * timeBetweenParticles);
                for (int i = 1; i <= particlesToCreateThisUpdate; i++)
                {
                    if (ShouldInterpolatePositions)
                    {
                        float positionLerp = (i / (float)particlesToCreateThisUpdate);
                        EmitterLocation = Vector2.Lerp(lastFrameEmitterLocation, currFrameEmitterLocation, positionLerp);
                    }
                    GenerateNewParticle(emitterShape.GetPointFromShape());
                }
                EmitterLocation = currFrameEmitterLocation; // Restore current position (in case changed by position interpolation)
            }
            else
            {
                timeSinceLastParticle = 0.0f;
            }

            // Update particles
            for (int i = 0; i < particles.Count; i++)
            {
                particles[i].Update(gameTime, UniformVectorFieldAcceleration, attractors);
            }

            // Remove expired particles and add them to the dead particle list
            Predicate<Particle> isExpiredPredicate = particle => (particle.IsExpired == true);
            List<Particle> particlesToRemove = particles.FindAll(isExpiredPredicate);
            for (int i = 0; i < particlesToRemove.Count; i++)
            {
                deadParticles.AddLast(particlesToRemove[i]);
            }
            particles.RemoveAll(isExpiredPredicate);

            lastFrameEmitterLocation = EmitterLocation;
        }

        /// <summary>
        /// Generates a new Particle for use by the Particle Engine.
        /// </summary>
        private void GenerateNewParticle(Vector2 initialPosition)
        {
            ParticleTextureData textureData = particleTextures[random.Next(particleTextures.Count)];

            Vector2 velocity = new Vector2((float)(random.NextDouble() * 2.0f - 1.0f), (float)(random.NextDouble() * 2.0f - 1.0f));
            if (velocity != Vector2.Zero)
                velocity.Normalize();

            velocity *= MaxMoveSpeed * (float)random.NextDouble();

            velocity += DirectionalVelocity;

            float angleRads = 0.0f;
            if (RandomInitialRotationEnabled)
                angleRads = (float)(random.NextDouble() * MathHelper.TwoPi);
            float angularVelocityRads = ((float)(random.NextDouble() * 2.0f - 1.0f)) * MaxAngularSpeedRads;
            float size = ((float)random.NextDouble() * (MaxSizeScale - MinSizeScale)) + MinSizeScale;
            float timeToLive = ((float)random.NextDouble() * (MaxTimeToLive - MinTimeToLive)) + MinTimeToLive;

            if (deadParticles.Count == 0)
            {
                Particle newParticle = new Particle(textureData, initialPosition, velocity, angleRads, angularVelocityRads, ParticleColor, size, 
                    SizeChangeRatePerSecond, timeToLive, AlphaValueFunction);
                particles.Add(newParticle);
            }
            else
            {
                Particle recycledParticle = deadParticles.Last();
                deadParticles.RemoveLast();
                recycledParticle.Reset(textureData, initialPosition, velocity, angleRads, angularVelocityRads, ParticleColor, size,
                    SizeChangeRatePerSecond, timeToLive, AlphaValueFunction);
                particles.Add(recycledParticle);
            }
        }

        #endregion Update

        #region StateChanges

        /// <summary>
        /// Add the particle attractor to the ParticleEngine.
        /// </summary>
        public void AddAttractor(ParticleAttractor attractor)
        {
            attractors.Add(attractor);
        }

        /// <summary>
        /// Remove the particle attractor from the ParticleEngine.
        /// </summary>
        public void RemoveAttractor(ParticleAttractor attractor)
        {
            attractors.Remove(attractor);
        }

        /// <summary>
        /// Remove all particle attractors from the ParticleEngine.
        /// </summary>
        public void RemoveAllAttractors()
        {
            attractors.Clear();
        }

        #endregion StateChanges

        #region Draw

        /// <summary>
        /// Draws all the Particles of the Particle Engine.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (Particle particle in particles)
            {
                particle.Draw(spriteBatch);
            }

#if DEBUG
            DrawAttractorPositions(spriteBatch);
#endif //DEBUG
        }

        #endregion Draw

        #region DebugUtil

#if DEBUG
        public void DrawAttractorPositions(SpriteBatch spriteBatch)
        {
            foreach (ParticleAttractor attractor in attractors)
            {
                spriteBatch.Draw(particleTextures[0].Texture, attractor.Position, null, Color.White, 0.0f, particleTextures[0].TextureOrigin, .4f, SpriteEffects.None, 0.0f);
            }
        }
#endif //DEBUG

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("EmitterLocation: {0}\n", EmitterLocation);
            sb.AppendFormat("MinPPS: {0}\n", MinParticlesPerSecond);
            sb.AppendFormat("MaxPPS: {0}\n", MaxParticlesPerSecond);
            sb.AppendFormat("CurrentPPS: {0}\n", currentParticlesPerSecond);
            sb.AppendFormat("MaxParticles: {0}\n", MaxParticles);
            sb.AppendFormat("TTL Range: {0} - {1}\n", MinTimeToLive, MaxTimeToLive);
            sb.AppendFormat("Size Range: {0} - {1}\n", MinSizeScale, MaxSizeScale);
            sb.AppendFormat("MaxMoveSpeed: {0}\n", MaxMoveSpeed);
            sb.AppendFormat("MaxAngularSpeed: {0}\n", MaxAngularSpeedRads);
            sb.AppendFormat("Interp. Positions: {0}\n", ShouldInterpolatePositions);
            sb.AppendFormat("AlphaValueFunction: {0}\n", (AlphaValueFunction == null) ? "1.0f" : AlphaValueFunction.Method.Name);
            sb.AppendFormat("UVF Accel: {0}\n", UniformVectorFieldAcceleration);
            sb.AppendFormat("Direct. Velocity: {0}\n", DirectionalVelocity);
            sb.AppendFormat("Particle Count: {0}\n", particles.Count);
            sb.AppendFormat("Dead Part. Count: {0}\n", deadParticles.Count);
            sb.AppendFormat("Num. Attractors: {0}\n", attractors.Count);
            sb.AppendFormat("Color: {0}", ParticleColor);

            return sb.ToString();
        }

        #endregion DebugUtil
    }
}