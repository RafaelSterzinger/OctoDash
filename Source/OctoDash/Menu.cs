using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using tainicom.Aether.Physics2D.Dynamics;
using System.Diagnostics;
using System.Collections.Generic;
using MonoGame.Extended.Screens;
using OctoDash;

namespace Menu
{

    public abstract class OctoGameScreen : GameScreen
    {

        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        public OctoGameScreen(OctoDash.OctoDash game) : base(game)
        {
        }


        protected void NotImplemented(string error)
        {
            throw new Exception(error);
        }
        protected void NotImplemented()
        {
            NotImplemented("Not Implemented.");
        }
    }

    public abstract class LevelScreen : OctoGameScreen
    {

        protected new OctoDash.OctoDash Game => (OctoDash.OctoDash)base.Game;

        protected bool showStopWatch = false;
        protected bool _isProperLevel = false;
        public bool IsProperLevel
        {
            get { return _isProperLevel; }
        }

        public Player player;
        // tiled
        public CameraView cv;
        // aether
        public int _levelID = -1;
        public Controls controls;

        public Vector2 _startPositionAether = Units.MonoGameToAether(new Vector2(400, 2900)); // we default to the demolevel

        protected int _defaultSpacing = 50;

        protected GUI.ListElement gameTitle;
        /*
         * Constants.GameState.MainMenu
         * or
         * Constants.GameState.InGamePaused
         */
        protected GUI.ListElement main;
        // Constants.GameState.MainMenuSettings
        protected GUI.ListElement settings;

        // Constants.GameState.VideoSettings
        protected GUI.ListElement videoSettings;

        // Constants.GameState.AudioSettings
        protected GUI.ListElement audioSettings;

        // Constants.GameState.LevelSelect
        protected GUI.ListElement levels;
        private string _levelMap;
        public string LevelMap
        {
            get { return _levelMap; }
        }

        protected List<GUI.ListElement> interactableGUI = new List<GUI.ListElement>();
        protected List<GUI.ListElement> nonInteractableGUI = new List<GUI.ListElement>();

        protected Texture2D _pauseBackDrop;

        protected Texture2D _controlsHint;

        public LevelScreen(OctoDash.OctoDash game) : base(game)
        {
            interactableGUI.Clear();
            nonInteractableGUI.Clear();
            Game.IsMouseVisible = false;
        }
        public LevelScreen(OctoDash.OctoDash game, string levelMap) : base(game)
        {
            interactableGUI.Clear();
            nonInteractableGUI.Clear();
            Game.IsMouseVisible = false;
            _levelMap = levelMap;
        }

