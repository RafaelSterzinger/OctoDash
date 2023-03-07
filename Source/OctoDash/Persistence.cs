using System.IO;
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
using LiteDB;

public class PersistenceManager
{
    private string gamePath;
    private string _saveFile;
    public string SaveFile { get { return _saveFile; } }
    public PersistenceManager()
    {
        gamePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Stardew-Team4", "OctoDash");
        Log.Logger.Log("Game data will we persisted in " + gamePath);
        if (!System.IO.Directory.Exists(gamePath))
        {
            System.IO.Directory.CreateDirectory(gamePath);
        }

        _saveFile = System.IO.Path.Combine(gamePath, Constants.SaveFileBaseNameString);
        Log.Logger.Log("Save file will be " + SaveFile);

        Preferences.Persistence = this;
    }


    public List<HighScore> loadHighscores(HighScore currentScore)
    {
        // https://www.litedb.org/docs/getting-started/

        Log.Logger.Log("Loading highscores.");
        List<HighScore> scores = new List<HighScore>();
        List<HighScore> _scores = new List<HighScore>();
        using (var db = new LiteDatabase(SaveFile))
        {
            var col = db.GetCollection<HighScore>(Constants.HighScoresCollectionNameString);

            scores.AddRange(col.Query()
            .OrderBy(x => DateTime.Parse(x.TotalTime))
            .Where(x => x.LevelID == currentScore.LevelID)
            .ToList());

            Log.Logger.Log("Loaded only for levelID=" + currentScore.LevelID);

            int currentRank = 0;
            for (int i = 0; i < scores.Count; i++)
            {
                scores[i].Rank = i;
                if (scores[i].Equals(currentScore))
                {
                    currentRank = i;
                }
            }

            for (int i = 0; i < scores.Count; i++)
            {
                if (i > currentRank - 5 && i < currentRank + 5)
                {
                    _scores.Add(scores[i]);
                }
            }

        }
        return _scores;
    }

    public void storeHighscore(HighScore score)
    {
        using (var db = new LiteDatabase(SaveFile))
        {
            var col = db.GetCollection<HighScore>(Constants.HighScoresCollectionNameString);
            col.Insert(score);
            col.EnsureIndex(x => x.TotalTime);
        }

        Log.Logger.Log("Stored highscore of player " + score.PlayerName);
    }

    public void storePreferences()
    {
        using (var db = new LiteDatabase(SaveFile))
        {
            var col = db.GetCollection<Preferences>(Constants.PreferencesCollectionNameString);
            if (col.Count() == 0)
            {
                col.Insert(Preferences.Instance);
            }
            else
            {
                var pref = col.Query()
                .First();
                pref.receiveConfiguration(Preferences.Instance);
                col.Update(pref);
            }
        }

        Log.Logger.Log("Preferences saved.");
    }

    public void loadPreferences()
    {
        // Preferences.Instance.setDefaults();
        using (var db = new LiteDatabase(SaveFile))
        {
            var col = db.GetCollection<Preferences>(Constants.PreferencesCollectionNameString);
            if (col.Count() > 0)
            {
                Preferences.Instance.receiveConfiguration(col.Query().First());
            }
        }
    }

}

public class Preferences
{

    [BsonId]
    public ObjectId Id { get; set; }
    private static PersistenceManager _persistence;
    public static PersistenceManager Persistence
    {
        get { return _persistence; }
        set
        {
            _persistence = value;
        }
    }

    // audio

    // 0.0f muted, 1.0f max
    private float _musicVolume;
    public float MusicVolume
    {
        get { return _musicVolume; }
        set
        {
            _musicVolume = value;
            // _persistence.storePreferences();
        }
    }

    private float _sfxVolume;
    public float SFXVolume
    {
        get { return _sfxVolume; }
        set
        {
            _sfxVolume = value;
            // _persistence.storePreferences();
        }
    }

