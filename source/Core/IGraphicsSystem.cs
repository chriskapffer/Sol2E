using System;

namespace Sol2E.Core
{
    /// <summary>
    /// Interface declaration of a domain system with rendering capabilities.
    /// For more explanation see IDomainSystem.
    /// </summary>
    interface IGraphicsSystem : IDomainSystem
    {
        float FramesPerSecond { get; }
        void Draw(TimeSpan elapsedGameTime);
    }
}
