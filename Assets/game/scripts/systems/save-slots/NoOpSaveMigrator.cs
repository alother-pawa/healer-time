using Game.Contracts.Save;
using Paowa.Core.Persistence;
using Paowa.Save;
using UnityEngine;

namespace Game.Systems.SaveSlots
{
    /// <summary>
    /// A migration step that changes nothing: for a data version that only adds keys, which older slots simply lack (every
    /// read falls back to its default). It is also the pattern of a real step: derive from <see cref="SaveMigrator"/>, read
    /// keys with their old types in <see cref="Migrate"/>, write the new shape, and list the asset in <c>SaveSlotsData</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Save/No-op Migrator", fileName = "SaveMigrator_NoOp")]
    internal sealed class NoOpSaveMigrator : SaveMigrator
    {
        public override void Migrate(ISaveStore data)
        {
        }
    }
}