    private bool _playingMusic;
    public bool PlayingMusic
    {
        get { return _playingMusic; }
        set
        {
            _playingMusic = value;
            // _persistence.storePreferences();
        }
    }

    private bool _playingSFX;
    public bool PlayingSFX
    {
        get { return _playingSFX; }
        set
        {
            _playingSFX = value;
            // _persistence.storePreferences();
        }
    }


    // video
    private bool _isFullscreen;
    public bool IsFullscreen
    {
        get { return _isFullscreen; }
        set
        {
            _isFullscreen = value;
            // _persistence.storePreferences();
        }
    }

    private Point _resolution;
    public Point Resolution
    {
        get { return _resolution; }
        set
        {
            _resolution = value;
            // _persistence.storePreferences();
        }
    }

    public int WindowWidth
    {
        get { return Resolution.X; }
        set
        {
            Resolution = new Point(value, Resolution.Y);
        }
    }
    public int WindowHeight
    {
        get { return Resolution.Y; }
        set
        {
            Resolution = new Point(Resolution.X, value);
        }
    }


    private static Preferences _instance;

    private Preferences()
    {
        this.setDefaults();
    }
    public static Preferences Instance
    {
        get
        {
            if (_instance == null)
            {
                if (_persistence == null)
                {
                    throw new Exception("Persistence needs to be instantiated before Preferences!");
                }
                _instance = new Preferences();
                _persistence.loadPreferences();
            }
            return _instance;
        }
    }

    public void receiveConfiguration(Preferences other)
    {

        // audio
        this._musicVolume = other._musicVolume;
        this._sfxVolume = other._sfxVolume;
        this._playingMusic = other._playingMusic;
        this._playingSFX = other._playingMusic;

        // video
        this._isFullscreen = other._isFullscreen;
        this._resolution = other._resolution;

    }

    public void setDefaults()
    {
        // audio
        this._musicVolume = 0.5f;
        this._sfxVolume = 1f;
        this._playingMusic = true;
        this._playingSFX = true;

        // video
        this._isFullscreen = true;
        this._resolution = new Point(0, 0);
    }

    public void Persist()
    {
        _persistence.storePreferences();
    }

}

public class HighScore
{
    private string _PlayerName;
    private string _Time;
    private string _TimePenalty;
    private string _TotalTime;
    private string _CollectedCrabs;
    private string _CollectedStarfishes;
    private int _levelID = -1;

    public string PlayerName { get { return _PlayerName; } set { _PlayerName = value; } }
    public string Time { get { return _Time; } set { _Time = value; } }
    public string TimePenalty { get { return _TimePenalty; } set { _TimePenalty = value; } }
    public string TotalTime { get { return _TotalTime; } set { _TotalTime = value; } }
    public string CollectedCrabs { get { return _CollectedCrabs; } set { _CollectedCrabs = value; } }
    public string CollectedStarfishes { get { return _CollectedStarfishes; } set { _CollectedStarfishes = value; } }
    public int LevelID { get { return _levelID; } set { _levelID = value; } }

    private int _rank = -1;
    public int Rank
    {
        get { return _rank; }
        set { _rank = value; }
    }


    public HighScore() { }

    public HighScore(string time, string timePenalty, string todalTime, string collectedCrabs)
    {
        Time = time;
        TimePenalty = timePenalty;
        TotalTime = todalTime;
        CollectedCrabs = collectedCrabs;
    }

    public HighScore(string playerName, string time, string timePenalty, string todalTime, string collectedCrabs)
    {
        PlayerName = playerName;
        Time = time;
        TimePenalty = timePenalty;
        TotalTime = todalTime;
        CollectedCrabs = collectedCrabs;
    }

    public bool Equals(HighScore other)
    {
        return (this.PlayerName == other.PlayerName
         && this.Time == other.Time
         && this.TimePenalty == other.TimePenalty
         && this.TotalTime == other.TotalTime
         && this.CollectedCrabs == other.CollectedCrabs
         && this.CollectedStarfishes == other.CollectedStarfishes);
    }
}