using Microsoft.Xna.Framework;
using tainicom.Aether.Physics2D.Common;
using Microsoft.Xna.Framework.Graphics;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Dynamics.Joints;
using tainicom.Aether.Physics2D.Dynamics.Contacts;
using System;
using System.IO;
using System.Collections.Generic;



namespace OctoDash
{

    public class Softbody
    {
        public bool stunned = false;
        public double stun_start = 0f;
        public Body center;
        private Body[] rigidBodiesInner;
        private Body[] rigidBodiesOuter;
        private Body[] pullAffected;

        private List<Body[]> arms = new List<Body[]>();
        private List<DistanceJoint[]> armJoints = new List<DistanceJoint[]>();
        private List<List<DistanceJoint>> connections = new List<List<DistanceJoint>>();

        private List<bool> isSticky = new List<bool>();

        private List<bool> isCarrying = new List<bool>();
        private List<bool> showHint = new List<bool>();

        private List<List<DistanceJoint>> stickyConnections = new List<List<DistanceJoint>>();

        private int lastElement;
        private float arm_length;
        private World world;

        private float sizeScale;
        public int numRigidBodiesBody = 26;
        private float frequencyScale = 1.25f;
        private float dampeningScale = 5;

        private MeshConfig _meshConfig;
        private SoftbodyTexture _texture;


        private List<Texture2D> hintTexture = new List<Texture2D>();
        private List<Texture2D> stickingTexture = new List<Texture2D>();

        private OctoDash game;
        private Menu.LevelScreen _levelScreen;

        private Category[] categories = (Category[])Enum.GetValues(typeof(Category));

        public readonly int NUM_ARMS = 4;

        public Softbody(OctoDash _game, ref World _world, int numRigidBodiesBody, int numSegmentsArm, float radiusOuter, float radiusInnerRatio, Vector2 position, Menu.LevelScreen levelScreen, string textureName, string objFile)
        {
            game = _game;
            world = _world;

            // before we create the first rigid body or joint
            _levelScreen = levelScreen;
            _texture = new SoftbodyTexture(game.GraphicsDevice, game, levelScreen, textureName, objFile);
            _meshConfig = new MeshConfig();
            sizeScale = 0.032f;

            CreateBody(numRigidBodiesBody, radiusOuter * sizeScale, radiusInnerRatio, position);

            int catIndex = 4 + 1;

            for (int armIndex = 0; armIndex < NUM_ARMS; ++armIndex)
            {
                arm_length = CreateArm(numSegmentsArm, 6f, 1 + (int)armIndex * 3, categories[catIndex++], MathF.PI * 0.5f);
            }
#if DEBUG
            _meshConfig.WriteMeshConfigToFile();
#endif
            lastElement = numSegmentsArm * 2;
        }

        private void CreateBody(int numRigidBodiesBody, float radiusOuter, float radiusInnerRatio, Vector2 position)
        {
            center = createCircleBody(9.5f * sizeScale, 1f, position);

            rigidBodiesInner = new Body[numRigidBodiesBody];
            rigidBodiesOuter = new Body[numRigidBodiesBody];
            pullAffected = new Body[1];

            float step = 2 * MathF.PI / numRigidBodiesBody;


            pullAffected[0] = center;

            for (int i = 0; i < numRigidBodiesBody; i++)
            {
                float xo = MathF.Cos(i * step);
                float yo = MathF.Sin(i * step);
                float xi = MathF.Cos(i * step + step / 2);
                float yi = MathF.Sin(i * step + step / 2);
                Vector2 positionOuter = position + new Vector2(xo, yo) * radiusOuter;
                Vector2 positionInner = position + new Vector2(xi, yi) * radiusOuter * radiusInnerRatio;
                rigidBodiesOuter[i] = createCircleBody(1.8f * sizeScale, 0f, positionOuter);
                rigidBodiesInner[i] = createCircleBody(1.2f * sizeScale, 0f, positionInner);

                //createDistanceJoint(center, rigidBodiesInner[i], 10f, 0.6f);
                createDistanceJoint(center, rigidBodiesOuter[i], 5f, 0.1f);
                createDistanceJoint(rigidBodiesInner[i], rigidBodiesOuter[i], 10f, 0.6f);
            }

            connectRing(rigidBodiesInner, rigidBodiesOuter, 15f, 0.6f);
            connectRing(rigidBodiesInner, rigidBodiesInner, 0f, 1f);
            connectRing(rigidBodiesOuter, rigidBodiesOuter, 25f, 0.6f);
        }

