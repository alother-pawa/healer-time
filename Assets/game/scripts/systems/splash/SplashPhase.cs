namespace Game.Systems.Splash
{
    /// <summary>Steps of the splash, in order.</summary>
    internal enum SplashPhase
    {
        /// <summary>The logo fades in.</summary>
        FadeIn = 0,

        /// <summary>The logo stays fully visible.</summary>
        Hold = 1,

        /// <summary>The logo fades out.</summary>
        FadeOut = 2,

        /// <summary>The logo is gone; a loading indicator shows until the preload is done.</summary>
        WaitForPreload = 3,

        /// <summary>The splash is over; the next scene may load.</summary>
        Finished = 4
    }
}
