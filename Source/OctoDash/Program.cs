using System;

namespace OctoDash
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                using (var game = new OctoDash())
                    game.Run();
            }
            catch (Exception e)
            {
                Log.Logger.Log(e.ToString());
            }
        }
    }
}
