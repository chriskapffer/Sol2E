using System;

namespace $safeprojectname$
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (GameTemplate1 game = new GameTemplate1())
            {
                game.Run();
            }
        }
    }
#endif
}

