using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using MonoGame.Extended.Screens;
using MonoGame.Extended.Screens.Transitions;
using tainicom.Aether.Physics2D.Dynamics;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using OctoDash;
using Menu;



namespace OctoDash
{
    public class OctoDash : Game
    {
        public static bool supportsMouse = false;

        // general graphics / game
        public GraphicsDeviceManager _graphics;
        public SpriteBatch _spriteBatch;

        public PersistenceManager _persistence;

        private Preferences preferences;

        public Level level;
        public Softbody character;
        public World _world;

        public OctoStopWatch _stopWatch;
        public bool inTransition; // if this is true, we are changing the screen: don't update or draw at all.
        public List<SoundEffect> soundEffects;
        public List<Song> songs;
        public Song currentSong;
        public bool playingMusicInMenu = true;
        public bool PlayingMusic
        {
            get { return Preferences.Instance.PlayingMusic; }
            set { Preferences.Instance.PlayingMusic = value; }

        }
        public bool PlayingSFX
        {
            get { return Preferences.Instance.PlayingSFX; }
            set { Preferences.Instance.PlayingSFX = value; }
        }
        public float MusicVolume
        {
            get { return Preferences.Instance.MusicVolume; }
            set
            {
                Log.Logger.Log("MusicVolume: received value " + value.ToString());
                Preferences.Instance.MusicVolume = MathF.Round(MathHelper.Clamp(value, 0f, 1f), 2);
                MediaPlayer.Volume = Preferences.Instance.MusicVolume;
                Log.Logger.Log("MusicVolume: set to value " + MediaPlayer.Volume.ToString());
            }
        }
        public float SFXVolume
        {
            get { return Preferences.Instance.SFXVolume; }
            set
            {
                Log.Logger.Log("SFXVolume: received value " + value.ToString());
                Preferences.Instance.SFXVolume = MathF.Round(MathHelper.Clamp(value, 0f, 1f), 2);
                SoundEffect.MasterVolume = Preferences.Instance.SFXVolume;
                Log.Logger.Log("SFXVolume: set to value " + SoundEffect.MasterVolume.ToString());
            }
        }

        private int _volumeSteps = 10;
        private float _volumeResolution = 1f / 10;
        public void MusicVolDown()
        {
            MusicVolume -= _volumeResolution;
        }
        public void SFXVolDown()
        {
            SFXVolume -= _volumeResolution;
        }
        public void MusicVolUp()
        {
            MusicVolume += _volumeResolution;
        }
        public void SFXVolUp()
        {
            SFXVolume += _volumeResolution;
        }


        public string createVolumeString(int volumeNum)
        {
            float volume = (volumeNum == 0) ? Preferences.Instance.MusicVolume : Preferences.Instance.SFXVolume;
            string vs = "[";
            for (int i = 0; i < _volumeSteps; i++)
            {
                if (volume / _volumeResolution - i < Double.Epsilon)
                {
                    vs += "_";
                }
                else
                {
                    vs += "+";
                }
            }
            vs += "]";
            return vs;
        }

        private Constants.GameState _gameState = Constants.GameState.MainMenu;
        public Constants.GameState GameState
        {
            get { return _gameState; }
            set
            {
                GUI.GUIElement.Unselect();
                _gameState = value;
                Log.Logger.Log("Game state changed to " + _gameState.ToString());
                Preferences.Instance.Persist();
            }
        }

        private readonly ScreenManager _screenManager;
        private float screenTransitionTime = 0.5f;


        public OctoDash()
        {

            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            soundEffects = new List<SoundEffect>();
            songs = new List<Song>();

            // load saved preferences
            _persistence = new PersistenceManager();
            preferences = Preferences.Instance;
            MusicVolume = Preferences.Instance.MusicVolume;
            SFXVolume = Preferences.Instance.SFXVolume;

            _screenManager = new ScreenManager();
            Components.Add(_screenManager);
            IsMouseVisible = supportsMouse;
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (!Preferences.Instance.Resolution.Equals(new Point()))
            {
                _graphics.PreferredBackBufferWidth = Preferences.Instance.WindowWidth;
                _graphics.PreferredBackBufferHeight = Preferences.Instance.WindowHeight;
            }
            else
            {
                _graphics.PreferredBackBufferWidth = GraphicsDevice.Adapter.CurrentDisplayMode.Width;
                _graphics.PreferredBackBufferHeight = GraphicsDevice.Adapter.CurrentDisplayMode.Height;
            }
            _graphics.IsFullScreen = Preferences.Instance.IsFullscreen;
            _graphics.ApplyChanges();
        }


        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            soundEffects.Add(Content.Load<SoundEffect>("Audio/suction_cup"));
            soundEffects.Add(Content.Load<SoundEffect>("Audio/star"));
            soundEffects.Add(Content.Load<SoundEffect>("Audio/crab"));
            soundEffects.Add(Content.Load<SoundEffect>("Audio/menu_select"));
            soundEffects.Add(Content.Load<SoundEffect>("Audio/menu_onclick"));
            soundEffects.Add(Content.Load<SoundEffect>("Audio/rock_break"));
            soundEffects.Add(Content.Load<SoundEffect>("Audio/hit"));
            soundEffects.Add(Content.Load<SoundEffect>("Audio/victory"));
            soundEffects.Add(Content.Load<SoundEffect>("Audio/spike_hit"));
            soundEffects.Add(Content.Load<SoundEffect>("Audio/spike_hit_2"));
            soundEffects.Add(Content.Load<SoundEffect>("Audio/spike_hit_alt"));
            soundEffects.Add(Content.Load<SoundEffect>("Audio/spike_hit_alt_2"));
            songs.Add(Content.Load<Song>("Audio/calm"));
            songs.Add(Content.Load<Song>("Audio/funny"));
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = Preferences.Instance.MusicVolume;
            toMainMenu(null);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            Controls.GetState();
        }

