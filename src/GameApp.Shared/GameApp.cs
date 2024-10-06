using GameApp.Shared.GameLogic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharedGameLogic.GameLogic;
using System;
using System.Collections.Generic;

namespace GameApp.Shared;

/// <summary>
/// This is the main type for your game.
/// </summary>
public class GameApp : Game
{
    GraphicsDeviceManager? graphics;
    SpriteBatch? spriteBatch;
    StarCamera? starCamera;
    ThreeDDebugGrid? threeDDebugGrid;
    StarSource? starSource;
    ClickDetectionGrid? clickDetectionGrid;

    public GameApp()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

    public void UpdateWebInput(IDictionary<string, object> input)
    {
    }

    /// <summary>
    /// Allows the game to perform any initialization it needs to before starting to run.
    /// This is where it can query for any required services and load any non-graphic
    /// related content.  Calling base.Initialize will enumerate through any components
    /// and initialize them as well.
    /// </summary>
    protected override void Initialize()
    {
        starCamera = new StarCamera(Vector3.Zero, Vector3.Forward, Vector3.Up);
        threeDDebugGrid = new ThreeDDebugGrid();
        starSource = new StarSource();
        clickDetectionGrid = new ClickDetectionGrid();

        base.Initialize();

    }

    /// <summary>
    /// LoadContent will be called once per game and is the place to load
    /// all of your content.
    /// </summary>
    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        if (starSource is null)
            throw new ArgumentNullException(nameof(starSource));

        starSource.LoadContent(GraphicsDevice);

        if (starCamera is null)
            throw new ArgumentNullException(nameof(starCamera));

        if (threeDDebugGrid is null)
            throw new ArgumentNullException(nameof(threeDDebugGrid));
         
        threeDDebugGrid.LoadContent(GraphicsDevice, starCamera);
    }

    /// <summary>
    /// UnloadContent will be called once per game and is the place to unload
    /// game-specific content.
    /// </summary>
    protected override void UnloadContent()
    {
        // TODO: Unload any non ContentManager content here
    }


    MouseState previousMouseState;
    /// <summary>
    /// Allows the game to run logic such as updating the world,
    /// checking for collisions, gathering input, and playing audio.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Update(GameTime gameTime)
    {
        this.IsMouseVisible = true;

        MouseState currentMouseState = Mouse.GetState();
        KeyboardState keyboardState = Keyboard.GetState();
        GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);

        if (keyboardState.IsKeyDown(Keys.Escape) ||
            keyboardState.IsKeyDown(Keys.Back) ||
            gamePadState.Buttons.Back == ButtonState.Pressed)
        {
            try { Exit(); }
            catch (PlatformNotSupportedException) { /* ignore */ }
        }

        if (clickDetectionGrid is null)
            throw new ArgumentNullException(nameof(clickDetectionGrid));

        clickDetectionGrid.Update(currentMouseState, previousMouseState);

        if (starCamera is null)
            throw new ArgumentNullException(nameof(starCamera));

        starCamera.Update(Keyboard.GetState(), gameTime);

        if (threeDDebugGrid is null)
            throw new ArgumentNullException(nameof(threeDDebugGrid));

        threeDDebugGrid.Update(starCamera, gameTime);

        previousMouseState = currentMouseState;

        base.Update(gameTime);
    }

    /// <summary>
    /// This is called when the game should draw itself.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        if (threeDDebugGrid is null)
            throw new ArgumentNullException(nameof(threeDDebugGrid));

        threeDDebugGrid.Draw(GraphicsDevice);

        if (starCamera is null)
            throw new ArgumentNullException(nameof(starCamera));

        if (spriteBatch is null)
            throw new ArgumentNullException(nameof(spriteBatch));

        spriteBatch.Begin();

        if (starSource is null)
            throw new ArgumentNullException(nameof(starSource));

        if (clickDetectionGrid is null)
            throw new ArgumentNullException(nameof(clickDetectionGrid));

        starCamera.Draw(GraphicsDevice, spriteBatch, starSource, clickDetectionGrid);

        spriteBatch.End();

        base.Draw(gameTime);
    }
}
