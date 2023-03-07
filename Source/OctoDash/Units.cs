using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using tainicom.Aether.Physics2D.Dynamics;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using OctoDash;

public static class Units
{
    public static float TileWidth = 128;
    public static float TileHeight = 128;

    public static float a_meter = 1.0f / TileWidth;

    // coordinates of tiles in monogame coordinates
    public static Vector2 TiledToMonoGame(uint x, uint y)
    {
        return new Vector2(x * TileWidth, y * TileHeight);
    }

    // coordinates expressed in tiles
    public static Vector2 MonoGameToTiled(Vector2 coord)
    {
        // TODO: test if this rounding is as expected (visual collision bugs may stem from this)
        uint x = (ushort)(coord.X / TileWidth);
        uint y = (ushort)(coord.Y / TileHeight);
        return new Vector2(x, y);
    }

    // coordinates of tiles in aether coordinates
    public static Vector2 TiledToAether(Vector2 coord)
    {
        return new Vector2(coord.X, -coord.Y);
    }
    public static Vector2 TiledToAether(uint x, uint y)
    {
        return new Vector2(x, -y);
    }

    // make unit conversions explicit.
    public static float TiledToAether(float x)
    {
        return x;
    }

    // coordinates expressed in tiles
    public static Vector2 AetherToTiled(Vector2 coord)
    {
        // TODO: test if this rounding is as expected (visual collision bugs may stem from this)
        uint x = (ushort)(coord.X);
        uint y = (ushort)(-coord.Y);
        return new Vector2(x, y);
    }

    public static Vector2 MonoGameToAether(Vector2 coord)
    {
        float x = coord.X * a_meter;
        float y = -coord.Y * a_meter;
        return new Vector2(x, y);
    }
    public static Vector2 MonoGameToAether(float _x, float _y)
    {
        float x = _x;
        float y = -_y;
        return new Vector2(x, y);
    }

    public static float MonoGameToAether(float scale)
    {
        return scale * a_meter;
    }

    public static Vector2 AetherToMonogame(Vector2 coord)
    {
        float x = coord.X / a_meter;
        float y = -coord.Y / a_meter;
        return new Vector2(x, y);
    }

}