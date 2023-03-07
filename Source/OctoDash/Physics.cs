using System;
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
using Physics;


namespace Physics
{
    // TO DECIDE: move SoftBody class here?


    // make a bounded floor for playtesting
    public class Bounds
    {

        public Bounds(World world, float floor_length, float floor_thickness, float wall_height)
        {
            float vertical_offset = -7.0f;
            float boundDensity = 1.0f;
            float wall_horizontal_offset = (floor_length + floor_thickness) / 2.0f;
            float ceiling_height = vertical_offset + wall_height - floor_thickness / 2.0f;

            Body bottomBound = world.CreateRectangle(floor_length, floor_thickness, boundDensity, new Vector2(0.0f, vertical_offset + floor_thickness / 2.0f), 0.0f, BodyType.Static);
            Body topBound = world.CreateRectangle(Units.MonoGameToAether(floor_length), Units.MonoGameToAether(floor_thickness), boundDensity, Units.MonoGameToAether(0.0f, ceiling_height), 0.0f, BodyType.Static);
            Body leftBound = world.CreateRectangle(Units.MonoGameToAether(floor_thickness), Units.MonoGameToAether(wall_height), boundDensity, Units.MonoGameToAether(wall_horizontal_offset, vertical_offset + wall_height / 2.0f), 0.0f, BodyType.Static);
            Body rightBound = world.CreateRectangle(Units.MonoGameToAether(floor_thickness), Units.MonoGameToAether(wall_height), boundDensity, Units.MonoGameToAether(-wall_horizontal_offset, vertical_offset + wall_height / 2.0f), 0.0f, BodyType.Static);
        }

        public Bounds(World world, Vector2 topLeftInAether, Vector2 bottomRightInAether, float floor_thickness)
        {

            Vector2 floorCenter = new Vector2((topLeftInAether.X + bottomRightInAether.X) / 2.0f, bottomRightInAether.Y - floor_thickness / 2.0f);
            Vector2 ceilingCenter = new Vector2((topLeftInAether.X + bottomRightInAether.X) / 2.0f, topLeftInAether.Y + floor_thickness / 2.0f);

            Vector2 leftWallCenter = new Vector2(topLeftInAether.X - floor_thickness / 2.0f, (topLeftInAether.Y + bottomRightInAether.Y) / 2.0f);
            Vector2 rightWallCenter = new Vector2(bottomRightInAether.X + floor_thickness / 2.0f, (topLeftInAether.Y + bottomRightInAether.Y) / 2.0f);

            float floor_length = Math.Abs(topLeftInAether.X - bottomRightInAether.X) + 2*floor_thickness;
            float wall_height = Math.Abs(topLeftInAether.Y - bottomRightInAether.Y);

            float boundDensity = 1.0f;
            Body floor = setCollisionCat3(world.CreateRectangle(floor_length, floor_thickness, boundDensity, floorCenter, 0.0f, BodyType.Static), false);
            Body ceiling = setCollisionCat3(world.CreateRectangle(floor_length, floor_thickness, boundDensity, ceilingCenter, 0.0f, BodyType.Static), false);
            Body leftWall = setCollisionCat3(world.CreateRectangle(floor_thickness, wall_height, boundDensity, leftWallCenter, 0.0f, BodyType.Static), false);
            Body rightWall = setCollisionCat3(world.CreateRectangle(floor_thickness, wall_height, boundDensity, rightWallCenter, 0.0f, BodyType.Static), false);

        }


        public Body setCollisionCat3(Body body, bool stickable)
        {
            body.Tag = (stickable) ? "bounds stickable" : "bounds";
            body.SetCollisionCategories(Category.Cat3);
            return body;
        }
    }

}