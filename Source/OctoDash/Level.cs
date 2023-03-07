using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Dynamics.Contacts;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using MonoGame.Extended.Sprites;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using OctoDash;


// Tiled Map logic
// (orthographic camera is in CameraView)
public class Level
{
    public double spike_timer = 0f;
    public bool collision = false;
    public Vector2 velocity;
    private Menu.LevelScreen levelScreen;

    public int LevelId { get; set; }
    private string _levelMap;
    private TiledMap _tiledMap;
    private TiledMapRenderer _tiledMapRenderer;

    private TiledMapTileLayer floorLayer;
    private TiledMapTileset slopeSet;
    private int slopeSetId = 65;

    private TiledMapTileLayer spikeLayer;
    private TiledMapTileset spikeSet;
    private int spikeSetId;

    private List<Tuple<Body, Sprite>> _movableBodies;
    private List<BreakableWall> _walls;
    private Texture2D _background;
    Random _rand = new Random();

    public Vector2 startPos;

    public OctoDash.OctoDash game;

    private float timePassed = 0;
    private float time = 0;
    MovableEntity[] entities;

    public Collectibles collectibles;

    public float WorldWidthInPixels { get { return _tiledMap.WidthInPixels; } }

    public float WorldHeightInPixels { get { return _tiledMap.HeightInPixels; } }

    public float WorldWidthInTiles { get { return _tiledMap.Width; } }

    public float WorldHeigthInTiles { get { return _tiledMap.Height; } }

    public Level(OctoDash.OctoDash _game, Menu.LevelScreen _levelScreen, string levelMap, int levelID)
    {
        this.game = _game;
        this.levelScreen = _levelScreen;
        this._levelMap = (String.IsNullOrWhiteSpace(levelMap)) ? "underwater/underwater" : levelMap;
        this.LevelId = levelID;
    }

    public void Initialize()
    {
    }

    public void LoadContent()
    {
        _background = game.Content.Load<Texture2D>("underwater/underwater-bg");
        _tiledMap = game.Content.Load<TiledMap>(_levelMap);
        _tiledMapRenderer = new TiledMapRenderer(game.GraphicsDevice, _tiledMap);


        floorLayer = _tiledMap.GetLayer<TiledMapTileLayer>("platformLayer");
        slopeSet = game.Content.Load<TiledMapTileset>("underwater/slopes");
        slopeSetId = _tiledMap.GetTilesetFirstGlobalIdentifier(slopeSet);

        spikeLayer = _tiledMap.GetLayer<TiledMapTileLayer>("spikeLayer");
        spikeSet = game.Content.Load<TiledMapTileset>("underwater/spike_set");
        try
        {
            spikeSetId = _tiledMap.GetTilesetFirstGlobalIdentifier(spikeSet);
        }
        catch (Exception e)
        {
            Log.Logger.Log("Error when trying to get spikeSetId \n" + e.ToString());
        }

        collectibles = new Collectibles(game, _tiledMap, levelScreen);
        collectibles.LoadContent();

        TiledMapObjectLayer obstacleLayer = _tiledMap.GetLayer<TiledMapObjectLayer>("obstacleLayer");
        entities = MovableEntity.createEntities(obstacleLayer, ref game._world);
        MovableEntity.LoadContent(ref game, ref levelScreen);



        // SetWorldDimensions();
        CreateFloorHitBoxes();
        CreateBounds();
        _movableBodies = new List<Tuple<Body, Sprite>>();
        _walls = new List<BreakableWall>();

        // rock layer
        CreateObjects(_tiledMap.GetLayer<TiledMapObjectLayer>("rocks"), "underwater/rock2", true);

        var wallTexture = game.Content.Load<Texture2D>("underwater/rock1");
        foreach (var l in _tiledMap.GetLayer<TiledMapGroupLayer>("walls").Layers)
        {
            if (!(l is TiledMapObjectLayer))
            {
                Debug.WriteLine("WARNING: non-object layer in walls layer group");
                continue;
            }
            _walls.Add(new BreakableWall(wallTexture, (TiledMapObjectLayer)l, levelScreen, game));
        }

    }