        private Body createCircleBody(float radius, float density, Vector2 position)
        {
            Body b = this.world.CreateCircle(radius, density, position, BodyType.Dynamic);
            b.SetCollisionCategories(Category.Cat1);
            b.SetCollidesWith(Category.Cat1 | Category.Cat3);
            b.Tag = this;
            // b.SetCollidesWith(Category.Cat3);
            _meshConfig.addVertex(b);
            _texture.AddVertex(b);
            return b;
        }

        private void connectRing(Body[] rigidBodiesA, Body[] rigidBodiesB, float frequency, float dampeningratio)
        {
            if (rigidBodiesA.Length != rigidBodiesB.Length) throw new ArgumentException("Length of circle A and circle B differs");
            int size = rigidBodiesA.Length;
            for (int i = 0; i < size; i++)
            {
                createDistanceJoint(rigidBodiesA[i], rigidBodiesB[(i + 1) % (size)], frequency, dampeningratio);
            }
        }


        private float CreateArm(int numSegments, float space, int anchor, Category cat1, float angle)
        {
            Body[] arm = new Body[numSegments * 2 + 1];
            DistanceJoint[] joints = new DistanceJoint[5 + (numSegments - 1) * 5 + 2];

            Vector2 upperPos = rigidBodiesOuter[anchor].Position + rotate(new Vector2(space, 2), angle) * sizeScale;
            Vector2 lowerPos = rigidBodiesOuter[anchor].Position + rotate(new Vector2(space, -2), angle) * sizeScale;


            Category cat = cat1;

            int j = 0;
            arm[0] = createInnerCircleArm(0, upperPos, cat);
            arm[numSegments] = createInnerCircleArm(0, lowerPos, cat);
            joints[j] = createDistanceJoint(arm[0], rigidBodiesInner[anchor], 23f, 0.8f, 0.5f * space);
            joints[j].CollideConnected = false;
            joints[++j] = createDistanceJoint(arm[numSegments], rigidBodiesInner[anchor], 23f, 0.8f, 0.5f * space);
            joints[j].CollideConnected = false;
            joints[++j] = createDistanceJoint(arm[0], arm[numSegments], 23f, 0.8f, 0.5f * space);
            j++;

            float bsr = 0.02f; // reduction of circle radius with increasing distance to center and spring length
            for (int i = 1; i < numSegments; i++)
            {
                float rdc = i * bsr;
                upperPos = rigidBodiesOuter[anchor].Position + rotate(new Vector2((i + 1) * space, 2), angle) * sizeScale;
                lowerPos = rigidBodiesOuter[anchor].Position + rotate(new Vector2((i + 1) * space, -2), angle) * sizeScale;

                arm[i] = createInnerCircleArm(rdc, upperPos, cat);
                arm[numSegments + i] = createInnerCircleArm(rdc, lowerPos, cat);
                joints[j++] = createDistanceJoint(arm[i], arm[i - 1], 20f, 0.8f, space - 7 * rdc);  // lenght
                joints[j++] = createDistanceJoint(arm[numSegments + i], arm[numSegments + i - 1], 20f, 0.8f,space - 7 * rdc);
                joints[j++] = createDistanceJoint(arm[numSegments + i], arm[i], 20f, 0.8f, space - 7 * rdc);
                joints[j++] = createDistanceJoint(arm[numSegments + i], arm[i - 1], 20f, 0.8f, space - 7 * rdc);
                joints[j++] = createDistanceJoint(arm[numSegments + i - 1], arm[i], 20f, 0.8f, space - 7 * rdc);
            }

            Vector2 tip = rigidBodiesOuter[anchor].Position + rotate(new Vector2((numSegments) * space, 0), angle) * sizeScale;
            arm[numSegments * 2] = createCircleArm(3, tip, cat);
            arm[numSegments * 2].SetCollidesWith(Category.Cat3 | Category.Cat10);
            joints[j++] = createDistanceJoint(arm[numSegments * 2], arm[numSegments - 1], 23f, 0.8f, space);
            joints[j++] = createDistanceJoint(arm[numSegments * 2], arm[numSegments * 2 - 1], 23f, 0.8f, space);

            float len = Vector2.Distance(rigidBodiesOuter[anchor].Position, arm[numSegments * 2].Position);


            arms.Add(arm);
            armJoints.Add(joints);
            isSticky.Add(false);
            isCarrying.Add(false);
            showHint.Add(false);
            stickyConnections.Add(new List<DistanceJoint>());
            connections.Add(new List<DistanceJoint>());
            return len;
        }

