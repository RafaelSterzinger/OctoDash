using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Dynamics.Joints;


// Utils for controlling the Game with Controllers or Mouse
/*
 *   NOTE: obviously it's super ugly that this file does all this at the same time:
 *   - changes stat of the simulation (character forces/ stickyness, Mouse fixedPoint stuff)
 *   - get and convert input of Mouse
 *   - get and convert input of GamePad(s)
 *
 *   TODO: split this up:
 *   - one class for getting input from a gamepad
 *   - one class for getting input from a gamepad
 *   - one class for getting input from mouse and keyboard
 *   - one class for applying those inputs to our hero
 *   - GOAL: decoupling!
*/
public class Controls
{

    private FixedMouseJoint fixedMouseJoint { get; set; }
    private World world { get; set; }

    private OctoDash.OctoDash game;

    private Menu.LevelScreen levelScreen;
    public double last_jump = -5f;
    public double start = -5f;
    public double speed = 0f;
    public static MouseState _mouseState;
    public static MouseState _oldMouseState;
    public static KeyboardState _keyboardState;
    public static KeyboardState _oldKeyboardState;
    public static GamePadState _gamePadState1;
    public static GamePadState _gamePadState2;

    public static GamePadState _oldGamePadState1;
    public static GamePadState _oldGamePadState2;

    private static Keys[] _lastPressedKeys = new Keys[5];

    public Controls(OctoDash.OctoDash _game, World _world, Menu.LevelScreen _levelScreen)
    {
        this.game = _game;
        this.levelScreen = _levelScreen;
        this.world = _world;
    }


    public static bool OnMousePress(ButtonState mouseButton)
    {
        return mouseButton == ButtonState.Pressed;
    }

    public static bool OnMouseRelease(ButtonState mouseButton, ButtonState oldMouseButton)
    {
        return mouseButton == ButtonState.Released && oldMouseButton == ButtonState.Pressed;
    }

    public static bool OnKeyboardPress(Keys key)
    {
        return _keyboardState.IsKeyDown(key);
    }
    public static bool OnKeyboardRelease(Keys key)
    {
        bool result = _keyboardState.IsKeyUp(key) && _oldKeyboardState.IsKeyDown(key);
        if (result)
        {
            Log.Logger.Log("Key release " + key.ToString());
        }
        return result;
    }


    public static bool OnGamePadPress(int gamePad, Buttons button)
    {
        switch (gamePad)
        {
            case 0: // ONE
                return _gamePadState1.IsButtonDown(button);
            case 1: // TWO
                return _gamePadState2.IsButtonDown(button);
            default:
                throw new System.ArgumentException("Unexpected gamePad index!");
        }
    }

    public static bool OnGamePadRelease(int gamePad, Buttons button)
    {
        bool result;
        // return _keyboardState.IsKeyDown(key) && !_oldKeyboardState.IsKeyDown(key);
        switch (gamePad)
        {
            case -1: // ANY
                result = (!_gamePadState1.IsButtonDown(button) && _oldGamePadState1.IsButtonDown(button))
                      || (!_gamePadState2.IsButtonDown(button) && _oldGamePadState2.IsButtonDown(button));
                break;
            case 0: // ONE
                result = !_gamePadState1.IsButtonDown(button) && _oldGamePadState1.IsButtonDown(button);
                break;
            case 1: // TWO
                result = !_gamePadState2.IsButtonDown(button) && _oldGamePadState2.IsButtonDown(button);
                break;
            default:
                throw new System.ArgumentException("Unexpected gamePad index!");
        }
        if (result)
        {
            Log.Logger.Log("GamePad button release " + button.ToString());
        }
        return result;
    }

    public static void allowWriting(GUI.MutableTextElement element)
    {
        if (element == null) return;

        if (GUI.GUIElement.Selected?.Name == Constants.PlayerString)
        {
            Keys[] pressedKeys = _keyboardState.GetPressedKeys();

            foreach (Keys key in _lastPressedKeys)
            {
                bool stillPressed = false;
                foreach (Keys pressedKey in pressedKeys)
                {
                    stillPressed |= key == pressedKey;
                }
                if (!stillPressed)
                {
                    OnKeyUp(element, key);
                }
            }

            foreach (Keys key in pressedKeys)
            {
                bool previouslyPressed = false;
                foreach (Keys lastPressedKey in _lastPressedKeys)
                {
                    previouslyPressed |= key == lastPressedKey;
                }
                if (!previouslyPressed)
                {
                    OnKeyDown(element, key);
                }
            }

            _lastPressedKeys = pressedKeys;
        }
    }