    public void Update(GameTime gameTime)
    {
        _tiledMapRenderer.Update(gameTime);
        collectibles.Update(gameTime);
        time += (float)gameTime.ElapsedGameTime.TotalSeconds;
        foreach (MovableEntity entity in entities)
        {
            entity.Update(gameTime);

        }
        //collision with spike
        if (collision)
        {
            int num_a = game.character.NUM_ARMS;
            game.character.stunned = true;
            game.character.stun_start = gameTime.TotalGameTime.TotalSeconds;
            game.character.remove_sticky(1 % num_a);
            game.character.remove_sticky(2 % num_a);
            game.character.remove_sticky(3 % num_a);
            game.character.remove_sticky(4 % num_a);

            if (time - spike_timer < 0.25f)
            {
                Softbody body = game.character;
                body.applyForce(body.center, velocity * -120);
            }
            else
            {
                collision = false;
            }
        }


        foreach (var w in _walls)
        {
            w.Update();
        }
    }

    public void Draw(SpriteBatch _spriteBatch)
    {
        // draw static BG
        _spriteBatch.Begin();
        _spriteBatch.Draw(_background, new Rectangle(0, 0, game._graphics.PreferredBackBufferWidth, game._graphics.PreferredBackBufferHeight), Color.White);
        _spriteBatch.End();

        var transformMatrix = levelScreen.cv.Camera.GetViewMatrix();
        // TODO: this kinda works but makes it weird
        game.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

        _tiledMapRenderer.Draw(transformMatrix);
        collectibles.Draw(_spriteBatch);
        foreach (MovableEntity entity in entities)
        {
            entity.Draw(_spriteBatch);
        }

        /* _tiledMapRenderer.Draw(game.cv.View * game.cv.Projection); */

        /* _spriteBatch.Begin(transformMatrix: transformMatrix); */
        /* _spriteBatch.Draw(_player, pos, Color.White); */
        /* _spriteBatch.End(); */

        _spriteBatch.Begin(transformMatrix: transformMatrix);
        // Draw the movable objects as well as the walls
        foreach (var obj in _movableBodies)
        {
            var position = Units.AetherToMonogame(obj.Item1.Position);
            _spriteBatch.Draw(obj.Item2, position, -obj.Item1.Rotation);
        }
        foreach (var w in _walls)
        {
            for (int i = 0; i < w.bodies.Count; i++)
            {
                var position = Units.AetherToMonogame(w.bodies[i].Position);
                _spriteBatch.Draw(w.sprites[i], position, w.bodies[i].Rotation);
            }

        }
        _spriteBatch.End();

    }

    // assumes rectangular tilemaps
    private void CreateFloorHitBoxes()
    {
        for (ushort i = 0; i < WorldHeigthInTiles; ++i)
        {
            for (ushort j = 0; j < WorldWidthInTiles; ++j)
            {
                TiledMapTile? tile;
                if (floorLayer.TryGetTile(j, i, out tile))
                {
                    if (!tile.Value.IsBlank)
                    {
                        List<Vector2> vertices = new List<Vector2>();
                        Vector2 tileCenter = new Vector2(j + 0.5f, i + 0.5f);

                        switch (tile.Value.GlobalIdentifier)
                        {
                            case var value when value == slopeSetId:
                                // Side slope half
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, -0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, 0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0f, 0.5f)));
                                break;

                            case var value when value == slopeSetId + 1:
                                // Side slope other half
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, -0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, 0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0.5f, 0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0f, -0.5f)));
                                break;