        private Body createCircleArm(float radius, Vector2 position, Category cat)
        {
            Body b = world.CreateCircle(radius * sizeScale, 0f, position, BodyType.Dynamic);
            b.SetCollisionCategories(cat);
            b.SetCollidesWith(Category.Cat3);
            b.Tag = this;
            _meshConfig.addVertex(b);
            _texture.AddVertex(b);
            return b;
        }

        private Body createInnerCircleArm(float rdc, Vector2 position, Category cat)
        {
            return createCircleArm(0.9f - rdc, position, cat);
        }

        // create Distance Joint between Body a and b with given characteristics
        public DistanceJoint createDistanceJoint(Body a, Body b, float frequency, float DampeningRatio)
        {
            DistanceJoint dj = new DistanceJoint(a, b, new Vector2(0f, 0f), new Vector2(0f, 0f));
            dj.Frequency = frequency * frequencyScale;
            dj.DampingRatio = DampeningRatio * dampeningScale;
            dj.CollideConnected = true;
            world.Add(dj);
            _texture.AddEdge(a, b);
            _meshConfig.addEdge(a, b);
            return dj;
        }
        public DistanceJoint createDistanceJoint(Body a, Body b, float frequency, float DampeningRatio, float Length)
        {
            DistanceJoint dj = new DistanceJoint(a, b, new Vector2(0f, 0f), new Vector2(0f, 0f));
            dj.Frequency = frequency * frequencyScale;
            dj.DampingRatio = DampeningRatio * dampeningScale;
            dj.Length = Length * this.sizeScale;
            dj.CollideConnected = true;
            world.Add(dj);
            _texture.AddEdge(a, b);
            _meshConfig.addEdge(a, b);
            return dj;
        }

        public Vector2 getPosition()
        {
            return center.Position;
        }

        public void applyForceArm(int arm, Vector2 force)
        {
            Vector2 direction = arms[arm][lastElement].Position - arms[arm][0].Position;
            if (isSticky[arm] && !isCarrying[arm])
            {
                force = forceDampening(force, direction);
                foreach (Body x in pullAffected)
                {
                    applyForce(x, 20 * force);
                }
            }
            else
            {
                applyForce(arms[arm][lastElement], force);
                force = forceReversal(force, direction);

                if (Vector2.Distance(arms[arm][lastElement].Position, center.Position) >= arm_length / 2)
                {
                    applyForce(center, 2 * force);
                }

            }
        }

        private static Vector2 forceReversal(Vector2 force, Vector2 direction)
        {
            direction.Normalize();
            float length = force.Length();
            force.X += direction.X * length;
            force.Y += direction.Y * length;
            force.X = force.X * (-1f);
            if (direction.Y < 0)
            {
                force.Y = 0;
            }
            else
            {
                force.Y = (force.Y * (-1f));
            }
            return force;
        }
        private static Vector2 forceDampening(Vector2 force, Vector2 direction)
        {
            direction.Normalize();
            float length = force.Length();
            force.X += direction.X * length;
            force.Y += (direction.Y * length);
            return force / 4;
        }