    public static void OnKeyUp(GUI.MutableTextElement element, Keys key) { }
    public static void OnKeyDown(GUI.MutableTextElement element, Keys key)
    {
        if (!element.Cleared)
        {
            element.Text = "";
            element.Cleared = true;
        }
        if (key == Keys.Back || key == Keys.Delete)
        {
            if (element.Text.Length > 0)
            {
                element.Text = element.Text.Remove(element.Text.Length - 1);
            }
        }
        else if (element.Text.Length > 11)
        {
            return;
        }
        else if (key == Keys.Space) { element.Text += " "; }
        else if (key == Keys.D0) { element.Text += "0"; }
        else if (key == Keys.D1) { element.Text += "1"; }
        else if (key == Keys.D2) { element.Text += "2"; }
        else if (key == Keys.D3) { element.Text += "3"; }
        else if (key == Keys.D4) { element.Text += "4"; }
        else if (key == Keys.D5) { element.Text += "5"; }
        else if (key == Keys.D6) { element.Text += "6"; }
        else if (key == Keys.D7) { element.Text += "7"; }
        else if (key == Keys.D8) { element.Text += "8"; }
        else if (key == Keys.D9) { element.Text += "9"; }
        else if (key == Keys.A
            || key == Keys.B
            || key == Keys.C
            || key == Keys.D
            || key == Keys.E
            || key == Keys.F
            || key == Keys.G
            || key == Keys.H
            || key == Keys.I
            || key == Keys.J
            || key == Keys.K
            || key == Keys.L
            || key == Keys.M
            || key == Keys.N
            || key == Keys.O
            || key == Keys.P
            || key == Keys.Q
            || key == Keys.R
            || key == Keys.S
            || key == Keys.T
            || key == Keys.U
            || key == Keys.V
            || key == Keys.W
            || key == Keys.X
            || key == Keys.Y
            || key == Keys.Z)
        {
            element.Text += key.ToString();
        }
    }

    public static void allowMenuTraversal()
    {
        if (GUI.GUIElement.Selected == null) { return; }

        // click on selected element
        if (Controls.OnGamePadRelease(-1, Buttons.A)
            || Controls.OnKeyboardRelease(Keys.Enter)
            || Controls.OnKeyboardRelease(Keys.Space)
            || (OctoDash.OctoDash.supportsMouse
                && Controls.OnMouseRelease(Controls._mouseState.LeftButton, Controls._oldMouseState.LeftButton)
                && GUI.GUIElement.Selected.Rectangle.Contains(Controls._mouseState.Position))
        )
        {
            GUI.GUIElement.Selected?.TriggerClickEvent();
        }

        if (Controls.OnKeyboardRelease(Keys.Up)
        //  || Controls.OnKeyboardRelease(Keys.K)
         || Controls.OnGamePadRelease(-1, Buttons.DPadUp))
        {
            GUI.GUIElement.Selected?.Up?.Select();
        }

        if (Controls.OnKeyboardRelease(Keys.Down)
        //  || Controls.OnKeyboardRelease(Keys.J)
         || Controls.OnKeyboardRelease(Keys.Enter)
         || Controls.OnGamePadRelease(-1, Buttons.DPadDown))
        {
            GUI.GUIElement.Selected?.Down?.Select();
        }

        if (Controls.OnKeyboardRelease(Keys.Left)
        //  || Controls.OnKeyboardRelease(Keys.H)
         || Controls.OnGamePadRelease(-1, Buttons.DPadLeft))
        {
            GUI.GUIElement.Selected?.Left?.Select();
        }

        if (Controls.OnKeyboardRelease(Keys.Right)
        //  || Controls.OnKeyboardRelease(Keys.L)
        || Controls.OnGamePadRelease(-1, Buttons.DPadRight))
        {
            GUI.GUIElement.Selected?.Right?.Select();
        }
    }

