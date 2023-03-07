using OctoDash;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using tainicom.Aether.Physics2D.Dynamics;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using tainicom.Aether.Physics2D.Dynamics.Contacts;
using System.Collections.Generic;
using System;
using System.Linq;

namespace OctoDash
{
     public class MovableEntity
    {
        private Vector2 targetPosition = new Vector2();
        private float time = 0.0f;
        private CatmullRomSpline spline;
        private float distance; 
        private Body body;

        private static Texture2D seahorse;
        private static Menu.LevelScreen level;
        private static float offsetX;
        private static float offsetY;


        public MovableEntity(Vector2[] path,World _world)
        {
            this.spline = new CatmullRomSpline(path,true);
            this.distance = spline.approxLength(100);
            body = _world.CreateEllipse(0.2f, 0.4f, 8, 1.0f, Vector2.Zero, 0, BodyType.Kinematic);
            body.SetCollisionCategories(Category.Cat3);
            body.Tag = "world stickable";
        }

        public void Update(GameTime gameTime)
        {
            time += (float)gameTime.ElapsedGameTime.TotalSeconds;

            float maxHuntTime = distance/2; // Hunt will last for 4 seconds, get a fraction value of this between 0 and 1. 
            float f = time / maxHuntTime;
            if (f <= 1.0f)
            {
                Vector2 bodyPosition = body.Position;
                targetPosition = spline.valueAt(targetPosition, f);
                Vector2 positionDelta = targetPosition-=bodyPosition;

                body.LinearVelocity = positionDelta * 5;
            }
            else
            {
                time = 0.0f;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var transformMatrix = level.cv.Camera.GetViewMatrix();

            spriteBatch.Begin(transformMatrix: transformMatrix);

            Vector2 position = new Vector2();
            position.X = offsetX;
            position.Y = offsetY;
            Vector2 bodyPosition = Units.AetherToMonogame(body.Position);
            spriteBatch.Draw(seahorse, new Rectangle((int)bodyPosition.X, (int)bodyPosition.Y, seahorse.Width, seahorse.Height), null, Color.White, 0, position, SpriteEffects.None, 0f);


            spriteBatch.End();

        }

        public static void LoadContent(ref OctoDash game, ref Menu.LevelScreen levelScreen)
        {

            level = levelScreen;
            seahorse = game.Content.Load<Texture2D>("underwater/seahorse");
            offsetX = seahorse.Width / 2;
            offsetY = seahorse.Height / 2;

        }

        public static MovableEntity[] createEntities(TiledMapObjectLayer obstacleLayer, ref World world)
        {
            Dictionary<string, List<TiledMapObject>> temp = new Dictionary<string, List<TiledMapObject>>();
            foreach (TiledMapObject item in obstacleLayer.Objects)
            {
                try
                {
                    string id;
                    item.Properties.TryGetValue("id", out id);
                    if (temp.ContainsKey(id) == false)
                    {
                        temp.Add(id, new List<TiledMapObject>());
                    }
                    temp[id].Add(item);
                }
                catch
                {
                    throw new ArgumentException("Path Property not correctly set in tiled");
                }

            }
            List<TiledMapObject>[] paths = temp.Values.ToArray();

            MovableEntity[] entities = new MovableEntity[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                List<TiledMapObject> list = paths[i];
                Vector2[] path = new Vector2[list.Count];
                for (int j = 0; j < list.Count; j++)
                {
                    Vector2 position = list[j].Position;
                    path[j] = Units.MonoGameToAether(position);
                }
                entities[i] = new MovableEntity(path, world);
            }
            return entities;
        }


    }

}