        public void applyForce(Body body, Vector2 force)
        {
            body.ApplyForce(force * body.Mass * 100, Vector2.Zero);
        }
        public void jump(GameTime time)
        {
            Vector2 j_force;
            j_force.X = 0f;
            j_force.Y = 40f;
            applyForce(center, j_force);

        }
        public void drag(float max_speed)
        {
            Vector2 current_speed = center.LinearVelocity;
            float speed_squared = current_speed.X * current_speed.X + current_speed.Y * current_speed.Y;
            if (speed_squared > max_speed)
            {
                float adjustment = speed_squared - max_speed;
                current_speed.Normalize();
                Vector2 direction = current_speed * (-1);
                foreach (Body x in rigidBodiesInner)
                {
                    x.ApplyForce(x.Mass * direction * adjustment);
                }
                foreach (Body x in rigidBodiesOuter)
                {
                    x.ApplyForce(x.Mass * direction * adjustment);
                }
                center.ApplyForce(center.Mass * direction * adjustment);
            }
        }
        public void range(int arm)
        {
            if (Vector2.Distance(arms[arm][lastElement].Position, center.Position) > 6f)
            {
                remove_sticky(arm);
            }
        }
        public void remove_sticky(int arm)
        {
            foreach (DistanceJoint connection in stickyConnections[arm])
            {
                if (connection.BodyA.Tag.ToString().Contains("movable"))
                {
                    connection.BodyA.SetCollidesWith(Category.All);
                }

                this.world.Remove(connection);
            }
            stickyConnections[arm] = new List<DistanceJoint>();
            isSticky[arm] = false;
            isCarrying[arm] = false;
        }
        public void changeStickynessArm(int arm, bool shouldBeSticky, GameTime time)
        {
            if (shouldBeSticky == isSticky[arm]) return;


            if (isSticky[arm])
            {
                remove_sticky(arm);
            }
            else if (stunned == true && time.TotalGameTime.TotalSeconds - stun_start < 1.5)
            {
                return;
            }
            else
            {
                ContactEdge contacts = arms[arm][lastElement].ContactList;
                while (contacts != null)
                {
                    if (contacts.Other != null && contacts.Other.Tag != null && contacts.Other.Tag.ToString().Contains("stickable") && contacts.Contact.IsTouching)
                    {
                        Vector2 normal = new Vector2();
                        FixedArray2<Vector2> points = new FixedArray2<Vector2>();
                        contacts.Contact.GetWorldManifold(out normal, out points);
                        Vector2 otherAnchor = contacts.Other.GetLocalPoint(points[0]);

                        DistanceJoint connection = new DistanceJoint(contacts.Other, arms[arm][lastElement], otherAnchor, Vector2.Zero);
                        connection.CollideConnected = true;
                        stickyConnections[arm].Add(connection);
                        isSticky[arm] = true;

                        this.world.Add(connection);
                        game.PlaySound(0);
                        if (contacts.Other.Tag.ToString().Contains("movable"))
                        {
                            contacts.Other.SetCollidesWith(Category.Cat3 | Category.Cat10);
                            isCarrying[arm] = true;
                        }
                    }
                    contacts = contacts.Next;
                }
            }

        }

        public void applyHint(int arm, bool trigger, bool stickPressed)
        {
            if ((isSticky[arm] || isCarrying[arm]))
            {
                // we only show the sticking texture unless the stick is pushed in
                showHint[arm] = stickPressed;
            }
            else
            {
                showHint[arm] = trigger | stickPressed;
            }
        }

        public static Vector2 rotate(Vector2 old, float angle)
        {
            float x = old.X * MathF.Cos(angle) - old.Y * MathF.Sin(angle);
            float y = old.X * MathF.Sin(angle) + old.Y * MathF.Cos(angle);
            return new Vector2(x, y);
        }


        public void LoadContent()
        {
            //hintTexture = game.Content.Load<Texture2D>("Octopus/glow_1");
            //stickingTexture = game.Content.Load<Texture2D>("Octopus/sticking");
            _texture.LoadContent();
            for (int i = 1; i <= NUM_ARMS; i++)
            {

                hintTexture.Add(game.Content.Load<Texture2D>($"Octopus/glow_{i}"));
                stickingTexture.Add(game.Content.Load<Texture2D>($"Octopus/sticky_{i}"));
            }
        }

        public void Update(GameTime gameTime)
        {
            _texture.Update(gameTime);

        }

        public void Draw(SpriteBatch _spriteBatch)
        {
            _texture.Draw();
            var transformMatrix = _levelScreen.cv.Camera.GetViewMatrix();
            _spriteBatch.Begin(transformMatrix: transformMatrix);
            for (int arm = 0; arm < NUM_ARMS; arm++)
            {
                // show sticked texture
                if (isSticky[arm] || isCarrying[arm])
                {
                    foreach (DistanceJoint d in stickyConnections[arm])
                    {
                        var position = Units.AetherToMonogame(d.BodyB.Position);
                        var rec = new Rectangle((int)position.X - 15, (int)position.Y - 15, 30, 30);
                        _spriteBatch.Draw(stickingTexture[arm], rec, Color.White);



                    }


                }
                // show hint texture
                if (showHint[arm])
                {
                    var position = Units.AetherToMonogame(arms[arm][lastElement].Position);
                    var rec = new Rectangle((int)position.X - 60, (int)position.Y - 60, 120, 120);
                    _spriteBatch.Draw(hintTexture[arm], rec, Color.White);
                }
            }
            _spriteBatch.End();

        }
    }

