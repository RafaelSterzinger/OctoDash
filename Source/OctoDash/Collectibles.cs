using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Content;
using MonoGame.Extended.Serialization;
using MonoGame.Extended.Sprites;
using MonoGame.Extended.Tiled;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Dynamics.Contacts;
using System.Collections.Generic;
using System.Linq;
using System;

// Collectible logic
public class Collectibles
{
    public OctoDash.OctoDash game;
    private Menu.LevelScreen levelScreen;
    private TiledMap tiledMap;

    private SpriteFont font;
    private Texture2D arrow;

   // private CollectibleItem starfish;
    private CollectibleItem crab;

    private Vector2? targetPosition;
    private bool targetOffScreen = false;

    public int CollectedCrabs
    {
        get { return crab.numCollected; }
        set { crab.numCollected = value; }
    }
    public int TotalCrabs
    {
        get { return crab.initialObjects.Length; }
    }

  //  public int CollectedStars
  //  {
  //      get { return starfish.numCollected; }
  //      set { starfish.numCollected = value; }
  //  }
    


    public Collectibles(OctoDash.OctoDash _game, TiledMap _tiledMap, Menu.LevelScreen _levelScreen)
    {
        game = _game;
        tiledMap = _tiledMap;
        levelScreen = _levelScreen;
    }

    public void LoadContent()
    {
        // Get Tiled layers
       // TiledMapObjectLayer starfishLayer = tiledMap.GetLayer<TiledMapObjectLayer>("starfishLayer");
        TiledMapObjectLayer crabLayer = tiledMap.GetLayer<TiledMapObjectLayer>("crabLayer");

        // Create collectible items
       // SpriteSheet starfishSpriteSheet = game.Content.Load<SpriteSheet>("underwater/starfish_animation.sf", new JsonContentLoader());
       // starfish = new CollectibleItem(game, starfishLayer.Objects, new AnimatedSprite(starfishSpriteSheet), starfishLayer.Objects.Length, 0);

        SpriteSheet crabSpriteSheet = game.Content.Load<SpriteSheet>("underwater/crab_animation.sf", new JsonContentLoader());
        crab = new CollectibleItem(game, crabLayer.Objects, new AnimatedSprite(crabSpriteSheet), 1, 1);

        // Make boxes for collision
        //starfish.CreateBoundingBoxes();
        crab.CreateBoundingBoxes();
        crab.WillRespawn = true;

        font = game.Content.Load<SpriteFont>("menu/sunnyspells50");
        arrow = game.Content.Load<Texture2D>("Arrow");
    }

    public void Update(GameTime gameTime)
    {
        float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        //starfish.sprite.Update(deltaSeconds);
        crab.sprite.Update(deltaSeconds);

        targetPosition = crab.GetNearestTargetPosition();
        if (targetPosition.HasValue)
        {
            targetOffScreen =
                targetPosition.Value.X <= levelScreen.cv.Camera.BoundingRectangle.Left ||
                targetPosition.Value.X >= levelScreen.cv.Camera.BoundingRectangle.Right ||
                targetPosition.Value.Y <= levelScreen.cv.Camera.BoundingRectangle.Top ||
                targetPosition.Value.Y >= levelScreen.cv.Camera.BoundingRectangle.Bottom;
        }
    }

    public void Draw(SpriteBatch _spriteBatch)
    {
        var transformMatrix = levelScreen.cv.Camera.GetViewMatrix();

        _spriteBatch.Begin(transformMatrix: transformMatrix);

        Vector2 position;
      //  foreach (TiledMapObject obj in starfish.remainingObjects)
      //  {
      //      position = new Vector2(obj.Position.X + starfish.offsetX, obj.Position.Y - starfish.offsetY);
      //      _spriteBatch.Draw(starfish.sprite, position);
      //  }

        foreach (TiledMapObject obj in crab.remainingObjects)
        {
            position = new Vector2(obj.Position.X + crab.offsetX, obj.Position.Y - crab.offsetY);
            _spriteBatch.Draw(crab.sprite, position);
        }

        _spriteBatch.End();

        Vector2 scale = new Vector2(0.7f, 0.7f);

        _spriteBatch.Begin();

        position = new Vector2(game.Window.ClientBounds.Width * 0.05f, game.Window.ClientBounds.Height * 0.05f);
        _spriteBatch.Draw(crab.sprite, position, 0f, scale);
        position.X += crab.offsetX * 1.1f;
        Vector2 textMiddlePoint = font.MeasureString("0") / 2;
        _spriteBatch.DrawString(font, " " + crab.numCollected.ToString() + "/" + crab.initialObjects.Length.ToString(), position, Color.Black, 0, textMiddlePoint, 1f, SpriteEffects.None, 0.5f);
        position.X += 2 * crab.offsetX * 1.1f;
     //   _spriteBatch.Draw(starfish.sprite, position, 0f, scale);
     //   position.X += starfish.offsetX * 1.1f;
     //   _spriteBatch.DrawString(font, "x" + starfish.numCollected.ToString(), position, Color.Cyan, 0, textMiddlePoint, 1f, SpriteEffects.None, 0.5f);

        if (targetPosition.HasValue && targetOffScreen)
        {
            float borderSize = 150f;
            Vector2 arrowPosition = levelScreen.cv.Camera.WorldToScreen(targetPosition.Value);
            if (arrowPosition.X <= borderSize) arrowPosition.X = borderSize;
            if (arrowPosition.X >= game.Window.ClientBounds.Width - borderSize) arrowPosition.X = game.Window.ClientBounds.Width - borderSize;
            if (arrowPosition.Y <= borderSize) arrowPosition.Y = borderSize;
            if (arrowPosition.Y >= game.Window.ClientBounds.Height - borderSize) arrowPosition.Y = game.Window.ClientBounds.Height - borderSize;

            Vector2 playerPosition = Units.AetherToMonogame(game.character.getPosition());
            float rotation = (float)Math.Atan2(targetPosition.Value.Y - playerPosition.Y, targetPosition.Value.X - playerPosition.X);

            Vector2 origin = new Vector2(arrow.Width / 2f, arrow.Height / 2f);

            _spriteBatch.Draw(arrow, new Rectangle((int)arrowPosition.X, (int)arrowPosition.Y, arrow.Width, arrow.Height), null, new Color(255, 80, 0, 255), rotation, origin, SpriteEffects.None, 0f);
        }

        _spriteBatch.End();
    }
}

