using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sol2E.Utils
{
    /// <summary>
    /// Helper to be able to use using blocks. Ensures that profiler.Stop() gehts called.
    /// Use it like this:
    /// 
    /// using (new ProfileContext(profilerInstance))
    /// {
    ///     DoSomethingAndMeasureExecutionTime();
    /// }
    /// </summary>
    public struct ProfileContext : IDisposable
    {
        private readonly Profiler _profiler;

        // start profiler on context creation
        public ProfileContext(Profiler profiler)
        {
            _profiler = profiler;
            profiler.Start();
        }

        // stop timer on context destruction
        public void Dispose()
        {
            _profiler.Stop();
        }
    }

    /// <summary>
    /// Profiler class, to measure execution time of user specified code.
    /// Multiple profiler instances can be used simultaniously. If used inside
    /// a game loop, call static method PeriodicalOutput(elapsedGameTime) every once
    /// in a while to print the results of all profilers currently running. 
    /// </summary>
    public class Profiler
    {
        #region Static Methods and Fields

        // collection of all profiler instances.
        private static readonly ICollection<Profiler> AllProfilers = new List<Profiler>();
        // need this to calculate some stats
        private static DateTime _timeSinceLastOutput = DateTime.Now;
        // used to allign output string in columns
        private const int MaxNameLength = 14;

        /// <summary>
        /// Returns a string containing all information about current profilers.
        /// </summary>
        /// <param name="averageInterval">average elapsed time per loop iteration</param>
        /// <returns>measurement results as string</returns>
        public static string PeriodicalOutput(double averageInterval)
        {
            // sum of actual working time of each profiler
            double totalWorkingTime = AllProfilers.Sum(profiler => profiler.WorkingTime);

            // time since last call to PeriodicalOutput
            double elapsedTime = (DateTime.Now - _timeSinceLastOutput).TotalMilliseconds;
            // number of calls to the profiled methods
            double estimatedCallRate = elapsedTime / averageInterval;

            // build result string and reset profilers
            var result = new StringBuilder();
            foreach (Profiler profiler in AllProfilers)
            {
                result.Append(profiler.ToString(elapsedTime, totalWorkingTime, estimatedCallRate));
                result.Append('\n');
                profiler.Reset();
            }

            _timeSinceLastOutput = DateTime.Now;
            return result.ToString();
        }

        #endregion

        #region Instance Fields

        // name of profiler, used for identification in output string
        public string Name { get; private set; }
        // time measured by the profiler
        public double WorkingTime { get; private set; }
        // Stopwatch (more accurate than System.DateTime)
        private Stopwatch _stopwatch;

        #endregion

        public Profiler(string name)
        {
            Name = name.Substring(0, Math.Min(MaxNameLength, name.Length));

            // register to list of profilers
            AllProfilers.Add(this);
        }

        #region Instace Methods

        /// <summary>
        /// Starts profiling
        /// </summary>
        public void Start()
        {
            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Stops profiling and adds measured time to total working time
        /// </summary>
        public void Stop()
        {
            WorkingTime += _stopwatch.Elapsed.TotalMilliseconds;
        }

        /// <summary>
        /// Resets profiler
        /// </summary>
        public void Reset()
        {
            WorkingTime = 0;
        }

        /// <summary>
        /// Returns a string which is composed of profiler name,
        /// measured time per frame, measured time per second,
        /// measured time relative to other profilers in percent
        /// </summary>
        /// <param name="elapsedTime">time since last call</param>
        /// <param name="totalWorkingTime">sum of measured time of all profilers</param>
        /// <param name="estimatedCallRate">estimated number of calls to the profiled methods</param>
        /// <returns>string with profiling information</returns>
        public string ToString(double elapsedTime, double totalWorkingTime, double estimatedCallRate)
        {
            return string.Format("{0}:", Name).PadRight(MaxNameLength + 1)
                + string.Format("{0:f2}ms/f", WorkingTime / estimatedCallRate).PadLeft(9)
                + string.Format("{0:f2}ms/s", WorkingTime * elapsedTime / 1000).PadLeft(10)
                + string.Format("({0:f2}%)", WorkingTime * 100 / totalWorkingTime).PadLeft(9);
        }

        /// <summary>
        /// Calls ToString and traces the result
        /// </summary>
        /// <param name="elapsedTime">time since last call</param>
        /// <param name="totalWorkingTime">sum of measured time of all profilers</param>
        /// <param name="estimatedCallRate">estimated number of calls to the profiled methods</param>
        public void Print(double elapsedTime, double totalWorkingTime, double estimatedCallRate)
        {
            Trace.WriteLine(ToString(elapsedTime, totalWorkingTime, estimatedCallRate));
        }

        #endregion
    }
}
