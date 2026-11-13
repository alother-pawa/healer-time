namespace Game.Systems.Gameplay
{
    /// <summary>What drives <c>Gameplay/Look</c>, which decides its units.</summary>
    internal enum LookSource
    {
        /// <summary>Mouse delta: pixels moved this frame.</summary>
        Mouse = 0,

        /// <summary>Right stick (or another controller axis): deflection −1..1, a rate.</summary>
        Stick = 1
    }
}