        public override void Initialize()
        {
            base.Initialize();

            if (_isProperLevel) // i.e. is a proper level screen
            {
                Name = Constants.LVL_SCREEN_BASE_NAME;
                if (_levelID > 0)
                {
                    Name += _levelID.ToString();
                }

                // the content of the level
                Game._world = new World();
                //Game._world.Gravity = new Vector2(0, 0);
                Game._world.Gravity = new Vector2(0, -9.81f / 8f);
                controls = new Controls((OctoDash.OctoDash)Game, Game._world, this);
                Game.level = new Level((OctoDash.OctoDash)Game, this, LevelMap, _levelID);

                string objPath = System.IO.Path.Join(System.IO.Directory.GetCurrentDirectory(), "Content/octopus.obj");
                if (!System.IO.File.Exists(objPath))
                {
                    throw new System.Exception("\n\nThe File\n" + objPath + "\ndoesn't exist.\n\n");
                }

                // character = new Softbody(Game, ref Game._world, 26, 25, 15, 0.75f, 8, _startPositionAether, this, "OctopusTexture", objPath);
                Game.character = new Softbody(Game, ref Game._world, 26, 25, 15, 0.75f, _startPositionAether, this, "Octopus/OctopusTexture", objPath);
                player = new Player((OctoDash.OctoDash)Game, this);
                Game._stopWatch = new OctoStopWatch(Game);
            }
            cv = new CameraView((OctoDash.OctoDash)Game, this);


            // initialize non interactable gui elements

            if (gameTitle == null || gameTitle.List.Count == 0)
            {
                gameTitle = new GUI.ListElement(Constants.GameTitleFieldString, false, _defaultSpacing);
                gameTitle.Add(new GUI.MutableTextElement("menu/sunnyspells200", Constants.TitleButtonAssetString, "Octodash"));
            }

            nonInteractableGUI.Add(gameTitle);
            foreach (var item in nonInteractableGUI)
            {
                item.setNonSelectable();
                item.ClickEvent += OnClickIdle;
                item.SelectEvent += OnClickIdle;
            }

            // initialize interactable gui elements

            settings = new GUI.ListElement(Constants.SettingsMenuOptionsListString, false, _defaultSpacing);
            settings.Add(new GUI.TextElement("", Constants.VideoString));
            settings.Add(new GUI.TextElement("", Constants.AudioString));
            settings.Add(new GUI.TextElement("", Constants.BackString));

            videoSettings = new GUI.ListElement(Constants.VideoMenuOptionsListString, false, _defaultSpacing);
            videoSettings.Add(new GUI.TextWithIndicator("", Constants.WindowModeString, Constants.WindowModeDisplayString, _defaultSpacing));
            videoSettings.Add(new GUI.TextWithIndicator("", Constants.ResolutionString, Constants.String1440p, _defaultSpacing));
            videoSettings.Add(new GUI.TextWithIndicator("", Constants.String1080p, _defaultSpacing));
            // videoSettings.Add(new GUI.TextWithIndicator("", Constants.String720p, _defaultSpacing));
            videoSettings.Add(new GUI.TextElement("", Constants.BackString));

            audioSettings = new GUI.ListElement(Constants.AudioMenuOptionsListString, false, _defaultSpacing);
            audioSettings.Add(new GUI.TextWithIndicator("", Constants.ToggleMusicString, Constants.MusicStateString, _defaultSpacing));
            audioSettings.Add(new GUI.TextWithIndicator("", Constants.ToggleSoundFXString, Constants.SoundFXStateString, _defaultSpacing));
            audioSettings.Add(new GUI.TextWithIndicator("", Constants.MusicVolumeString, Constants.MusicVolDisplayString, _defaultSpacing));
            audioSettings.Add(new GUI.TextWithIndicator("", Constants.SFXVolumeString, Constants.SFXVolumeDisplayString, _defaultSpacing));
            audioSettings.Add(new GUI.TextElement("", Constants.BackString));

            levels = new GUI.ListElement(Constants.LevelSelectString, false, _defaultSpacing);
            levels.Add(new GUI.TextElement("", Constants.LevelTutorialStringWithLength));
            levels.Add(new GUI.TextElement("", Constants.Level1StringWithLength));
            levels.Add(new GUI.TextElement("", Constants.Level2StringWithLength));
            levels.Add(new GUI.TextElement("", Constants.Level3StringWithLength));
            // geting over it not yet finished
            //levels.Add(new GUI.TextElement("", Constants.Level4String));
            levels.Add(new GUI.TextElement("", Constants.BackString));

            GUI.MutableTextElement fs = videoSettings.FindIndicator(Constants.WindowModeString, Constants.WindowModeDisplayString);
            fs.Left = new GUI.ActionElement(Constants.WindowModeToggleActionString);
            fs.Right = fs.Left;
            fs.Left.SelectEvent += OnSelectToggleWindowMode;
            fs.Left.SelectEvent += OnSelectSound;

            GUI.MutableTextElement mMute = audioSettings.FindIndicator(Constants.ToggleMusicString, Constants.MusicStateString);
            mMute.Left = new GUI.ActionElement(Constants.MusicMuteString);
            mMute.Right = mMute.Left;
            GUI.MutableTextElement sfxMute = audioSettings.FindIndicator(Constants.ToggleSoundFXString, Constants.SoundFXStateString);
            sfxMute.Left = new GUI.ActionElement(Constants.SFXMuteString);
            sfxMute.Right = sfxMute.Left;

            mMute.Left.SelectEvent += OnSelectMute;
            sfxMute.Left.SelectEvent += OnSelectMute;
            mMute.Left.SelectEvent += OnSelectSound;
            sfxMute.Left.SelectEvent += OnSelectSound;

            GUI.MutableTextElement mVol = audioSettings.FindIndicator(Constants.MusicVolumeString, Constants.MusicVolDisplayString);
            mVol.Left = new GUI.ActionElement(Constants.MusicVolDown);
            mVol.Right = new GUI.ActionElement(Constants.MusicVolUp);
            mVol.Left.SelectEvent += OnSelectVolDown;
            mVol.Right.SelectEvent += OnSelectVolUp;
            mVol.Left.SelectEvent += OnSelectSound;
            mVol.Right.SelectEvent += OnSelectSound;

            GUI.MutableTextElement sfxVol = audioSettings.FindIndicator(Constants.SFXVolumeString, Constants.SFXVolumeDisplayString);
            sfxVol.Left = new GUI.ActionElement(Constants.SFXVolDown);
            sfxVol.Right = new GUI.ActionElement(Constants.SFXVolUp);
            sfxVol.Left.SelectEvent += OnSelectVolDown;
            sfxVol.Right.SelectEvent += OnSelectVolUp;
            sfxVol.Left.SelectEvent += OnSelectSound;
            sfxVol.Right.SelectEvent += OnSelectSound;

            interactableGUI.Add(main);
            interactableGUI.Add(settings);
            interactableGUI.Add(videoSettings);
            interactableGUI.Add(audioSettings);
            interactableGUI.Add(levels);
            foreach (var item in interactableGUI)
            {
                item.ClickEvent += OnClickSound;
                item.ClickEvent += OnClick;
                item.SelectEvent += OnSelectSound;
            }
        }

        virtual protected void setPositionsInteractableGui()
        {
            int width = Game._graphics.PreferredBackBufferWidth;
            int height = Game._graphics.PreferredBackBufferHeight;
            foreach (var item in interactableGUI)
            {
                item.CenterElement_leftAligned(width, height).MoveElement(new Point(-350, -50));
            }
        }

        virtual protected void setPositionsNonInteractableGui()
        {
            int width = Game._graphics.PreferredBackBufferWidth;
            int height = Game._graphics.PreferredBackBufferHeight;

            gameTitle?.List[0].CenterElement(width, height).MoveElement(new Point(0, -150));
        }


