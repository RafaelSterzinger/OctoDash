using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Dynamics.Joints;
using tainicom.Aether.Physics2D.Diagnostics;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using MonoGame.Extended;
using MonoGame.Extended.ViewportAdapters;
using OctoDash;

public class CameraView
{

    private static float adjustedFieldOfView = 1f;
    private static float AdjustedFieldOfView
    {
        get
        {
            return adjustedFieldOfView;
        }
        set
        {
            adjustedFieldOfView = value;
            adjustedFieldOfViewWidth = adjustedFieldOfView * 1f;
            adjustedFieldOfViewHeight = adjustedFieldOfView * 1f;
        }
    }
    private static float adjustedFieldOfViewWidth = adjustedFieldOfView * 1f;
    private static float adjustedFieldOfViewHeight = adjustedFieldOfView * 1f;

    private Vector2 _position;
    private Vector2 _velocity = new Vector2();
    private float _breakingIntensity = 3f;
    public Matrix View { get; set; }
    public Matrix Projection { get; set; }
    public Vector2 _lower { get; set; }
    public Vector2 _upper { get; set; }
    public float _viewZoom { get; set; }
    public Vector2 _viewCenter { get; set; }
    public DebugView debugView { get; set; }
    public FixedMouseJoint _fixedMouseJoint { get; set; }


    public void adjust_FOV(float change)
    {
        AdjustedFieldOfView = MathHelper.Clamp(AdjustedFieldOfView + change, 0.4f, 10f);
    }



    public OctoDash.OctoDash game { get; set; }
    private Menu.LevelScreen levelScreen;

    public OrthographicCamera Camera { get; set; }

    public bool DrawDebug = false;

    public CameraView(OctoDash.OctoDash _game, Menu.LevelScreen _levelScreen)
    {
        this.game = _game;
        this.levelScreen = _levelScreen;
        if (game._world != null)
        {
            this.debugView = new DebugView(game._world);
            this.debugView.LoadContent(this.game.GraphicsDevice, this.game.Content);

        }

        this._position = (game != null && game.character != null) ? game.character.getPosition() : new Vector2();

        // float zoom = 2.0f;
        int boxWidth = (int)(game._graphics.PreferredBackBufferWidth * adjustedFieldOfViewWidth);
        int boxHeight = (int)(game._graphics.PreferredBackBufferHeight * adjustedFieldOfViewHeight);
        BoxingViewportAdapter _viewportAdapter = new BoxingViewportAdapter(game.Window, game.GraphicsDevice, boxWidth, boxHeight);
        // DefaultViewportAdapter _viewportAdapter = new DefaultViewportAdapter(game.GraphicsDevice);
        this.Camera = new OrthographicCamera(_viewportAdapter);
    }

    public void Update(GameTime gameTime)
    {
        UpdateProjection(gameTime);
    }

    public void Draw()
    {
        /* leave this block comment here please */
        /* game._spriteBatch.Begin(transformMatrix: Camera.GetViewMatrix()); */
        /* game._spriteBatch.Draw(game.player.Texture, game.player.Pos, Color.White); */
        /* game._spriteBatch.End(); */

        if (DrawDebug)
        {
            debugView.RenderDebugData(Projection, View);
        }
    }

    private Vector2 getForces(Vector2 position, Vector2 velocity)
    {
        Vector2 forces = new Vector2();
        if (game.character == null)
        {
            return forces;
        }
        Vector2 target = game.character.getPosition();
        Vector2 goal = (target - position);
        float factor = goal.LengthSquared();
        // forces += goal * factor * factor * factor; // follow center
        forces += goal * factor * factor;
        forces += -velocity * _breakingIntensity; // decelerate
        return forces;
    }

    // update position, velocity according to desired forces
    private void symplecticEuler(GameTime gameTime)
    {
        float _dt = gameTime.GetElapsedSeconds();
        Vector2 newPos = _position + _dt * _velocity;
        Vector2 newVel = _velocity + _dt * getForces(newPos, _velocity);

        _position = newPos;
        _velocity = newVel;
    }

    public void UpdateProjection(GameTime gameTime)
    {
        // update projection matrix of aether's debugView camera
        float boxWidth = (game._graphics.PreferredBackBufferWidth * adjustedFieldOfViewWidth) / Units.TileWidth / 2;
        float boxHeight = (game._graphics.PreferredBackBufferHeight * adjustedFieldOfViewHeight) / Units.TileHeight / 2;
        Projection = Matrix.CreateOrthographicOffCenter(-(boxWidth), (boxWidth), -(boxHeight), (boxHeight), 0f, 2f);

        if (game.character != null)
        {
            if (game.GameState.Equals(Constants.GameState.InGame))
            {
                // update position and velocity
                symplecticEuler(gameTime);
            }

            // monogame camera (non-shader sprites and tileMap)
            int cboxWidth = (int)(game._graphics.PreferredBackBufferWidth * adjustedFieldOfViewWidth);
            int cboxHeight = (int)(game._graphics.PreferredBackBufferHeight * adjustedFieldOfViewHeight);
            BoxingViewportAdapter _viewportAdapter = new BoxingViewportAdapter(game.Window, game.GraphicsDevice, cboxWidth, cboxHeight);
            this.Camera = new OrthographicCamera(_viewportAdapter);
            Camera.LookAt(Units.AetherToMonogame(_position));

            // aether debugView camera
            ViewCenter = _position;
        }

    }

    public Vector2 ViewCenter
    {
        get { return _viewCenter; }
        set
        {
            _viewCenter = value;
            UpdateView();
        }
    }

    private void UpdateView()
    {
        View = Matrix.CreateLookAt(new Vector3(ViewCenter, 1), new Vector3(ViewCenter, 0), Vector3.Up);
    }

    public Vector2 ConvertWorldToScreen(Vector2 position)
    {
        Vector3 temp = game.GraphicsDevice.Viewport.Project(new Vector3(position, 0), Projection, View, Matrix.Identity);
        return new Vector2(temp.X, temp.Y);
    }

    public Vector2 ConvertScreenToWorld(int x, int y)
    {
        Vector3 temp = game.GraphicsDevice.Viewport.Unproject(new Vector3(x, y, 0), Projection, View, Matrix.Identity);
        return new Vector2(temp.X, temp.Y);
    }
}