        protected override void Draw(GameTime gameTime)
        {
            // GraphicsDevice.Clear(Color.DarkCyan);
            base.Draw(gameTime);
        }

        private void LoadScreen(GameScreen screen)
        {
            _screenManager.LoadScreen(screen, new FadeTransition(GraphicsDevice, Color.Black, screenTransitionTime));
            inTransition = false;
        }

        private void PrepareScreenForTransition(LevelScreen levelScreen)
        {
            levelScreen?.prepareForTransition();
        }

        public void toMainMenu(LevelScreen levelScreen)
        {
            inTransition = true;
            PrepareScreenForTransition(levelScreen);
            GameState = Constants.GameState.MainMenu;
            PlayMenuSong();
            _world = null;
            level = null;
            character = null;
            _stopWatch = null;
            LoadScreen(new MenuScreen(this));
        }

        public void startLevel(int __levelID)
        {
            Log.Logger.Log("Starting level with levelID=" + __levelID.ToString());
            inTransition = true;
            GameState = Constants.GameState.InGame;
            resetLevel();
            PlaySong(1);

            switch (__levelID)
            {
                case 0:
                    LoadScreen(new Menu.TutorialLevel(this));
                    break;
                case 1:
                    LoadScreen(new GeneralLevel(this, 1, new Vector2(128 * 96, 128 * 57)));
                    break;
                case 2:
                    LoadScreen(new GeneralLevel(this, 2, new Vector2(9472, 13952)));
                    break;
                case 3:
                    LoadScreen(new GeneralLevel(this, 3, new Vector2(3584, 9984)));
                    break;
                case 4:
                    LoadScreen(new GeneralLevel(this, 4, Units.TiledToMonoGame(2, 198)));
                    break;
                default:
                    if (__levelID > 0)
                    {
                        LoadScreen(new Menu.GeneralLevel(this, __levelID));
                        break;
                    }
                    LoadScreen(new Menu.DemoLevel(this));
                    break;
            }
        }

        /* All this method needs to do is ensure the winning condition is not met.
         * Reason:
         * - The monogame-extended screen seems to get repurposed even though we pass a newly created instance.
         * - When restarting, the existing level will be updated before monogame-extended re-initializes all values.
         * - We therefore need undo the winning condition to prevent immediate transition to GameState GameComplete 
         */
        public void resetLevel()
        {
            if (level == null || level.collectibles == null)
            {
                return;
            }

            level.collectibles.CollectedCrabs = 0;
            //level.collectibles.CollectedStars = 0;
        }

        public void ToggleWindowMode()
        {
            _graphics.IsFullScreen = Preferences.Instance.IsFullscreen = !_graphics.IsFullScreen;
            if (_graphics.IsFullScreen)
            {
                _graphics.PreferredBackBufferWidth = Preferences.Instance.WindowWidth = GraphicsDevice.Adapter.CurrentDisplayMode.Width;
                _graphics.PreferredBackBufferHeight = Preferences.Instance.WindowHeight = GraphicsDevice.Adapter.CurrentDisplayMode.Height;
            }
            _graphics.ApplyChanges();
        }

        public void ToggleSoundFX()
        {
            PlayingSFX = !PlayingSFX;
        }

        public void PlaySound(int sound)
        {

            if (PlayingSFX)
            {
                soundEffects[sound].Play();
            }
        }

        public void ToggleMusic()
        {
            if (PlayingMusic)
            {
                MediaPlayer.Stop();
                PlayingMusic = !PlayingMusic;
            }
            else
            {
                PlayingMusic = !PlayingMusic;
                switch (_gameState)
                {
                    case Constants.GameState.InGame:
                    case Constants.GameState.PauseMenu:
                    case Constants.GameState.PauseMenuSettings:
                    case Constants.GameState.PauseMenuVideoSettings:
                    case Constants.GameState.PauseMenuAudioSettings:
                        PlaySong(1);
                        break;
                    case Constants.GameState.MainMenu:
                    case Constants.GameState.MainMenuSettings:
                    case Constants.GameState.MainMenuVideoSettings:
                    case Constants.GameState.MainMenuAudioSettings:
                        PlayMenuSong();
                        break;
                    default:
                        PlayMenuSong();
                        break;
                }
            }
        }

        public void PlayMenuSong()
        {
            currentSong = songs[0];
            if (playingMusicInMenu)
            {
                PlaySong(0);
            }
            else
            {
                MediaPlayer.Stop();
            }
        }

        public void PlaySong(int song)
        {
            currentSong = songs[song];
            if (PlayingMusic)
            {
                MediaPlayer.Play(currentSong);
            }
        }

        /* Note: stopwatch etc needs to be managed separately. */
        public void PauseGame()
        {
            switch (GameState)
            {
                case Constants.GameState.InGame:
                    GameState = Constants.GameState.PauseMenu;
                    MediaPlayer.Pause();
                    Log.Logger.Log("Game Paused");
                    break;
                case Constants.GameState.PauseMenu:
                case Constants.GameState.PauseMenuSettings:
                case Constants.GameState.PauseMenuVideoSettings:
                case Constants.GameState.PauseMenuAudioSettings:
                default:
                    GameState = Constants.GameState.InGame;
                    MediaPlayer.Resume();
                    Log.Logger.Log("Game Unpaused");
                    break;
            }
        }

        public void toGameCompleteScreen(int levelID)
        {
            inTransition = true;
            GameState = Constants.GameState.GameCompleted;
            MediaPlayer.Stop();
            PlaySound(7);
            LoadScreen(new GameCompleteScreen(this, levelID));
        }
    }
}
