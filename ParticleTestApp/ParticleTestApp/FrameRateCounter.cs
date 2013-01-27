#region --- Using Statements ---

using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

#endregion


namespace IconArena
{
    public class FrameRateCounter : DrawableGameComponent
    {
        #region Fields

        ContentManager content;
        SpriteBatch spriteBatch;
        SpriteFont spriteFont;

        int drawFrameRate = 0;
        int drawFrameCounter = 0;

        int updateRate = 0;
        int updateCounter = 0;

        TimeSpan elapsedTime = TimeSpan.Zero;

        #endregion //Fields

        #region Initialization

        public FrameRateCounter(Game game)
            : base(game)
        {
            content = new ContentManager(game.Services);
        }


        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteFont = content.Load<SpriteFont>("Content/debugfont");
        }


        protected override void UnloadContent()
        {
            content.Unload();
        }

        #endregion //Initialization

        #region Update

        public override void Update(GameTime gameTime)
        {
            updateCounter++;
            elapsedTime += gameTime.ElapsedGameTime;

            if (elapsedTime > TimeSpan.FromSeconds(1))
            {
                elapsedTime -= TimeSpan.FromSeconds(1);

                drawFrameRate = drawFrameCounter;
                drawFrameCounter = 0;

                updateRate = updateCounter;
                updateCounter = 0;
            }
        }

        #endregion //Update

        #region Draw

        public override void Draw(GameTime gameTime)
        {
            drawFrameCounter++;

            spriteBatch.Begin();

            StringBuilder frameRateInfo = new StringBuilder();
            frameRateInfo.AppendFormat("fps: {0}\n", drawFrameRate);
            frameRateInfo.AppendFormat("ups: {0}", updateRate);

            int numberOfLines = 2;

            float scale = 1.0f;

            Vector2 shadowedPosition = new Vector2(1, 768 - (spriteFont.LineSpacing * numberOfLines * scale));
            Vector2 position = new Vector2(shadowedPosition.X - 1, shadowedPosition.Y - 1);

            spriteBatch.DrawString(spriteFont, frameRateInfo.ToString(), shadowedPosition, Color.Black, 0.0f, Vector2.Zero, scale, SpriteEffects.None, 0.0f);
            spriteBatch.DrawString(spriteFont, frameRateInfo.ToString(), position, Color.White, 0.0f, Vector2.Zero, scale, SpriteEffects.None, 0.0f);

            spriteBatch.End();
        }

        #endregion //Draw
    }
}