        public override void LoadContent()
        {
            base.LoadContent();

            foreach (var item in interactableGUI)
            {
                item.LoadContent(Game.Content);
            }
            setPositionsInteractableGui();

            foreach (var item in nonInteractableGUI)
            {
                item.LoadContent(Game.Content);
            }
            setPositionsNonInteractableGui();

            Game.level?.LoadContent();
            player?.LoadContent();
            Game.character?.LoadContent();
            Game._stopWatch?.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            if (Game.inTransition)
            {
                return;
            }
            gameTitle?.Update();
            switch (Game.GameState)
            {
                case Constants.GameState.MainMenu:
                    if (Controls.OnGamePadRelease(-1, Buttons.Back)
                    || Controls.OnKeyboardRelease(Keys.Escape))
                    {
                        Game.Exit();
                    }

                    main?.Update();
                    if (GUI.GUIElement.Selected == null)
                    {
                        if (main?.FindElement(Constants.ResumeString) != null)
                        {
                            // if we can find the resume option, wait until main is repopulated
                            return;
                        }
                        main?.Select();
                    }
                    break;
                case Constants.GameState.PauseMenu:

                    // this is a dev exit. can be removed for final release
                    if (Controls.OnKeyboardRelease(Keys.Escape))
                    {
                        Game.toMainMenu(this);
                        return;
                    }

                    allowTogglePause();
                    main?.Update();
                    if (GUI.GUIElement.Selected == null)
                    {
                        main?.Select();
                    }
                    allowBack();
                    cv?.Update(gameTime);
                    Game.character?.Update(gameTime);
                    break;
                case Constants.GameState.InGame:
                    if (Controls.OnKeyboardRelease(Keys.R))
                    {
#if DEBUG
                        this.prepareForTransition();
                        Game.startLevel(_levelID);
                        return;
#endif
                    }

                    cv?.Update(gameTime);
                    controls?.Update(gameTime);

                    Game.level?.Update(gameTime);
                    player?.Update();
                    Game.character?.Update(gameTime);
                    Game._world?.Step((float)gameTime.ElapsedGameTime.TotalSeconds);
                    Game._stopWatch?.Update(gameTime);

                    if (Game.level?.collectibles?.TotalCrabs != null && Game.level?.collectibles?.CollectedCrabs == Game.level?.collectibles?.TotalCrabs
#if DEBUG
|| Controls.OnKeyboardRelease(Keys.C)
#endif
                    )
                    {
                        prepareForTransition();
                        Game.toGameCompleteScreen(_levelID);
                        return;
                    }

                    allowTogglePause();

#if DEBUG
                    if (Controls.OnGamePadRelease(-1, Buttons.X))
                    {
                        Game.level.collectibles.CollectedCrabs++;
                    }
#endif
                    return;
                case Constants.GameState.PauseMenuSettings:
                    allowTogglePause();
                    updateSettings();
                    Game.character?.Update(gameTime);
                    cv?.Update(gameTime);
                    break;
                case Constants.GameState.MainMenuSettings:
                    updateSettings();
                    break;
                case Constants.GameState.PauseMenuVideoSettings:
                    allowTogglePause();
                    updateVideoSettings();
                    Game.character?.Update(gameTime);
                    cv?.Update(gameTime);
                    break;
                case Constants.GameState.MainMenuVideoSettings:
                    updateVideoSettings();
                    Game.character?.Update(gameTime);
                    cv?.Update(gameTime);
                    break;
                case Constants.GameState.PauseMenuAudioSettings:
                    allowTogglePause();
                    updateAudioSettings();
                    break;
                case Constants.GameState.MainMenuAudioSettings:
                    updateAudioSettings();
                    break;
                case Constants.GameState.LevelSelect:
                    updateLevelSelect();
                    break;
                case Constants.GameState.GameCompleted:
                case Constants.GameState.HighScoreState:
                    return;
                default:
                    break;
            }

            Controls.allowMenuTraversal();
        }

