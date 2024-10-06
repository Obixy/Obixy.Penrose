using GameApp.Shared.GameLogic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

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

    public GameApp()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
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

        //_ = Content.Load<Effect>("NewEffect");
    }

    /// <summary>
    /// UnloadContent will be called once per game and is the place to unload
    /// game-specific content.
    /// </summary>
    protected override void UnloadContent()
    {
        // TODO: Unload any non ContentManager content here
    }

    /// <summary>
    /// Allows the game to run logic such as updating the world,
    /// checking for collisions, gathering input, and playing audio.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Update(GameTime gameTime)
    {
        MouseState mouseState = Mouse.GetState();
        KeyboardState keyboardState = Keyboard.GetState();
        GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);

        if (keyboardState.IsKeyDown(Keys.Escape) ||
            keyboardState.IsKeyDown(Keys.Back) ||
            gamePadState.Buttons.Back == ButtonState.Pressed)
        {
            try { Exit(); }
            catch (PlatformNotSupportedException) { /* ignore */ }
        }

        if (starCamera is null)
            throw new ArgumentNullException(nameof(starCamera));

        starCamera.Update(Keyboard.GetState(), gameTime);

        if (threeDDebugGrid is null)
            throw new ArgumentNullException(nameof(threeDDebugGrid));

        threeDDebugGrid.Update(starCamera, gameTime);

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

        if (spriteBatch is null)
            throw new ArgumentNullException(nameof(spriteBatch));

        spriteBatch.Begin();

        if (starCamera is null)
            throw new ArgumentNullException(nameof(starCamera));

        if (starSource is null)
            throw new ArgumentNullException(nameof(starSource));

        starCamera.Draw(GraphicsDevice, spriteBatch, starSource);
        
        spriteBatch.End();

        base.Draw(gameTime);
    }
}