    public void Initialize()
    {
        _oldMouseState = Mouse.GetState();
        _mouseState = _oldMouseState;

        _oldKeyboardState = Keyboard.GetState();
        _keyboardState = _oldKeyboardState;

        _oldGamePadState1 = GamePad.GetState(PlayerIndex.One, GamePadDeadZone.Circular);
        _oldGamePadState2 = GamePad.GetState(PlayerIndex.Two, GamePadDeadZone.Circular);
        if (!_oldGamePadState2.IsConnected)
        {
            _oldGamePadState2 = _oldGamePadState1;
        }
        _gamePadState1 = _oldGamePadState1;
        _gamePadState2 = _oldGamePadState2;
    }

    /* updates mouse, keybord and gamepad states */
    public static void GetState()
    {
        // https://community.monogame.net/t/one-shot-key-press/11669
        _oldMouseState = _mouseState;
        _mouseState = Mouse.GetState();

        _oldKeyboardState = _keyboardState;
        _keyboardState = Keyboard.GetState();

        _oldGamePadState1 = _gamePadState1;
        _oldGamePadState2 = _gamePadState2;

        _gamePadState1 = GamePad.GetState(PlayerIndex.One, GamePadDeadZone.Circular);
        _gamePadState2 = GamePad.GetState(PlayerIndex.Two, GamePadDeadZone.Circular);
        if (!_gamePadState2.IsConnected)
        {
            _gamePadState2 = _gamePadState1;
        }
        else if (!_gamePadState1.IsConnected)
        {
            _gamePadState1 = _gamePadState2;
        }
        else
        {
            // This tries to avoid the following bug:
            // when gamepad 1 presses buttons and gamepad 2 gets connected, the old state is wrong for an update-frame.
            if (_oldGamePadState1.Equals(_oldGamePadState2))
            {
                _oldGamePadState2 = _gamePadState2;
                _oldGamePadState1 = _gamePadState1;
            }
        }
    }
    public void Update(GameTime time)
    {
        var pad1 = GamePad.GetState(PlayerIndex.One, GamePadDeadZone.Circular);
        var pad2 = GamePad.GetState(PlayerIndex.Two, GamePadDeadZone.Circular);

        if (!pad2.IsConnected && pad1.IsConnected)
        {
            apply_intput_arm_left(0, pad1, time);
            apply_input_arm_right(1, pad1, time);

            apply_intput_arm_left(2, pad1, time);
            apply_input_arm_right(3, pad1, time);
            apply_input_general(pad1);
            apply_input_general(pad1);
            apply_input_jump(pad1, pad1, time);
        }
        else if (!pad1.IsConnected && pad2.IsConnected)
        {
            apply_intput_arm_left(0, pad2, time);
            apply_input_arm_right(1, pad2, time);

            apply_intput_arm_left(2, pad2, time);
            apply_input_arm_right(3, pad2, time);
            apply_input_general(pad2);
            apply_input_general(pad2);
            apply_input_jump(pad2, pad2, time);
        }
        else
        {
            apply_intput_arm_left(0, pad1, time);
            apply_input_arm_right(3, pad1, time);

            apply_intput_arm_left(1, pad2, time);
            apply_input_arm_right(2, pad2, time);
            apply_input_general(pad1);
            apply_input_general(pad2);
            apply_input_jump(pad1, pad2, time);
        }
        max_speed();
        max_range();
        //hook_arm_left(0, _gamePadState1);
        //hook_arm_right(1, _gamePadState1);

        //hook_arm_left(2, _gamePadState2);
        //hook_arm_right(3, _gamePadState2);
        //#if DEBUG
        toggleDebugView();
        //#endif
    }

    private void apply_input_arm(int arm, GamePadState gamePadState, Vector2 forceInput, float triggerInput, bool stickPressed, GameTime time)
    {
        if (arm > game.character.NUM_ARMS - 1)
        {
            return;
        }
        game.character.applyForceArm(arm, 1.7f * forceInput);

        if ((_gamePadState1.Equals(_gamePadState2) && (arm == 0 || arm == 3)) || !_gamePadState1.Equals(_gamePadState2))
        {
            bool wannaStick = triggerInput > 0.5;
            game.character.changeStickynessArm(arm, wannaStick, time);
            game.character.applyHint(arm, wannaStick, stickPressed);
        }
    }