        protected void updateSettings()
        {
            settings?.Update();
            if (GUI.GUIElement.Selected == null)
            {
                settings.Select();
            }
            allowBack();
        }
        protected void updateVideoSettings()
        {
            videoSettings?.Update();
            GUI.MutableTextElement windowMode = videoSettings.FindIndicator(Constants.WindowModeString, Constants.WindowModeDisplayString);
            windowMode.Text = (Game._graphics.IsFullScreen) ? "fullscreen" : "windowed";

            if (GUI.GUIElement.Selected == null)
            {
                videoSettings.Select();
            }
            allowBack();
        }
        protected void updateAudioSettings()
        {
            audioSettings?.Update();
            GUI.MutableTextElement mss = audioSettings.FindIndicator(Constants.ToggleMusicString, Constants.MusicStateString);
            mss.Text = (Game.PlayingMusic) ? "on" : "off";
            GUI.MutableTextElement sfxs = audioSettings.FindIndicator(Constants.ToggleSoundFXString, Constants.SoundFXStateString);
            sfxs.Text = (Game.PlayingSFX) ? "on" : "off";

            GUI.MutableTextElement vs = audioSettings.FindIndicator(Constants.MusicVolumeString, Constants.MusicVolDisplayString);
            vs.Text = Game.createVolumeString(0);
            GUI.MutableTextElement sfxvs = audioSettings.FindIndicator(Constants.SFXVolumeString, Constants.SFXVolumeDisplayString);
            sfxvs.Text = Game.createVolumeString(1);

            if (GUI.GUIElement.Selected == null)
            {
                audioSettings.Select();
            }

            string element = GUI.GUIElement.Selected.Name;

            if (Controls.OnGamePadRelease(-1, Buttons.DPadLeft))
            {
                Log.Logger.Log("Volume Control: " + element);
                if (element == Constants.MusicStateString)
                {
                    Game.ToggleMusic();
                }
                else if (element == Constants.SoundFXStateString)
                {
                    Game.ToggleSoundFX();
                }
                else if (element == Constants.MusicVolDisplayString)
                {
                    Game.MusicVolDown();
                }
                else if (element == Constants.SFXVolumeDisplayString)
                {
                    Game.SFXVolDown();
                }
            }

            if (Controls.OnGamePadRelease(-1, Buttons.DPadRight))
            {
                Log.Logger.Log("Volume Control: " + element);
                if (element == Constants.MusicStateString)
                {
                    Game.ToggleMusic();
                }
                else if (element == Constants.SoundFXStateString)
                {
                    Game.ToggleSoundFX();
                }
                else if (element == Constants.MusicVolDisplayString)
                {
                    Game.MusicVolUp();
                }
                else if (element == Constants.SFXVolumeDisplayString)
                {
                    Game.SFXVolUp();
                }
            }

            allowBack();
        }

        protected void updateLevelSelect()
        {
            levels?.Update();
            if (GUI.GUIElement.Selected == null)
            {
                levels?.Select();
            }
            allowBack();
        }

        public override void Draw(GameTime gameTime)
        {
            if (Game.inTransition)
            {
                return;
            }

            Game._spriteBatch.Begin();

            // drawing menu elements
            switch (Game.GameState)
            {
                case Constants.GameState.MainMenu:
                    gameTitle?.Draw(Game._spriteBatch);
                    main?.Draw(Game._spriteBatch);
                    // sorry for butchering this chris
                    drawControlsOverview(new Rectangle(Game._graphics.PreferredBackBufferWidth - 540, Game._graphics.PreferredBackBufferHeight - 540, 512, 512));
                    break;
                case Constants.GameState.PauseMenu:
                    DrawLevel();
                    drawPauseBackDrop();
                    gameTitle?.Draw(Game._spriteBatch);
                    main?.Draw(Game._spriteBatch);
                    drawControlsOverview(new Rectangle(Game._graphics.PreferredBackBufferWidth - 540, Game._graphics.PreferredBackBufferHeight - 540, 512, 512));
                    break;
                case Constants.GameState.InGame:
                    DrawLevel();
                    break;
                case Constants.GameState.MainMenuSettings:
                    gameTitle?.Draw(Game._spriteBatch);
                    settings?.Draw(Game._spriteBatch);
                    break;
                case Constants.GameState.PauseMenuSettings:
                    DrawLevel();
                    drawPauseBackDrop();
                    gameTitle?.Draw(Game._spriteBatch);
                    settings?.Draw(Game._spriteBatch);
                    break;
                case Constants.GameState.MainMenuVideoSettings:
                    gameTitle?.Draw(Game._spriteBatch);
                    videoSettings?.Draw(Game._spriteBatch);
                    break;
                case Constants.GameState.PauseMenuVideoSettings:
                    DrawLevel();
                    drawPauseBackDrop();
                    gameTitle?.Draw(Game._spriteBatch);
                    videoSettings?.Draw(Game._spriteBatch);
                    break;
                case Constants.GameState.MainMenuAudioSettings:
                    gameTitle?.Draw(Game._spriteBatch);
                    audioSettings?.Draw(Game._spriteBatch);
                    break;
                case Constants.GameState.PauseMenuAudioSettings:
                    DrawLevel();
                    drawPauseBackDrop();
                    gameTitle?.Draw(Game._spriteBatch);
                    audioSettings?.Draw(Game._spriteBatch);
                    break;
                case Constants.GameState.LevelSelect:
                    gameTitle?.Draw(Game._spriteBatch);
                    levels?.Draw(Game._spriteBatch);
                    break;
                case Constants.GameState.GameCompleted:
                    DrawLevel();
                    drawPauseBackDrop();
                    gameTitle?.Draw(Game._spriteBatch);
                    main?.Draw(Game._spriteBatch);
                    break;
                case Constants.GameState.HighScoreState:
                    gameTitle?.Draw(Game._spriteBatch);
                    main?.Draw(Game._spriteBatch);
                    break;
            }
            Game._spriteBatch.End();
        }

        private void drawControlsOverview(Rectangle rectangle)
        {
            // assets will often be null in the first few frames during screen transitions.
            if (_controlsHint != null)
            {
                Game._spriteBatch.Draw(_controlsHint, rectangle, Color.White);
            }
        }