                            case var value when value == slopeSetId + 2:
                                // Side slope full
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, -0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, 0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0.5f, 0.5f)));
                                break;

                            case var value when value == slopeSetId + 3:
                                // Slope half
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, 0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0.5f, 0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0.5f, 0f)));
                                break;

                            case var value when value == slopeSetId + 4:
                                // Slope other half
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, 0f)));
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, 0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0.5f, 0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0.5f, -0.5f)));
                                break;

                            case var value when value == slopeSetId + 5:
                                // Slope full
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, 0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0.5f, 0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0.5f, -0.5f)));
                                break;

                            case var value when value == slopeSetId + 6:
                                // Half square
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, 0f)));
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, 0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0.5f, 0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0.5f, 0f)));
                                break;

                            case var value when value == slopeSetId + 7:
                                // Side half square
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, -0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, 0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0f, 0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0f, -0.5f)));
                                break;

                            default:
                                // Square
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, -0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(-0.5f, 0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0.5f, -0.5f)));
                                vertices.Add(Units.TiledToAether(new Vector2(0.5f, 0.5f)));
                                break;
                        }

                        if (tile.Value.IsFlippedHorizontally)
                        {
                            for (int v = 0; v < vertices.Count; v++)
                            {
                                Vector2 vertex = vertices[v];
                                vertex.X *= -1;
                                vertices[v] = vertex;
                            }
                        }

                        if (tile.Value.IsFlippedVertically)
                        {
                            for (int v = 0; v < vertices.Count; v++)
                            {
                                Vector2 vertex = vertices[v];
                                vertex.Y *= -1;
                                vertices[v] = vertex;
                            }
                        }

                        Body polygon = game._world.CreatePolygon(new Vertices(vertices), 1.0f, Units.TiledToAether(tileCenter), 0.0f, BodyType.Static);
                        polygon.Tag = "world stickable";
                        polygon.SetFriction(2f);
                        polygon.SetCollisionCategories(Category.Cat3);
                    }
                }

                if (spikeLayer.TryGetTile(j, i, out tile))
                {
                    if (!tile.Value.IsBlank)
                    {
                        List<Vector2> vertices = new List<Vector2>();
                        Vector2 tileCenter = new Vector2(j + 0.5f, i + 0.5f);

                        if (tile.Value.GlobalIdentifier == spikeSetId || tile.Value.GlobalIdentifier == spikeSetId + 2)
                        {
                            // Full square
                            vertices.Add(Units.TiledToAether(new Vector2(-0.5f, -0.5f)));
                            vertices.Add(Units.TiledToAether(new Vector2(-0.5f, 0.5f)));
                            vertices.Add(Units.TiledToAether(new Vector2(0.5f, -0.5f)));
                            vertices.Add(Units.TiledToAether(new Vector2(0.5f, 0.5f)));
                        }
                        else
                        {
                            // Half square
                            vertices.Add(Units.TiledToAether(new Vector2(-0.5f, 0f)));
                            vertices.Add(Units.TiledToAether(new Vector2(-0.5f, 0.5f)));
                            vertices.Add(Units.TiledToAether(new Vector2(0.5f, 0.5f)));
                            vertices.Add(Units.TiledToAether(new Vector2(0.5f, 0f)));
                        }

                        if (tile.Value.IsFlippedDiagonally)
                        {
                            for (int v = 0; v < vertices.Count; v++)
                            {
                                Vector2 vertex = new Vector2(-vertices[v].Y, vertices[v].X);
                                vertices[v] = vertex;
                            }
                        }

                        if (tile.Value.IsFlippedHorizontally)
                        {
                            for (int v = 0; v < vertices.Count; v++)
                            {
                                Vector2 vertex = vertices[v];
                                vertex.X *= -1;
                                vertices[v] = vertex;
                            }
                        }

                        if (tile.Value.IsFlippedVertically)
                        {
                            for (int v = 0; v < vertices.Count; v++)
                            {
                                Vector2 vertex = vertices[v];
                                vertex.Y *= -1;
                                vertices[v] = vertex;
                            }
                        }

                        Body square = game._world.CreatePolygon(new Vertices(vertices), 1.0f, Units.TiledToAether(tileCenter), 0.0f, BodyType.Static);
                        square.SetCollidesWith(Category.Cat1);
                        square.OnCollision += OnCollision;
                    }
                }
            }
        }
    }

    private bool OnCollision(Fixture sender, Fixture other, Contact contact)
    {
        if (time - timePassed > 2f)
        {
            spike_timer = time;
            collision = true;
            velocity = Vector2.Normalize(game.character.center.LinearVelocity);
            this.game._stopWatch.addPenalty();
            int r = _rand.Next(1, 101);
            if (r == 100)
                game.PlaySound(10);
            else if (r == 99)
                game.PlaySound(11);
            else if (r < 50)
                game.PlaySound(8);
            else
                game.PlaySound(9);
            timePassed = time;
        }
        return true;

    }

    private void CreateBounds()
    {
        Vector2 topLeftInAether = Units.TiledToAether(new Vector2(0, 0));
        Vector2 bottomRightInAether = Units.TiledToAether(new Vector2(_tiledMap.Width, _tiledMap.Height));

        Physics.Bounds bounds = new Physics.Bounds(game._world, topLeftInAether, bottomRightInAether, 1.0f);
    }
    private void CreateObjects(TiledMapObjectLayer layer, string spriteName, bool breaking)
    {
        foreach (TiledMapObject obj in layer.Objects)
        {
            var stoneTexture = game.Content.Load<Texture2D>(spriteName);
            Sprite sprite = new Sprite(stoneTexture);
            Vector2 position = new Vector2(obj.Position.X + 64, obj.Position.Y - 64);
            float radius = (float)stoneTexture.Width / 2 * 0.7f;
            Body stone = game._world.CreateCircle(Units.MonoGameToAether(radius), 1.0f, Units.MonoGameToAether(position), BodyType.Dynamic);
            if (breaking)
                stone.Tag = "movable stickable break";
            else
                stone.Tag = "movable stickable";
            stone.Mass = 4f;
            stone.SetCollisionCategories(Category.Cat10);
            stone.SetFriction(10);
            _movableBodies.Add(Tuple.Create(stone, sprite));
            //stone.OnCollision += OnCollision;
        }
    }

}

