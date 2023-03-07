using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using tainicom.Aether.Physics2D.Dynamics;
using OctoDash;

public class Player
{
    private OctoDash.OctoDash game;
    private Menu.LevelScreen levelScreen;

    private Vector2 _center;
    private Vector2 _drawPosition;
    public Vector2 Center
    {
        get
        {
            return _center;
        }
    }
    public Vector2 DrawPosition
    {
        get
        {
            return _drawPosition;
        }
    }
    public Vector2 Pos
    {
        set
        {
            _center = value;
            if (this.Texture != null)
            {
                _drawPosition.X = _center.X - this.Texture.Width / 2.0f;
                _drawPosition.Y = _center.Y - this.Texture.Height / 2.0f;
            }
        }
    }

    // public Vector2 Bound;
    public Vector2 BoundOffset;

    private Texture2D _texture;
    public Texture2D Texture
    {
        get {
            return _texture;
        } set {
            _texture = value;
        }
    }

    public float MoveSpeed { get; set; }
    public float JumpSpeed { get; set; }
    public float Gravity { get; set; }
    public bool IsFalling { get; set; }
    public int RiseCount { get; set; }


    public Body body;

    public Player(OctoDash.OctoDash _game, Menu.LevelScreen _levelScreen)
    {

        this.game = _game;
        
        // TODO: idea is that in future, there is Menu.LevelScreen class to inherit from
        this.levelScreen = _levelScreen;

        // start position in Nina's demo level
        this.Pos = new Vector2(128, 2400);
        // this.Pos = new Vector2(0, 0);

        this.BoundOffset = new Vector2(64, 64);

        MoveSpeed = 2.0f;
        JumpSpeed = 10f;
        Gravity = 2f;
        IsFalling = true;
        RiseCount = 0;



        //this.Texture = game.Content.Load<Texture2D>("underwater/octopus-200");

        // body = game.world.CreateCircle(.45f, 1f, Units.MonoGameToAether(this.Center), BodyType.Dynamic);
        // body.SetCollisionCategories(Category.Cat3);
        // body.Tag = "stickable";
        // body = game.world.CreateRectangle(1.5f, 1.5f, .3f, Units.MonoGameToAether(this.Center), 0, BodyType.Dynamic);
        // 
        body = game.character.center;
    }

    public void LoadContent()
    {

    }

    public void Update()
    {
        // update state or sth idk
        Pos = Units.AetherToMonogame(body.Position);
    }

    /* private Vector2 getBoundLR() */
    /* { */
    /*     // lower right */
    /*     Vector2 res = new Vector2(this.Pos.X + Texture.Width, this.Pos.Y + Texture.Height); */
    /*     return res; */
    /* } */

}