        private void drawPauseBackDrop()
        {
            if (_pauseBackDrop != null)
            {
                Game._spriteBatch.Draw(_pauseBackDrop, new Vector2(), Color.Black);
            }
        }

        private void DrawLevel()
        {
            Game._spriteBatch.End();
            Game.level?.Draw(Game._spriteBatch); // draw level tiles
            Game.character?.Draw(Game._spriteBatch);
            if (showStopWatch)
            {
                Game._stopWatch?.Draw();
            }
            cv?.Draw();
            Game._spriteBatch.Begin();
        }

        protected void allowTogglePause()
        {
            if (Controls.OnGamePadRelease(-1, Buttons.Start)
             || Controls.OnGamePadRelease(-1, Buttons.Back)
             || Controls.OnKeyboardRelease(Keys.P)
             || Controls.OnKeyboardRelease(Keys.Escape)
             )
            {
                goPause();
            }
        }

        protected void goPause()
        {
            Game.PauseGame();
            Game._stopWatch?.Pause();
        }

        protected void allowBack()
        {
            if (Controls.OnGamePadRelease(-1, Buttons.B)
             || Controls.OnGamePadRelease(-1, Buttons.Back)
             || Controls.OnKeyboardRelease(Keys.Escape))
            {
                goBack();
            }
        }

        protected void goBack()
        {
            switch (Game.GameState)
            {
                case Constants.GameState.LevelSelect:
                case Constants.GameState.MainMenuSettings:
                    Game.GameState = Constants.GameState.MainMenu;
                    break;
                case Constants.GameState.MainMenuVideoSettings:
                case Constants.GameState.MainMenuAudioSettings:
                    Game.GameState = Constants.GameState.MainMenuSettings;
                    break;
                case Constants.GameState.PauseMenuSettings:
                    Game.GameState = Constants.GameState.PauseMenu;
                    break;
                case Constants.GameState.PauseMenuVideoSettings:
                case Constants.GameState.PauseMenuAudioSettings:
                    Game.GameState = Constants.GameState.PauseMenuSettings;
                    break;
                case Constants.GameState.GameCompleted:
                case Constants.GameState.HighScoreState:
                    Game.toMainMenu(this);
                    return;
            }
        }


        public void prepareForTransition()
        {
            main = null;
            settings = null;
            videoSettings = null;
            audioSettings = null;
            gameTitle = null;
            levels = null;
            interactableGUI.Clear();
            nonInteractableGUI.Clear();
            this.UnloadContent();
        }

        public void OnClickIdle(string element) { }
        public void OnClickSound(string element)
        {
            Game.PlaySound(4);
        }
        public void OnSelectSound(string element)
        {
            Game.PlaySound(3);
        }

        public void OnSelectMute(string element)
        {
            if (element == Constants.MusicMuteString)
            {
                Game.ToggleMusic();
            }
            else if (element == Constants.SFXMuteString)
            {
                Game.ToggleSoundFX();
            }
        }

        public void OnSelectVolDown(string element)
        {
            if (element == Constants.MusicVolDown)
            {
                Game.MusicVolDown();
            }
            else if (element == Constants.SFXVolDown)
            {
                Game.SFXVolDown();
            }
        }
        public void OnSelectVolUp(string element)
        {
            if (element == Constants.MusicVolUp)
            {
                Game.MusicVolUp();
            }
            else if (element == Constants.SFXVolUp)
            {
                Game.SFXVolUp();
            }
        }

        public void OnSelectToggleWindowMode(string element)
        {
            Game.ToggleWindowMode();
            setPositionsInteractableGui();
            setPositionsNonInteractableGui();
        }

