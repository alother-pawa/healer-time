using System;
using System.Collections.Generic;
using Game.Contracts.Modules;
using Game.Contracts.Navigation;
using Game.Contracts.Save;
using Paowa.Core.Flow.Timing;
using Paowa.Core.Foundation.Bootstrap;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Foundation.Ticking;
using Paowa.Core.Messaging;
using Paowa.Pause;
using Paowa.Save;
using Paowa.UI;
using UnityEngine;
using CoreSaveSlots = Paowa.Save.SaveSlots;

namespace Game.Systems.SaveSlots
{
    /// <summary>
    /// Save slots of the game. Configures Core's <see cref="ISaveSlotService"/> from <see cref="SaveSlotsData"/> (data version,
    /// migration steps, checkpoints into the current slot), registers <see cref="IPlayMenu"/> (Play opens the slot panel),
    /// <see cref="IPlaytime"/> (ticked every frame) and <see cref="ICheckpoint"/>; the writer behind it hooks the save package
    /// events, so every slot save (also a direct <c>ISaveService.Save()</c>) writes the slot card and keeps the thumbnail in step. New game and Continue hide the panel and switch to the gameplay scene of the config.
    /// </summary>
    internal sealed class SaveSlotsModule : ICoreModule
    {
        private readonly string[] _dependsOn = { SaveModuleIds.Save, UIModuleIds.UI };

        public string Id => GameModuleIds.SaveSlots;

        public int Priority => GameModulePriorities.SaveSlots;

        public bool IsRequired => false;

        public IReadOnlyList<string> DependsOn => _dependsOn;

        public void Initialize()
        {
            var config = Resources.Load<SaveSlotsConfig>(SaveSlotsConfig.ResourcePath);
            if (config == null || config.Panel == null || config.Data == null || config.GameplayScene == null)
            {
                throw new InvalidOperationException(
                    $"SaveSlotsConfig with the panel, the data and the gameplay scene is missing at Resources/{SaveSlotsConfig.ResourcePath}. "
                    + "Run Game › Build All.");
            }

            SaveSlotsData data = config.Data;
            ValidateSlotIds(data.SlotIds);
            var save = CoreServiceLocator.Get<ISaveSlotService>();
            save.DataVersion = data.DataVersion;
            save.AutoSaveTarget = AutoSaveTarget.CurrentSlot;
            RegisterMigrators(save, data);

            CoreServiceLocator.TryGet(out IPauseService pause);
            CoreServiceLocator.TryGet(out ITimeScaleService timeScale);
            var playtime = new PlaytimeTracker(() => (pause != null && pause.IsPaused) || (timeScale != null && timeScale.IsPaused));
            if (CoreServiceLocator.TryGet(out IFrameDriver frames))
            {
                frames.Register(playtime);
            }
            else
            {
                Debug.LogWarning("[Game] Core's frame driver is not running; playtime does not count.");
            }

            var writer = new SlotSaveWriter(save, CoreServiceLocator.Get<IEventBus>(), playtime,
                () => CaptureThumbnailAsync(data.ThumbnailWidth, data.ThumbnailHeight, data.ThumbnailQuality));

            var ui = CoreServiceLocator.Get<IUIManager>();
            UIViewId panel = config.Panel.ViewId;
            ui.RegisterView(panel, config.Panel.gameObject);
            SceneKey gameplay = config.GameplayScene;
            var context = new SaveSlotPanelContext(save, writer, data, () =>
            {
                ui.HidePopup(panel);
                _ = EnterGameplayAsync(gameplay);
            });

            CoreServiceLocator.Register<IPlaytime>(playtime);
            CoreServiceLocator.Register<ICheckpoint>(writer);
            CoreServiceLocator.Register<IPlayMenu>(new SaveSlotPlayMenu(ui, panel, context));
        }

        private static void ValidateSlotIds(IReadOnlyList<string> slotIds)
        {
            if (slotIds == null || slotIds.Count == 0)
            {
                throw new InvalidOperationException("SaveSlotsData lists no slot ids.");
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string slotId in slotIds)
            {
                if (!CoreSaveSlots.IsValidId(slotId) || slotId == CoreSaveSlots.AutoSave || !seen.Add(slotId))
                {
                    throw new InvalidOperationException(
                        $"SaveSlotsData slot id '{slotId}' is invalid, reserved or listed twice. Use 1-{CoreSaveSlots.MaxIdLength} "
                        + "characters of a-z, 0-9, _ or -, never 'autosave'.");
                }
            }
        }

        // A null entry or two steps from one version is a data error: startup stops with the reason (Core throws on duplicates).
        private static void RegisterMigrators(ISaveSlotService save, SaveSlotsData data)
        {
            var from = new HashSet<int>();
            foreach (SaveMigrator migrator in data.Migrators)
            {
                if (migrator == null)
                {
                    throw new InvalidOperationException("SaveSlotsData lists an empty migrator entry.");
                }

                save.RegisterMigrator(migrator);
                from.Add(migrator.FromVersion);
            }

            for (int version = 0; version < data.DataVersion; version++)
            {
                if (!from.Contains(version))
                {
                    Debug.LogWarning($"[Game] No save migrator from data version {version}: slots written at it will not load "
                        + $"(data version is {data.DataVersion}).");
                }
            }
        }

        // End of frame: after rendering, before a pause UI opened by the caller after awaiting the checkpoint.
        private static async Awaitable<byte[]> CaptureThumbnailAsync(int width, int height, int quality)
        {
            if (!ThumbnailCapture.CanCapture)
            {
                return null;
            }

            await Awaitable.EndOfFrameAsync();
            return ThumbnailCapture.TryCaptureJpg(width, height, quality);
        }

        private static async Awaitable EnterGameplayAsync(SceneKey gameplay)
        {
            try
            {
                if (!CoreServiceLocator.TryGet(out IGameNavigator navigator))
                {
                    Debug.LogError("[Game] The scene flow module is not running; the gameplay scene cannot open.");
                    return;
                }

                if (!await navigator.GoToAsync(gameplay))
                {
                    Debug.LogWarning("[Game] Another scene switch is running; the gameplay scene did not open.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            CoreBootstrapper.Contribute(new SaveSlotsModule());
        }
    }
}
