#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
#endregion

namespace ParticleTestApp
{
    public class InputState
    {
        #region Fields
        public const int MaxInputs = 1;
        public readonly KeyboardState[] CurrentKeyboardStates;
        public readonly GamePadState[] CurrentGamePadStates;
        public MouseState CurrentMouseState;

        public readonly KeyboardState[] LastKeyboardStates;
        public readonly GamePadState[] LastGamePadStates;
        public MouseState LastMouseState;

        public readonly bool[] GamePadWasConnected;
        #endregion

		#region Initialization


		/// <summary>
        /// Constructs a new input state.
        /// </summary>
        public InputState()
        {
            CurrentKeyboardStates = new KeyboardState[MaxInputs];
            CurrentGamePadStates = new GamePadState[MaxInputs];

            LastKeyboardStates = new KeyboardState[MaxInputs];
            LastGamePadStates = new GamePadState[MaxInputs];

            GamePadWasConnected = new bool[MaxInputs];
        }


        #endregion

        #region Public Methods

        /// <summary>
        /// Reads the latest state of the keyboard and gamepad.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            LastMouseState = CurrentMouseState;
            CurrentMouseState = Mouse.GetState();

            for (int i = 0; i < MaxInputs; i++)
            {
                LastKeyboardStates[i] = CurrentKeyboardStates[i];
                LastGamePadStates[i] = CurrentGamePadStates[i];
                

                CurrentKeyboardStates[i] = Keyboard.GetState((PlayerIndex)i);
                CurrentGamePadStates[i] = GamePad.GetState((PlayerIndex)i);
                

                // Keep track of whether a gamepad has ever been
                // connected, so we can detect if it is unplugged.
                if (CurrentGamePadStates[i].IsConnected)
                {
                    GamePadWasConnected[i] = true;
                }
            }
        }

        public bool IsKeyPress(Keys key, PlayerIndex playerIndex = PlayerIndex.One)
        {
            int i = (int)playerIndex;

            return CurrentKeyboardStates[i].IsKeyDown(key);
        }

        public bool IsNewKeyPress(Keys key, PlayerIndex playerIndex = PlayerIndex.One)
        {
            int i = (int)playerIndex;

            return (CurrentKeyboardStates[i].IsKeyDown(key) && LastKeyboardStates[i].IsKeyUp(key));
        }

        public bool IsNewButtonPress(Buttons button, PlayerIndex playerIndex = PlayerIndex.One)
        {
            int i = (int)playerIndex;

            return (CurrentGamePadStates[i].IsButtonDown(button) && LastGamePadStates[i].IsButtonUp(button));
        }

		public bool IsGamepadConnected(PlayerIndex? controllingPlayer)
		{
			return CurrentGamePadStates[(int)controllingPlayer].IsConnected;
		}

        public bool IsPauseGame()
        {
            return IsNewKeyPress(Keys.Enter) || IsNewButtonPress(Buttons.Start);
        }

        public bool IsExitGame()
        {
            return IsNewKeyPress(Keys.Escape) || IsNewButtonPress(Buttons.Back);
        }

        #endregion

        #region ParticleInput

        public int MinPPSChange()
        {
            if (IsKeyPress(Keys.D2))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public int MaxPPSChange()
        {
            if (IsKeyPress(Keys.D3))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public int MaxParticlesChange()
        {
            if (IsKeyPress(Keys.D4))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public int MinTTLChange()
        {
            if (IsKeyPress(Keys.Left))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public int MaxTTLChange()
        {
            if (IsKeyPress(Keys.Right))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public int MinSizeChange()
        {
            if (IsKeyPress(Keys.Down))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public int MaxSizeChange()
        {
            if (IsKeyPress(Keys.Up))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public int MaxMoveSpeedChange()
        {
            if (IsKeyPress(Keys.M))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public int MaxAngularSpeedChange()
        {
            if (IsKeyPress(Keys.A))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public bool InterpolatePositionsChange()
        {
            return IsNewKeyPress(Keys.L);
        }

        public int AlphaFuncChange()
        {
            if (IsNewKeyPress(Keys.T))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public int AccelXChange()
        {
            if (IsKeyPress(Keys.OemMinus))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public int AccelYChange()
        {
            if (IsKeyPress(Keys.OemPlus))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public int DirecVelXChange()
        {
            if (IsKeyPress(Keys.OemOpenBrackets))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public int DirecVelYChange()
        {
            if (IsKeyPress(Keys.OemCloseBrackets))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public int ColorRedChange()
        {
            if (IsKeyPress(Keys.Z))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public int ColorGreenChange()
        {
            if (IsKeyPress(Keys.X))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public int ColorBlueChange()
        {
            if (IsKeyPress(Keys.C))
            {
                return (IsKeyPress(Keys.LeftShift)) ? -1 : 1;
            }

            return 0;
        }

        public bool PlaceAttractor()
        {
            return !IsKeyPress(Keys.LeftShift) && (CurrentMouseState.RightButton == ButtonState.Pressed) && (LastMouseState.RightButton == ButtonState.Released);
        }

        public bool RemoveAllAttractors()
        {
            return IsKeyPress(Keys.LeftShift) && (CurrentMouseState.RightButton == ButtonState.Pressed) && (LastMouseState.RightButton == ButtonState.Released);
        }

        #endregion ParticleInput
    }
}