        public void OnClick(string element)
        {
            switch (element)
            {
                // main menu
                case Constants.PlayString:
                    Game.GameState = Constants.GameState.LevelSelect;
                    break;
                case Constants.ExitString:
                    Game.Exit();
                    break;
                case Constants.SettingsString:

                    switch (Game.GameState)
                    {
                        case Constants.GameState.MainMenu:
                            Game.GameState = Constants.GameState.MainMenuSettings;
                            break;
                        case Constants.GameState.PauseMenu:
                            Game.GameState = Constants.GameState.PauseMenuSettings;
                            break;
                    }
                    break;
                // general
                case Constants.BackString:
                    goBack();
                    break;
                // settings
                case Constants.VideoString:
                    switch (Game.GameState)
                    {
                        case Constants.GameState.MainMenuSettings:
                            Game.GameState = Constants.GameState.MainMenuVideoSettings;
                            break;
                        case Constants.GameState.PauseMenuSettings:
                            Game.GameState = Constants.GameState.PauseMenuVideoSettings;
                            break;
                    }
                    break;
                case Constants.AudioString:
                    switch (Game.GameState)
                    {
                        case Constants.GameState.MainMenuSettings:
                            Game.GameState = Constants.GameState.MainMenuAudioSettings;
                            break;
                        case Constants.GameState.PauseMenuSettings:
                            Game.GameState = Constants.GameState.PauseMenuAudioSettings;
                            break;
                    }
                    break;
                // video settings
                case Constants.WindowModeString:
                case Constants.WindowModeDisplayString:
                    OnSelectToggleWindowMode(Constants.WindowModeDisplayString);
                    break;
                case Constants.String720p:
                    Game._graphics.PreferredBackBufferHeight = Preferences.Instance.WindowHeight = 720;
                    Game._graphics.PreferredBackBufferWidth = Preferences.Instance.WindowWidth = 1080;
                    Game._graphics.ApplyChanges();
                    setPositionsInteractableGui();
                    setPositionsNonInteractableGui();
                    break;
                case Constants.String1080p:
                    Game._graphics.PreferredBackBufferHeight = Preferences.Instance.WindowHeight = 1080;
                    Game._graphics.PreferredBackBufferWidth = Preferences.Instance.WindowWidth = 1920;
                    Game._graphics.ApplyChanges();
                    setPositionsInteractableGui();
                    setPositionsNonInteractableGui();
                    break;
                case Constants.String1440p:
                    Game._graphics.PreferredBackBufferHeight = Preferences.Instance.WindowHeight = 1440;
                    Game._graphics.PreferredBackBufferWidth = Preferences.Instance.WindowWidth = 2560;
                    Game._graphics.ApplyChanges();
                    setPositionsInteractableGui();
                    setPositionsNonInteractableGui();
                    break;
                // audio settings
                case Constants.ToggleMusicString:
                case Constants.MusicStateString:
                    Game.ToggleMusic();
                    break;
                case Constants.ToggleSoundFXString:
                case Constants.SoundFXStateString:
                    Game.ToggleSoundFX();
                    break;
                case Constants.ResumeString:
                    goPause();
                    break;
                case Constants.RestartString:
                    int id = Game.level.LevelId;
                    prepareForTransition();
                    Game.startLevel(id);
                    break;
                case Constants.ExitToMainMenuString:
                    Game.toMainMenu(this);
                    break;


                // level select
                case Constants.LevelTutorialStringWithLength:
                    this.UnloadContent();
                    Game.startLevel(0);
                    break;
                case Constants.Level1StringWithLength:
                    this.UnloadContent();
                    Game.startLevel(1);
                    break;
                case Constants.Level2StringWithLength:
                    this.UnloadContent();
                    Game.startLevel(2);
                    break;
                case Constants.Level3StringWithLength:
                    this.UnloadContent();
                    Game.startLevel(3);
                    break;
                case Constants.Level4StringWithLength:
                    this.UnloadContent();
                    Game.startLevel(4);
                    break;
                default:
                    break;
            }
        }
    }

    public class MenuScreen : LevelScreen
    {
        // https://www.monogameextended.net/docs/features/screen-management/screen-management/

        private new OctoDash.OctoDash Game => (OctoDash.OctoDash)base.Game;

        public MenuScreen(OctoDash.OctoDash game) : base(game)
        {
            Name = Constants.MENU_SCREEN_NAME;
            Game.IsMouseVisible = OctoDash.OctoDash.supportsMouse;
            showStopWatch = false;
        }

        public override void Initialize()
        {
            main = new GUI.ListElement(Constants.MainMenuOptionsListString, false, _defaultSpacing);
            main.Add(new GUI.TextElement("", Constants.PlayString));
            main.Add(new GUI.TextElement("", Constants.SettingsString));
            main.Add(new GUI.TextElement("", Constants.ExitString));
            base.Initialize();
        }

        public override void LoadContent()
        {
            _controlsHint = Game.Content.Load<Texture2D>("controls");
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            Game.GraphicsDevice.Clear(Color.DarkCyan);
            base.Draw(gameTime);
        }

    }


    public abstract class ProperLevel : LevelScreen
    {

        public ProperLevel(OctoDash.OctoDash game) : base(game) { }

        public ProperLevel(OctoDash.OctoDash game, string levelMap) : base(game, levelMap) { }

        public override void Initialize()
        {
            showStopWatch = true;
            _isProperLevel = true;

            main = new GUI.ListElement(Constants.PauseMenuOptionsListString, false, _defaultSpacing);
            main.Add(new GUI.TextElement("", Constants.ResumeString));
            main.Add(new GUI.TextElement("", Constants.RestartString));
            main.Add(new GUI.TextElement("", Constants.SettingsString));
            main.Add(new GUI.TextElement("", Constants.ExitToMainMenuString));

            base.Initialize();
        }

