namespace Game.Systems.Gameplay
{
    /// <summary>What the revive popup asks of the run that opened it (its payload).</summary>
    internal interface IRevivePopupHost
    {
        int ReviveCost { get; }

        long Coins { get; }

        /// <summary>Pays and revives; false when the coins were not enough (the popup stays).</summary>
        bool Revive();

        /// <summary>The popup closed without a revive (Give up, Cancel/Back).</summary>
        void GiveUp();
    }
}
