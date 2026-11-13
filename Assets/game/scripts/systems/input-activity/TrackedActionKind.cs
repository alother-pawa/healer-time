namespace Game.Systems.InputActivity
{
    /// <summary>How a tracked action is read from the input service.</summary>
    internal enum TrackedActionKind
    {
        /// <summary>A button or trigger: pressed state plus its axis value (<c>IsPressed</c>, <c>ReadAxis</c>).</summary>
        Button = 0,

        /// <summary>A two-dimensional value (stick, WASD composite, mouse delta): pressed past the threshold (<c>ReadVector2</c>).</summary>
        Vector = 1
    }
}