internal class CollectibleItem
{
    internal int numCollected;
    internal TiledMapObject[] initialObjects;
    internal HashSet<TiledMapObject> remainingObjects;
    internal AnimatedSprite sprite;
    internal float offsetX;
    internal float offsetY;
    public int obj_id;
    public OctoDash.OctoDash game;
    private int nextStartIndex;
    private int spawnCount;
    private bool willRespawn = false;

    internal CollectibleItem(OctoDash.OctoDash _game, TiledMapObject[] objects, AnimatedSprite animatedSprite, int numToSpawn, int n)
    {
        game = _game;
        obj_id = n;
        numCollected = 0;
        try
        {
            initialObjects = objects.OrderBy(obj =>
            {
                string id;
                obj.Properties.TryGetValue("id", out id);
                return Int16.Parse(id);
            }).ToArray();
        }
        catch
        {
            initialObjects = objects.OrderBy(obj => new Random().Next()).ToArray();
        }
        remainingObjects = new HashSet<TiledMapObject>(initialObjects[0..numToSpawn]);
        nextStartIndex = numToSpawn;
        spawnCount = numToSpawn;
        sprite = animatedSprite;
        sprite.Play("animation");
        offsetX = sprite.GetBoundingRectangle(new MonoGame.Extended.Transform2()).Width / 2;
        offsetY = sprite.GetBoundingRectangle(new MonoGame.Extended.Transform2()).Height / 2;
    }

    internal bool WillRespawn
    {
        get => willRespawn;
        set => willRespawn = value;
    }

    internal Vector2? GetNearestTargetPosition()
    {
        Vector2 playerPosition = Units.AetherToMonogame(game.character.getPosition());
        float minDistance = float.PositiveInfinity;
        Vector2? targetPosition = null;
        foreach (TiledMapObject obj in remainingObjects)
        {
            float distance = Vector2.Distance(playerPosition, obj.Position);
            if (distance < minDistance)
            {
                minDistance = distance;
                targetPosition = obj.Position;
            }
        }
        return targetPosition;
    }

    internal void CreateBoundingBoxes()
    {
        foreach (TiledMapObject obj in remainingObjects)
        {
            Vector2 position = new Vector2(obj.Position.X + offsetX, obj.Position.Y - offsetY);
            Body box = game._world.CreateRectangle(Units.TiledToAether(1.0f), Units.TiledToAether(1.0f), 1.0f, Units.MonoGameToAether(position), 0.0f, BodyType.Static);
            box.SetCollisionCategories(Category.Cat3);
            box.OnCollision += OnCollision;
        }
    }

    private bool OnCollision(Fixture sender, Fixture other, Contact contact)
    {
        Vector2 objectPosition = Units.AetherToMonogame(sender.Body.Position);

        if (!(other.Body.Tag.GetType() == typeof(OctoDash.Softbody)))
        {
            return true;
        }

        foreach (TiledMapObject obj in initialObjects)
        {
            if (obj.Position == new Vector2(objectPosition.X - offsetX, objectPosition.Y + offsetY) && remainingObjects.Contains(obj))
            {
                if(obj_id == 0)
                {
                    game.PlaySound(1);
                }
                if(obj_id == 1)
                {
                    game.PlaySound(2);
                }
                
                numCollected++;
                remainingObjects.Remove(obj);
            }
        }

        sender.Body.World.RemoveAsync(sender.Body);

        if (remainingObjects.Count == 0 && willRespawn)
        {
            Respawn();
            CreateBoundingBoxes();
        }

        return true;
    }

    private void Respawn()
    {
        if (nextStartIndex >= initialObjects.Length) return;
        int currentStartIndex = nextStartIndex;
        nextStartIndex += spawnCount;
        if (nextStartIndex > initialObjects.Length) nextStartIndex = initialObjects.Length;
        remainingObjects = new HashSet<TiledMapObject>(initialObjects[currentStartIndex..nextStartIndex]);
    }
}