public class BreakableWall
{
    private bool _breakNow;
    private bool _broken;
    public List<Body> bodies;
    private Menu.LevelScreen _screen;
    public List<Sprite> sprites;
    private Vector2 _contactPoint;
    private Vector2 _contactspeed;
    private Body _contactBody;
    private OctoDash.OctoDash _game;

    public BreakableWall(Texture2D texture, TiledMapObjectLayer layer, Menu.LevelScreen screen, OctoDash.OctoDash game)
    {
        this._broken = false;
        this._breakNow = false;
        this._screen = screen;
        sprites = new List<Sprite>();
        bodies = new List<Body>();
        Random rand = new Random();
        _game = game;
        foreach (TiledMapObject obj in layer.Objects)
        {
            //Sprite sprite = new Sprite(texture);
            Vector2 position = new Vector2(obj.Position.X + 64, obj.Position.Y - 64);
            float radius = (float)texture.Width / 2 * 0.7f;
            float rotation = (float)rand.NextDouble() * 2f * (float)Math.PI;
            Body stone = _game._world.CreateCircle(Units.MonoGameToAether(radius), 1.0f, Units.MonoGameToAether(position), BodyType.Static);
            stone.Rotation = rotation;
            stone.Mass = 4f;
            stone.Tag = "wall stickable";
            stone.SetCollisionCategories(Category.Cat3);
            bodies.Add(stone);
            sprites.Add(new Sprite(texture));
            stone.OnCollision += WallCollision;
        }
    }

    public void Update()
    {
        if (!_broken && _breakNow)
        {
            foreach (Body b in bodies)
            {
                b.BodyType = BodyType.Dynamic;
                b.Tag = "movable stickable break";
                b.SetCollisionCategories(Category.Cat10);
                var forceDirection = (b.Position - _contactPoint);
                forceDirection.Normalize();
                b.ApplyLinearImpulse(forceDirection);
                b.OnCollision -= WallCollision;
                b.SetFriction(10);
            }
            // play break sound
            _game.PlaySound(5);
            _broken = true;
            //_contactBody.ApplyForce(_contactspeed);
            //_contactBody.ApplyLinearImpulse(_contactspeed);
        }
    }

    private bool WallCollision(Fixture sender, Fixture other, Contact contact)
    {
        if (!_broken && other.Body.Tag != null && other.Body.Tag.ToString().Contains("break"))
        {
            // velocity check
            // fast hit: 4.5349426  -7.1809406

            //Debug.WriteLine(other.Body.LinearVelocity.Length());
            //Debug.Flush();
            if (other.Body.LinearVelocity.Length() > 4f)
            {
                _contactPoint = other.Body.Position;
                _contactspeed = other.Body.LinearVelocity;
                _breakNow = true;
                _contactBody = other.Body;
            }
        }

        return true;
    }

}