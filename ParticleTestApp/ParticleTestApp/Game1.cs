using System;
using System.Collections.Generic;
using IconArena;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ParticleTestApp
{
    /// <summary>
    /// Environment to test particle effect function and performance. Diagnostic info is given
    /// some basic parameters can by changed during runtime via input.
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager graphics;
        private InputState input = new InputState();
        private SpriteBatch spriteBatch;
        private SpriteFont debugFont;

        private Vector2 mousePosition;
        private Texture2D mousePointerTexture;

        private ParticleEngine particleEffect;

        private int currentAlphaFuncIndex = 0;
        private AlphaValueDelegate[] alphaFunctions;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 768;
            Content.RootDirectory = "Content";

#if DEBUG
            Components.Add(new FrameRateCounter(this));
#endif //DEBUG
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            alphaFunctions = new AlphaValueDelegate[] { null, Particle.FadeInThenOutAlphaValue, Particle.FadeOutQuickThenSlowAlphaValue, 
                Particle.HalfFadeInThenOutAlphaValue, Particle.HalfVisiblePulseFadeInAndOutAlphaValue, Particle.LinearFadeOutAlphaValue, 
                Particle.PulseFadeInAndOutAlphaValue, Particle.SigmoidFadeOutAlphaValue };
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            debugFont = Content.Load<SpriteFont>("debugfont");
            mousePointerTexture = Content.Load<Texture2D>("MouseCursor");

            List<Texture2D> particleTextures = new List<Texture2D>();
            //particleTextures.Add(Content.Load<Texture2D>("Smoke"));
            particleTextures.Add(Content.Load<Texture2D>("Smoke2"));
            IEmitterShape circleEmitterShape = new CircleEmitterShape(0.0f, 0.0f);
            particleEffect = new ParticleEngine(particleTextures, Color.White, circleEmitterShape, Vector2.Zero, 0, 0, 4000, 5000, 5.1f, 7.3f, .2f, .4f);
            particleEffect.MaxParticles = 1000000;
            particleEffect.RandomInitialRotationEnabled = true;
            //particleEffect.SizeChangeRatePerSecond = 1.5f;
            particleEffect.AlphaValueFunction += Particle.FadeOutQuickThenSlowAlphaValue;

            // Set the current alpha function index
            for (int i = 0; i < alphaFunctions.Length; i++)
            {
                if (alphaFunctions[i] == particleEffect.AlphaValueFunction)
                {
                    currentAlphaFuncIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
        }

        float timeSinceLastUpdate = 0.0f;
        double updateTime = 0.0f;
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            input.Update(gameTime);

            // Allows the game to exit
            if (input.IsExitGame())
                this.Exit();

            MouseState mouseState = input.CurrentMouseState;
            mousePosition = new Vector2(mouseState.X, mouseState.Y);

            #region ParticleInput
            particleEffect.MinParticlesPerSecond += 10 * input.MinPPSChange();
            particleEffect.MaxParticlesPerSecond += 10 * input.MaxPPSChange();
            particleEffect.MaxParticles += 200 * input.MaxParticlesChange();
            particleEffect.MinTimeToLive += .025f * input.MinTTLChange();
            particleEffect.MaxTimeToLive += .025f * input.MaxTTLChange();
            particleEffect.MinSizeScale += .01f * input.MinSizeChange();
            particleEffect.MaxSizeScale += .01f * input.MaxSizeChange();
            particleEffect.MaxMoveSpeed += 1.0f * input.MaxMoveSpeedChange();
            particleEffect.MaxAngularSpeedRads += .1f * input.MaxAngularSpeedChange();

            if (input.InterpolatePositionsChange())
                particleEffect.ShouldInterpolatePositions = !particleEffect.ShouldInterpolatePositions;

            int alphaFuncChange =  input.AlphaFuncChange();
            if (alphaFuncChange != 0)
            {
                currentAlphaFuncIndex += alphaFuncChange;
                while (currentAlphaFuncIndex < 0)
                    currentAlphaFuncIndex = alphaFunctions.Length + currentAlphaFuncIndex;
                currentAlphaFuncIndex = currentAlphaFuncIndex % alphaFunctions.Length;
                particleEffect.AlphaValueFunction = alphaFunctions[currentAlphaFuncIndex];
            }

            Vector2 currAccel = particleEffect.UniformVectorFieldAcceleration;
            particleEffect.UniformVectorFieldAcceleration = currAccel + (1.0f * new Vector2((float)input.AccelXChange(), (float)input.AccelYChange()));
            Vector2 currVel = particleEffect.DirectionalVelocity;
            particleEffect.DirectionalVelocity = currVel + (1.0f * new Vector2((float)input.DirecVelXChange(), (float)input.DirecVelYChange()));

            Vector3 currColor = particleEffect.ParticleColor.ToVector3();
            currColor += .005f * new Vector3(input.ColorRedChange(), input.ColorGreenChange(), input.ColorBlueChange());
            particleEffect.ParticleColor = new Color(currColor);

            if (input.PlaceAttractor())
                particleEffect.AddAttractor(new ParticleAttractor() { Position = mousePosition, Force = 200.0f, MaxDistance = 1000 });
            else if (input.RemoveAllAttractors())
                particleEffect.RemoveAllAttractors();

            #endregion ParticleInput

            particleEffect.EmitterLocation = mousePosition;

            bool shouldCreateNewParticles = (mouseState.LeftButton == ButtonState.Pressed);
            timeSinceLastUpdate += elapsed;
            DateTime start = DateTime.UtcNow;

            particleEffect.Update(gameTime, shouldCreateNewParticles);

            DateTime end = DateTime.UtcNow;
            if (timeSinceLastUpdate >= 1.0f)
            {
                timeSinceLastUpdate = timeSinceLastUpdate - 1.0f;
                updateTime = (end - start).TotalMilliseconds; // Store time taken to update particles (once per second)
            }

            //float t = (float)gameTime.TotalGameTime.TotalMilliseconds;
            //Vector2 s = new Vector2(100, (float)Math.Sin(t / 100.0f) * 100);
            //Vector2 r = new Vector2((float)Math.Cos(t / 100.0f) * 1000, (float)Math.Sin(t / 100.0f) * 1000);
            //particleEffect.DirectionalVelocity = s;
            //particleEffect.UniformVectorFieldAcceleration = r;

            base.Update(gameTime);
        }

        float timeSinceLastDraw = 0.0f;
        double drawTime = 0.0f;
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            timeSinceLastDraw += elapsed;

            GraphicsDevice.Clear(Color.Gray);

            spriteBatch.Begin();

            spriteBatch.Draw(mousePointerTexture, mousePosition, null, Color.White);

            timeSinceLastUpdate += (float)gameTime.ElapsedGameTime.TotalSeconds;
            DateTime start = DateTime.UtcNow;

            particleEffect.Draw(spriteBatch);

            DateTime end = DateTime.UtcNow;

            if (timeSinceLastUpdate >= 1.0f)
            {
                timeSinceLastUpdate = timeSinceLastUpdate - 1.0f;
                drawTime = (end - start).TotalMilliseconds; // Store time taken to draw particles (once per second)
            }

            DrawParticleEffectInfo(spriteBatch, debugFont);

            spriteBatch.DrawString(debugFont, "Update: " + updateTime + " ms.", new Vector2(900, 720), Color.White);
            spriteBatch.DrawString(debugFont, "Draw: " + drawTime + " ms.", new Vector2(900, 740), Color.White);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawParticleEffectInfo(SpriteBatch spriteBatch, SpriteFont debugFont)
        {
            spriteBatch.DrawString(debugFont, particleEffect.ToString(), new Vector2(0, 0), Color.Green);
        }
    }
}