    public class MeshConfig
    {
        private int vertIndex = 0; // TODO: remove if not needed.
        private Vector2 center;
        private Dictionary<Body, int> vertexIndices = new Dictionary<Body, int>();
        public List<Vector2> vertices = new List<Vector2>();
        public List<Body> vertexBodies = new List<Body>();
        public List<int[]> edges = new List<int[]>();
        public MeshConfig()
        {
            vertIndex = 0;
            vertexIndices = new Dictionary<Body, int>();
            vertices = new List<Vector2>();
            vertexBodies = new List<Body>();
            edges = new List<int[]>();
        }

        public void addVertex(Body b)
        {
            vertexBodies.Add(b);
            vertices.Add(b.Position);
            if (vertIndex == 0)
            {
                center = b.Position;
            }
            vertexIndices[b] = vertIndex++;
        }

        public void addEdge(Body a, Body b)
        {
            if (edges == null)
            {
                edges = new List<int[]>();
            }
            int[] edge = { vertexIndices[a], vertexIndices[b] };
            edges.Add(edge);
        }

        // TODO: remove as soon as shader pipeline is tested to work without this
        protected void updateVertex(Body b)
        {
            vertices[vertexIndices[b]] = b.Position;
        }

        public void WriteMeshConfigToFile()
        {
            List<int>[] neighbors = new List<int>[vertexIndices.Keys.Count];
            for (int i = 0; i < neighbors.Length; ++i)
            {
                neighbors[i] = new List<int>();
            }

            foreach (int[] edge in edges)
            {
                neighbors[edge[0]].Add(edge[1]);
                neighbors[edge[1]].Add(edge[0]);
            }

            // get all possible triangles (each triangle will be captured at least 3 times and often more)
            int[] done = new int[vertexIndices.Keys.Count];
            List<List<int>> tmpTriangles = new List<List<int>>();
            foreach (int vi in vertexIndices.Values)
            {
                foreach (int vj in neighbors[vi])
                {
                    foreach (int vk in neighbors[vj])
                    {
                        if (vk == vi)
                        {
                            continue;
                        }
                        if (neighbors[vk].Contains(vi))
                        {
                            List<int> triangle = new List<int>();
                            triangle.Add(vi);
                            triangle.Add(vj);
                            triangle.Add(vk);
                            triangle.Sort();
                            tmpTriangles.Add(triangle);
                        }
                    }
                }

            }
            tmpTriangles.Sort(new ListComparer());

            // remove duplicates
            Stack<int[]> triangles = new Stack<int[]>();
            List<int> f = tmpTriangles[0];
            int[] f0 = { f[0] + 1, f[1] + 1, f[2] + 1 };
            triangles.Push(f0);
            foreach (List<int> ti in tmpTriangles)
            {
                if (triangles.Peek()[0] - 1 != ti[0]
                || triangles.Peek()[1] - 1 != ti[1]
                || triangles.Peek()[2] - 1 != ti[2])
                {
                    // Blender needs indexed by 1 insread of 0
                    int[] t = { ti[0] + 1, ti[1] + 1, ti[2] + 1 };
                    triangles.Push(t);
                }
            }

            using (var file = new System.IO.StreamWriter("../../../OctopusMesh.obj", append: false))
            {
                foreach (Body b in vertexIndices.Keys)
                {
                    Vector2 xy = b.Position - center;
                    file.WriteLine("v " + xy.X.ToString() + " " + xy.Y.ToString() + " 0");
                }

                Stack<int[]> reversed = new Stack<int[]>();
                while (triangles.Count > 0)
                {
                    reversed.Push(triangles.Pop());
                }
                while (reversed.Count > 0)
                {
                    int[] t = reversed.Pop();
                    file.WriteLine("f " + t[0].ToString() + " " + t[1].ToString() + " " + t[2].ToString());
                }

            }
        }
    }

    public class SoftbodyTexture
    {

