using Paowa.Core.Persistence;
using Paowa.Save;
using UnityEngine;

namespace Game.Contracts.Save
{
    /// <summary>
    /// One step of game data migration kept as an asset, so the save slot module registers the game's steps from data (the
    /// list in its <c>SaveSlotsData</c>). A step reads data written at <see cref="FromVersion"/> and leaves it at
    /// <c>FromVersion + 1</c>; Core runs the steps in order on a copy of the slot data when a slot written at an older data
    /// version is loaded. Subclasses live in any game assembly.
    /// </summary>
    public abstract class SaveMigrator : ScriptableObject, ISaveMigrator
    {
        [Tooltip("Data version this step reads; it leaves the data at this version + 1.")]
        [Min(0)] [SerializeField] private int _fromVersion;

        /// <summary>Data version this step reads.</summary>
        public int FromVersion => _fromVersion;

        /// <summary>
        /// Upgrades slot data in place: read keys with their old types, write the new shape. Throwing aborts the load and
        /// leaves the slot untouched.
        /// </summary>
        public abstract void Migrate(ISaveStore data);
    }
}