        public override void LoadContent()
        {
            base.LoadContent();
            _controlsHint = Game.Content.Load<Texture2D>("controls");
            _pauseBackDrop = Game.Content.Load<Texture2D>("menu/pauseMenuBackdrop");
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkCyan);
            base.Draw(gameTime);
        }
    }

    public class DemoLevel : ProperLevel
    {
        public DemoLevel(OctoDash.OctoDash game) : base(game)
        {
            _levelID = -1;
        }
        public override void Initialize()
        {
            base.Initialize();
            Name += Constants.DEMO_LVL_SCREEN_NAME_SUFFIX;
        }
    }

    public class TutorialLevel : ProperLevel
    {
        public TutorialLevel(OctoDash.OctoDash game) : base(game, "underwater/tutorial")
        {
            _levelID = 0;
            _startPositionAether = Units.TiledToAether(new Vector2(2, 16));
        }
        public override void Initialize()
        {
            base.Initialize();
            //_startPositionAether = Units.MonoGameToAether(new Vector2(3584, 9984));
        }
    }

    public class GeneralLevel : ProperLevel
    {
        public GeneralLevel(OctoDash.OctoDash game, int level_ID) : base(game, "underwater/level" + level_ID.ToString())
        {
            _levelID = level_ID;
        }

        public GeneralLevel(OctoDash.OctoDash game, int level_ID, Vector2 startPos) : base(game, "underwater/level" + level_ID.ToString())
        {
            _levelID = level_ID;
            _startPositionAether = Units.MonoGameToAether(startPos);
        }
    }

    public class GameCompleteScreen : LevelScreen
    {
        private bool _playerNameEntered;

        private double lastTime = 0;
        private Color current = Color.White;
        public bool PlayerNameEntered
        {
            get { return _playerNameEntered; }
            set { _playerNameEntered = value; }
        }

        private HighScore _score;
        public HighScore Score
        {
            get { return _score; }
            set { _score = value; }
        }

        public GameCompleteScreen(OctoDash.OctoDash game, int levelID) : base(game)
        {
            interactableGUI.Clear();
            nonInteractableGUI.Clear();

            showStopWatch = true;
            this._levelID = levelID;
        }

        GUI.ListElement info;
        public override void Initialize()
        {
            // public HighScore(string time, string timePenalty, string todalTime, string collectedCrabs, string collectedStarfishes)
            _score = new HighScore(
                Game._stopWatch.getTimeString(),
                Game._stopWatch.getPenalties(),
                Game._stopWatch.getTotalTimeString(),
                Game.level.collectibles.CollectedCrabs.ToString()
            );

            info = new GUI.ListElement(Constants.PauseMenuOptionsListString, false, _defaultSpacing);
            info.Add(new GUI.TextWithIndicator("", Constants.TimeString, _score.Time, _defaultSpacing));
            info.Add(new GUI.TextWithIndicator("", Constants.TimePenaltyString, _score.TimePenalty, _defaultSpacing));
            info.Add(new GUI.TextWithIndicator("", Constants.TotalTimeString, _score.TotalTime, _defaultSpacing));
            info.Add(new GUI.TextWithIndicator("", Constants.CollectedCrabsString, _score.CollectedCrabs, _defaultSpacing));

            nonInteractableGUI.Add(info);

            gameTitle = new GUI.ListElement(Constants.GameTitleFieldString, false, _defaultSpacing);
            gameTitle.Add(new GUI.MutableTextElement("menu/sunnyspells200", Constants.TitleButtonAssetString, "Level Complete!"));

            main = new GUI.ListElement(Constants.PauseMenuOptionsListString, false, _defaultSpacing);
            main.Add(new GUI.TextWithIndicator("", Constants.NameQueryString, Constants.PlayerString, _defaultSpacing));
            // main.Add(new GUI.TextElement("", Constants.NextLevelString));
            /* main.Add(new GUI.TextElement("", Constants.RestartString)); */
            /* main.Add(new GUI.TextElement("", Constants.ExitToMainMenuString)); */

            base.Initialize();

            Name += Constants.GAME_COMPLETE_SCREEN_NAME;
        }

        private void populateHighScore()
        {
            info = new GUI.ListElement(Constants.HighScoresCollectionNameString, false, _defaultSpacing);
            List<HighScore> hs = Game._persistence.loadHighscores(Score);
            bool highlightedTheScore = false;
            foreach (var item in hs)
            {
                string pn = item.Rank.ToString() + ". " + ((String.IsNullOrWhiteSpace(item.PlayerName)) ? "anonimous" : item.PlayerName);
                if (String.IsNullOrWhiteSpace(item.TotalTime))
                {
                    continue;
                }
                Log.Logger.Log("Loading score : " + pn + " " + item.TotalTime);
                info.Add(new GUI.TextWithIndicator("", pn, item.TotalTime, _defaultSpacing));
                if (!highlightedTheScore)
                {
                    if (item.Equals(Score))
                    {
                        GUI.TextWithIndicator ownScoreField = ((GUI.TextWithIndicator)info.List[info.List.Count - 1]);
                        ownScoreField.TextElem.SetColor = Color.White;
                        ownScoreField.Indicator.SetColor = Color.White;
                        highlightedTheScore = true;
                    }
                }
            }
            nonInteractableGUI.Add(info);
            base.Initialize();
            base.LoadContent();
        }

        protected override void setPositionsInteractableGui()
        {
            int width = Game._graphics.PreferredBackBufferWidth;
            int height = Game._graphics.PreferredBackBufferHeight;
            Point p1 = new Point(-650, -50);
            Point p2 = new Point(-100, -50);
            main.CenterElement_leftAligned(width, height).MoveElement(p2);
        }

        protected override void setPositionsNonInteractableGui()
        {
            int width = Game._graphics.PreferredBackBufferWidth;
            int height = Game._graphics.PreferredBackBufferHeight;

            info.CenterElement_leftAligned(width, height).MoveElement(new Point(-650, -50));
            gameTitle?.List[0].CenterElement(width, height).MoveElement(new Point(0, -150));
        }


        public override void LoadContent()
        {
            _pauseBackDrop = Game.Content.Load<Texture2D>("menu/pauseMenuBackdrop");
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            // only update within expected state
            if (Game.inTransition || (Game.GameState != Constants.GameState.GameCompleted && Game.GameState != Constants.GameState.HighScoreState))
            {
                return;
            }

            // allow back to main menu
            if (Controls.OnGamePadRelease(-1, Buttons.Back) || Controls.OnKeyboardRelease(Keys.Escape))
            {
                Game.toMainMenu(this);
                return;
            }

            // ensure correct menu element is selected
            if (GUI.GUIElement.Selected == null)
            {
                main?.Select();
            }

            if (GUI.GUIElement.Selected?.Name == Constants.PlayerString)
            {
                GUI.MutableTextElement playerNameField = ((GUI.MutableTextElement)GUI.GUIElement.Selected);
                if (Controls.OnKeyboardRelease(Keys.Enter))
                {
                    _playerNameEntered = true;
                    Score.PlayerName = (String.IsNullOrWhiteSpace(playerNameField.Text)) ? "anonimous" : playerNameField.Text;
                    Score.LevelID = _levelID;
                    Game._persistence.storeHighscore(Score);

                    // ((GUI.MutableTextElement)gameTitle.List[0]).Text = "High Score";
                    ((GUI.MutableTextElement)gameTitle.List[0]).Text = Constants.getLevelString(_levelID);

                    main.List.Remove(main.FindElement(Constants.NameQueryString));
                    main.Add(new GUI.TextElement("", Constants.RestartString));
                    main.Add(new GUI.TextElement("", Constants.ExitToMainMenuString));
                    populateHighScore();
                    Game.GameState = Constants.GameState.HighScoreState;
                }
                else if (!_playerNameEntered)
                {
                    Controls.allowWriting(playerNameField);

                    if (!playerNameField.Cleared && gameTime.TotalGameTime.Seconds - lastTime >= 0.5)
                    {
                        current = current == Color.White ? Color.Cyan : Color.White;
                        lastTime = gameTime.TotalGameTime.Seconds;
                    }
                    playerNameField.SetColor = current;
                }
            }

            // explicitly update what should animate
            gameTitle?.Update();
            main?.Update();
            info?.Update();
            Game.level?.Update(gameTime); // Note: updating level allows collecting collectibles if the character falls into them. This is probably unintended but also unproblematic.
            Game.character?.Update(gameTime);
            cv?.Update(gameTime);
            player?.Update();
            Game._world?.Step((float)gameTime.ElapsedGameTime.TotalSeconds);

            Controls.allowMenuTraversal();
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkCyan);
            base.Draw(gameTime);
            Game._spriteBatch.Begin();
            info.Draw(Game._spriteBatch);
            Game._spriteBatch.End();
        }
    }
    public class OctoStopWatch
    {
        private OctoDash.OctoDash _game;
        private Stopwatch _stopwatch;
        private SpriteFont _stopwatchFont;
        private string _text = "";
        private Vector2 _position;
        private Vector2 _textMiddlePoint;
        private TimeSpan penalty = new TimeSpan(0, 0, 0);
        private TimeSpan penaltyAppliedAt = new TimeSpan();
        private static TimeSpan threshold = new TimeSpan(0, 0, 2);

        public OctoStopWatch(OctoDash.OctoDash game)
        {
            _game = game;
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        public void LoadContent()
        {
            _stopwatchFont = _game.Content.Load<SpriteFont>("menu/sunnyspells50");
        }

        public void Update(GameTime gameTime)
        {
            _text = (_stopwatch.Elapsed + penalty).ToString(@"%m\:ss\:f");
            _textMiddlePoint = _stopwatchFont.MeasureString("mm:ss:f") / 2;
            _position = new Vector2(_game.Window.ClientBounds.Width * 0.95f, _game.Window.ClientBounds.Height * 0.05f);
        }

        public void Draw()
        {
            _game._spriteBatch.Begin();
            if (!penaltyAppliedAt.Equals(TimeSpan.Zero) && _stopwatch.Elapsed - penaltyAppliedAt < threshold)
            {
                _game._spriteBatch.DrawString(_stopwatchFont, _text, _position, Color.Red, 0, _textMiddlePoint, 1.5f, SpriteEffects.None, 0.5f);
            }
            else
            {
                _game._spriteBatch.DrawString(_stopwatchFont, _text, _position, Color.Black, 0, _textMiddlePoint, 1f, SpriteEffects.None, 0.5f);
            }
            _game._spriteBatch.End();
        }

        public void Pause()
        {
            if (_stopwatch.IsRunning)
            {
                _stopwatch.Stop();
            }
            else
            {
                _stopwatch.Start();
            }
        }

        public void addPenalty()
        {
            penaltyAppliedAt = _stopwatch.Elapsed;
            penalty += new TimeSpan(0, 0, 10);
        }

        public string getTimeString()
        {
            return (_stopwatch.Elapsed).ToString(@"%m\:ss\:f");
        }

        public string getPenalties()
        {
            return (penalty).ToString(@"%m\:ss\:f");
        }

        public string getTotalTimeString()
        {
            return (_stopwatch.Elapsed + penalty).ToString(@"%m\:ss\:f");
        }

    }
}