        private Dictionary<Body, int> vertexIndices = new Dictionary<Body, int>();
        private List<Body> _bodies = new List<Body>();
        private List<int[]> edges = new List<int[]>();
        private BasicEffect _basicEffect;
        private VertexPositionTexture[] _vertices;
        private int[] _ind;
        private int _vertexIndex;
        private float _maxX, _minX, _maxY, _minY;
        private OctoDash _game;
        private Menu.LevelScreen _levelScreen;
        private GraphicsDevice _graphics;
        private String _textureName;
        private String _objPath;
        private int[] _meshTobody;
        private bool _loaded = false;
        //private SpriteFont _debugfont;

        public SoftbodyTexture(GraphicsDevice graphicsDevice, OctoDash game, Menu.LevelScreen levelScreen, string textureName, string objPath)
        {
            _basicEffect = new BasicEffect(graphicsDevice);
            _game = game;
            _levelScreen = levelScreen;
            _graphics = graphicsDevice;
            _vertexIndex = 0;
            // use min and max initial coordinates for the uv hack thing
            _minX = float.PositiveInfinity;
            _minY = float.PositiveInfinity;
            _maxX = float.NegativeInfinity;
            _maxY = float.NegativeInfinity;
            _textureName = textureName;
            _objPath = objPath;
        }


        public void LoadContent()
        {
            if (_loaded)
            {
                return;
            }
            Texture2D bodyTexture = _game.Content.Load<Texture2D>(_textureName);
            _basicEffect.Texture = bodyTexture;
            _basicEffect.TextureEnabled = true;
            //_debugfont = _game.Content.Load<SpriteFont>("DebugText");
            this.InitializeTextureCoordinates();
            _loaded = true;

        }

        public void AddVertex(Body b)
        {
            _bodies.Add(b);
            _minX = Math.Min(_minX, b.Position.X);
            _maxX = Math.Max(_maxX, b.Position.X);
            _minY = Math.Min(_minY, b.Position.Y);
            _maxY = Math.Max(_maxY, b.Position.Y);
            //vertices.Add(b.Position);
            vertexIndices[b] = _vertexIndex++;
        }


        public void AddEdge(Body a, Body b)
        {
            int[] edge = { vertexIndices[a], vertexIndices[b] };
            edges.Add(edge);
        }

        // Deprecated, only needed for legacy reasons
        public void CreateTriangleIndexes()
        {
            List<int>[] neighbors = new List<int>[vertexIndices.Keys.Count];
            for (int i = 0; i < neighbors.Length; ++i)
            {
                neighbors[i] = new List<int>();
            }

            foreach (int[] edge in edges)
            {
                neighbors[edge[0]].Add(edge[1]);
                neighbors[edge[1]].Add(edge[0]);
            }

            // get all possible triangles (each triangle will be captured at least 3 times and often more)
            int[] done = new int[vertexIndices.Keys.Count];
            List<List<int>> tmpTriangles = new List<List<int>>();
            foreach (int vi in vertexIndices.Values)
            {
                foreach (int vj in neighbors[vi])
                {
                    foreach (int vk in neighbors[vj])
                    {
                        if (vk == vi)
                        {
                            continue;
                        }
                        if (neighbors[vk].Contains(vi))
                        {
                            List<int> triangle = new List<int>();
                            triangle.Add(vi);
                            triangle.Add(vj);
                            triangle.Add(vk);
                            triangle.Sort();
                            tmpTriangles.Add(triangle);
                        }
                    }
                }

            }
            tmpTriangles.Sort(new ListComparer());

            // remove duplicates
            Stack<int[]> triangles = new Stack<int[]>();
            List<int> f = tmpTriangles[0];
            int[] f0 = { f[0], f[1], f[2] };
            triangles.Push(f0);
            foreach (List<int> ti in tmpTriangles)
            {
                if (triangles.Peek()[0] != ti[0]
                || triangles.Peek()[1] != ti[1]
                || triangles.Peek()[2] != ti[2])
                {
                    // here triangles need to be indexed by 0
                    int[] t = { ti[0], ti[1], ti[2] };
                    triangles.Push(t);
                }
            }

            _ind = new int[triangles.Count * 3];
            int j = 0;
            while (triangles.Count > 0)
            {
                _ind[j++] = triangles.Peek()[0];
                _ind[j++] = triangles.Peek()[1];
                _ind[j++] = triangles.Pop()[2];
            }

        }