    private void apply_intput_arm_left(int arm, GamePadState gamePadState, GameTime time)
    {
        apply_input_arm(arm, gamePadState, gamePadState.ThumbSticks.Left, gamePadState.Triggers.Left, gamePadState.IsButtonDown(Buttons.LeftStick), time);
    }

    private void apply_input_arm_right(int arm, GamePadState gamePadState, GameTime time)
    {
        apply_input_arm(arm, gamePadState, gamePadState.ThumbSticks.Right, gamePadState.Triggers.Right, gamePadState.IsButtonDown(Buttons.RightStick), time);
    }

    private void apply_input_jump(GamePadState gamePadState1, GamePadState gamePadState2, GameTime time)
    {
        double t = time.TotalGameTime.TotalSeconds;
        if ((gamePadState1.IsButtonDown(Buttons.A) || gamePadState2.IsButtonDown(Buttons.A)))
        {
            if ((t - last_jump) > 5f)
            {
                start = t;
                last_jump = time.TotalGameTime.TotalSeconds;
                game.character.jump(time);
            }
            else if (t - start < 1f)
            {
                game.character.jump(time);
            }
        }
    }

    private void max_speed()
    {
        game.character.drag(30f);
    }
    private void max_range()
    {
        int num_a = game.character.NUM_ARMS;
        game.character.range(1 % num_a);
        game.character.range(2 % num_a);
        game.character.range(3 % num_a);
        game.character.range(4 % num_a);
    }
    private void hook_arm_left(int arm, GamePadState gamePadState)
    {
        hook(arm, gamePadState, gamePadState.ThumbSticks.Left, false);
    }

    private void hook_arm_right(int arm, GamePadState gamePadState)
    {
        hook(arm, gamePadState, gamePadState.ThumbSticks.Right, true);
    }

    private void hook(int arm, GamePadState gamePadState, Vector2 forceInput, bool side)
    {
        if (arm > game.character.NUM_ARMS - 1)
        {
            return;
        }
        Vector2 force = 10 * forceInput;
        if (side && gamePadState.IsButtonDown(Buttons.RightShoulder))
        {
            game.character.applyForceArm(arm, force);
        }
        else if (gamePadState.IsButtonDown(Buttons.LeftShoulder))
        {
            game.character.applyForceArm(arm, force);
        }

    }

    private void apply_input_general(GamePadState gamePadState)
    {

        // zoom
        float amount = .1f;
        if (gamePadState.IsButtonDown(Buttons.DPadUp))
        {
            levelScreen.cv.adjust_FOV(-amount);
        }
        if (gamePadState.IsButtonDown(Buttons.DPadDown))
        {
            levelScreen.cv.adjust_FOV(amount);
        }


    }

    private void toggleDebugView()
    {
        //if (OnGamePadRelease(-1, Buttons.RightShoulder))
        if (OnKeyboardRelease(Keys.D))
        {
            levelScreen.cv.DrawDebug = !levelScreen.cv.DrawDebug;
        }
    }

    public virtual void mouse()
    {
        Vector2 position = levelScreen.cv.ConvertScreenToWorld(_mouseState.X, _mouseState.Y);

        if (_mouseState.LeftButton == ButtonState.Released && _oldMouseState.LeftButton == ButtonState.Pressed)
            MouseUp();
        else if (_mouseState.LeftButton == ButtonState.Pressed && _oldMouseState.LeftButton == ButtonState.Released)
            MouseDown(position);

        MouseMove(position);
    }

    private void MouseDown(Vector2 p)
    {
        if (fixedMouseJoint != null)
            return;

        Fixture fixture = world.TestPoint(p);

        if (fixture != null)
        {
            Body body = fixture.Body;
            fixedMouseJoint = new FixedMouseJoint(body, p);
            // fixedMouseJoint.MaxForce = 100.0f * body.Mass;
            // force by distance from joint:
            fixedMouseJoint.MaxForce = (fixedMouseJoint.BodyA.Position - fixedMouseJoint.LocalAnchorA).Length();
            world.Add(fixedMouseJoint);
            body.Awake = true;
        }
    }

    private void MouseUp()
    {
        if (fixedMouseJoint != null)
        {
            world.Remove(fixedMouseJoint);
            fixedMouseJoint = null;
        }
    }

    private void MouseMove(Vector2 p)
    {
        if (fixedMouseJoint != null)
            fixedMouseJoint.WorldAnchorB = p;
    }

}
