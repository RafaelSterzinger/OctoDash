
using System;


namespace Log
{

    public static class Logger
    {
        public static void Log(string s)
        {
            Console.WriteLine("[" + DateTime.Now + " "  + DateTime.Now.Millisecond + "ms" + "] " + s);
        }
    }

}