        public void InitializeTextureCoordinates()
        {
            // import object file created by blender, which contains the UV coordinates, as well as the triangles
            ObjReader reader = new ObjReader(_objPath);
            // no valid object file found file found
            // fall back to old method
            if (null == reader.meshVertices || null == reader.meshUVs || reader.meshVertices.Count != _bodies.Count)
            {
                Console.WriteLine("Vertices: " + reader.meshVertices?.ToString());
                Console.WriteLine("UVs: " + reader.meshUVs?.ToString());
                Console.WriteLine("Vertices Count: " + reader.meshVertices?.Count.ToString());
                Console.WriteLine("Bodies Count: " + _bodies.Count.ToString());
                Console.WriteLine("UVs Count: " + reader.meshUVs?.Count.ToString());

                _vertices = new VertexPositionTexture[_bodies.Count];
                _meshTobody = new int[_bodies.Count];
                System.Diagnostics.Debug.WriteLine($"### WARNING ####\n{this._objPath} is not a valid uv unwrapping for the octopus!\nfalling back to default unwrapping.");
                float scale = 1f / Math.Max(_maxX - _minX, _maxY - _minY);
                for (int i = 0; i < _vertices.Length; i++)
                {
                    _meshTobody[i] = i;
                    // _vertices[i].TextureCoordinate.X = (_bodies[i].Position.X - _minX) * scale;
                    // _vertices[i].TextureCoordinate.Y = (_bodies[i].Position.Y - _minY) * scale;
                    _vertices[i].TextureCoordinate.X = .2f;
                    _vertices[i].TextureCoordinate.Y = .7f;
                }
                CreateTriangleIndexes();
                return;

            }
            // build the vertices with textures, and create the mapping from mesh->physics by measuring original distance
            _vertices = new VertexPositionTexture[reader.vertUV.Count];
            _meshTobody = new int[reader.vertUV.Count];
            Vector2 center = _bodies[0].Position;
            for (int i = 0; i < reader.vertUV.Count; i++)
            {
                Vector2 objVert = reader.vertUV[i].Item1 + center;
                for (int j = 0; j < _bodies.Count; j++)
                {
                    if ((_bodies[j].Position - objVert).LengthSquared() < 10e-4f)
                    {

                        _vertices[i].TextureCoordinate = reader.vertUV[i].Item2;
                        _meshTobody[i] = j;
                        break;
                    }
                }
            }
            _ind = reader.GetTrianlgeList();
            // reverse draw oder, so body is after tentacles
            Array.Reverse(_ind, 0, _ind.Length);

        }

        public void Update(GameTime gameTime)
        {
            this.UpdatePosition();
        }
        // Update _vert to the current posistion of the softbody
        // TODO: maybe move camera/projection stuff to the shader??
        public void UpdatePosition()
        {
            for (int i = 0; i < _vertices.Length; i++)
            {
                Vector2 screenPos = _levelScreen.cv.ConvertWorldToScreen(_bodies[_meshTobody[i]].Position);
                _vertices[i].Position = new Vector3(screenPos, 0f);
            }
        }

