using System;
using Sol2E.Utils;

namespace Sol2E.Core
{
    /// <summary>
    /// Abstract implementation of IGraphicsSystem, to decorate it with the disposable pattern
    /// and a second profiler to measure the draw calls.
    /// </summary>
    public abstract class AbstractGraphicsSystem : AbstractDomainSystem, IGraphicsSystem
    {
        // draw calls per seconds
        public float FramesPerSecond { get; private set; }
        // profiler to watch the systems draw duration
        private readonly Profiler _drawProfiler;

        protected AbstractGraphicsSystem(string domainName)
            : base(domainName)
        {
            _drawProfiler = new Profiler(domainName + "Draw");
        }

        /// <summary>
        /// Calls protected Update method inside a ProfileContext.
        /// </summary>
        /// <param name="elapsedGameTime">elapsed game time</param>
        public void Draw(TimeSpan elapsedGameTime)
        {
            FramesPerSecond = 1f / (float)elapsedGameTime.TotalSeconds;

            using (new ProfileContext(_drawProfiler))
            {
                Draw((float)elapsedGameTime.TotalSeconds);
            }
        }

        /// <summary>
        /// Protected draw method. Do your drawing here.
        /// </summary>
        /// <param name="deltaTime">elapsed game time in total seconds</param>
        protected abstract void Draw(float deltaTime);
    }
}