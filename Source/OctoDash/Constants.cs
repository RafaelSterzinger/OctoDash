

public static class Constants
{
    public const string MENU_SCREEN_NAME = "Menu";
    public const string DEMO_LVL_SCREEN_NAME_SUFFIX = "Demo";
    public const string LVL_SCREEN_BASE_NAME = "Level";
    public const string TUTORIAL_LVL_SCREEN_NAME = "Tutorial";
    public const string GAME_COMPLETE_SCREEN_NAME = "Game Complete Screen";

    public enum GameState
    {
        MainMenu,
        MainMenuSettings,
        MainMenuVideoSettings,
        MainMenuAudioSettings,
        LevelSelect,
        InGame,
        PauseMenu,
        PauseMenuSettings,
        PauseMenuVideoSettings,
        PauseMenuAudioSettings,
        GameCompleted,
        HighScoreState,
    }

    // game title
    public const string TitleButtonAssetString = "menu/game_title";
    public const string GameTitleFieldString = "game_title";

    // main menu
    public const string MainMenuOptionsListString = "MainMenuOptionList";
    public const string SettingsMenuOptionsListString = "SettingsMenuOptionList";
    public const string VideoMenuOptionsListString = "VideoMenuOptionList";
    public const string AudioMenuOptionsListString = "AudioMenuOptionList";
    public const string PlayString = "Play";
    public const string SettingsString = "Settings";
    public const string ExitString = "Exit";

    // setting menu
    public const string VideoString = "Video";
    public const string AudioString = "Audio";
    public const string BackString = "Back";

    // video menu
    public const string ResolutionString = "Resolution";
    public const string WindowModeString = "Screen Mode";
    public const string WindowModeDisplayString = "windowMode";
    public const string WindowModeToggleActionString = "windowModeToggleAction";
    public const string String720p = "1280x720";
    public const string String1080p = "1920x1080";
    public const string String1440p = "2560x1440";

    // music menu
    public const string MusicVolumeString = "Volume Music";
    public const string MusicMuteString = "Mute Music";
    public const string MusicVolDisplayString = "MusicVol %";
    public const string MusicVolDown = "MusicVolDown";
    public const string MusicVolUp = "MusicVolUp";
    public const string SFXVolumeString = "Volume SFX";
    public const string SFXMuteString = "Mute SFX";
    public const string SFXVolumeDisplayString = "SFXVol %";
    public const string SFXVolDown = "SFXVolDown";
    public const string SFXVolUp = "SFXVolUp";
    public const string ToggleMusicString = "Music";
    public const string MusicStateString = "Music State";
    public const string SoundFXStateString = "SoundFX State";
    public const string ToggleSoundFXString = "SFX";

    // level seletion
    public const string LevelSelectString = "LevelSelect";
    public const string LevelTutorialString = "Tutorial";
    public const string Level1String = "Crab Hunt";
    public const string Level2String = "Breaking Free";
    public const string Level3String = "Octopus Journey";
    public const string Level4String = "Sky Is The Limit";
    public static string getLevelString(int levelID)
    {
        switch (levelID)
        {
            case 0:
                return LevelTutorialString;
            case 1:
                return Level1String;
            case 2:
                return Level2String;
            case 3:
                return Level3String;
            case 4:
                return Level4String;
            default:
                throw new System.Exception("Unknown levelID passed to getLevelString!");
        }
    }
    public const string LevelTutorialStringWithLength = "Tutorial (Short)";
    public const string Level1StringWithLength = "Crab Hunt (Medium)";
    public const string Level2StringWithLength = "Breaking Free (Medium)";
    public const string Level3StringWithLength = "Octopus Journey (Long)";
    public const string Level4StringWithLength = "Sky Is The Limit (Long)";


    // pause menu
    public const string PauseMenuOptionsListString = "PauseMenuOptionList";
    public const string ResumeString = "Resume";
    public const string RestartString = "Restart";
    public const string ExitToMainMenuString = "Exit to Main Menu";


    // game complete
    public const string PlayerString = "player";
    public const string NameQueryString = "Enter your name and press enter:";
    public const string TimeString = "Time: ";
    public const string TimePenaltyString = "Penalties: ";
    public const string TotalTimeString = "Total Time: ";
    public const string CollectedCrabsString = "Crabs collected: ";
    public const string NumCrabsCollectedString = "NumCrabs";
    public const string CollectedStarfishesString = "Starfishes collected: ";
    public const string NumStarfishesCollectedString = "NumStars";
    public const string NextLevelString = "Next Level";


    // persistence, db collection names etc.
    public const string SaveFileBaseNameString = "autosave.db";
    public const string HighScoresCollectionNameString = "highscores";
    public const string PreferencesCollectionNameString = "preferences";

}