        public void Draw()
        {
            // https://stackoverflow.com/questions/33136388/monogame-xna-draw-polygon-in-spritebatch
            // https://community.monogame.net/t/minimal-example-of-drawing-a-quad-into-2d-space/11063/2

            //_game._spriteBatch.Begin();
            //_game._spriteBatch.DrawString(_debugfont, _vertices[0].Position.ToString(), new Vector2(100, 100), Color.Black);
            //_game._spriteBatch.End();

            Viewport viewport = _graphics.Viewport;
            _basicEffect.World = Matrix.Identity;
            Vector3 cameraUp = Vector3.Transform(new Vector3(0, -1, 0), Matrix.CreateRotationZ(_levelScreen.cv.Camera.Rotation));
            _basicEffect.View = Matrix.CreateLookAt(new Vector3(0, 0, -1), new Vector3(0, 0, 0), cameraUp);
            // Vector3 cameraUp = Vector3.Transform(new Vector3(0, -1, 0), Matrix.CreateRotationZ(_game.cv.Camera.Rotation));
            // _basicEffect.View = _game.cv.Camera.GetViewMatrix();
            //_basicEffect.View = _game.cv.View;
            // _basicEffect.View = Matrix.CreateLookAt(new Vector3(0, 0, -1), new Vector3(0,0, 0), cameraUp);
            //_basicEffect.Projection = _game.cv.Projection;
            _basicEffect.Projection = Matrix.CreateScale(1, -1, 1) * Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);
            // not sure if necessary
            // necessery for now, triangles have different facing  sides, so we don't cull
            _graphics.RasterizerState = RasterizerState.CullNone;
            foreach (EffectPass effectPass in _basicEffect.CurrentTechnique.Passes)
            {
                effectPass.Apply();
                _graphics.DrawUserIndexedPrimitives<VertexPositionTexture>(
                    PrimitiveType.TriangleList, _vertices, 0, _vertices.Length, _ind, 0, _ind.Length / 3);
            }
        }
    }

    public class ObjReader
    {
        private String _path;
        public List<Vector2> meshVertices;
        public List<Vector2> meshUVs;
        public Dictionary<int, List<int>> uvMapping;
        public List<Tuple<Vector2, Vector2>> vertUV;
        private List<int> triangleList;
        public int totalVerts;
        public int extraVerts;
        public ObjReader(String path)
        {
            _path = path;
            this.ReadFile();
        }

        public void ReadFile()
        {
            if (!File.Exists(_path))
            {
                Console.WriteLine(_path + " not found.");
                return;
            }
            meshVertices = new List<Vector2>();
            meshUVs = new List<Vector2>();
            vertUV = new List<Tuple<Vector2, Vector2>>();
            uvMapping = new Dictionary<int, List<int>>();
            triangleList = new List<int>();
            totalVerts = 0;
            extraVerts = 0;
            using (StreamReader file = new StreamReader(_path))
            {
                String ln;
                while ((ln = file.ReadLine()) != null)
                {
                    String[] fields = ln.Split(' ');
                    if (fields[0].Equals("v"))
                    {
                        float a = float.Parse(fields[1], System.Globalization.CultureInfo.InvariantCulture);
                        float b = float.Parse(fields[2], System.Globalization.CultureInfo.InvariantCulture);
                        meshVertices.Add(new Vector2(a, b));
                    }
                    else if (fields[0].Equals("vt"))
                    {
                        float a = float.Parse(fields[1], System.Globalization.CultureInfo.InvariantCulture);
                        float b = 1f - float.Parse(fields[2], System.Globalization.CultureInfo.InvariantCulture);
                        meshUVs.Add(new Vector2(a, b));
                    }
                    else if (fields[0].Equals("f"))
                    {
                        var v_temp = new int[3];
                        var vt_temp = new int[3];
                        for (int i = 1; i <= 3; i++)
                        {
                            String[] inds = fields[i].Split('/');
                            // "f meshvertex/uvvertex/normal meshvertex/uvvertex/normal meshvertex/uvvertex/normal"
                            // obj files have indices starting at 1, we start at 0
                            int v = int.Parse(inds[0]) - 1;
                            int vt = int.Parse(inds[1]) - 1;
                            v_temp[i - 1] = v;
                            vt_temp[i - 1] = vt;

                        }
                        // build the vertUV list with unique vertex/uv combos
                        // also build the triangle list with the adjusted index
                        for (int i = 0; i < 3; i++)
                        {
                            var to_add = new Tuple<Vector2, Vector2>(meshVertices[v_temp[i]], meshUVs[vt_temp[i]]);
                            if (!vertUV.Contains(to_add))
                            {
                                vertUV.Add(to_add);
                            }
                            triangleList.Add(vertUV.IndexOf(to_add));
                        }
                    }
                }
            }
        }

        public int[] GetTrianlgeList()
        {
            return triangleList.ToArray();
        }

    }
    // thanks internet https://stackoverflow.com/questions/12233822/lexicographical-sort-array-of-arrays-algorithm-using-c-sharp
    public class ListComparer : IComparer<IEnumerable<int>>
    {
        public int Compare(IEnumerable<int> x, IEnumerable<int> y)
        {
            var xenum = x.GetEnumerator();
            var yenum = y.GetEnumerator();
            while (xenum.MoveNext() && yenum.MoveNext())
            {
                if (xenum.Current != yenum.Current)
                    return xenum.Current - yenum.Current;
            }
            return 0;
        }
    }
}
