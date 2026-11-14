using System;
using System.Linq;
using Game.Contracts.Roster;
using Paowa.Economy;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Editor.Builders
{
    /// <summary>
    /// The game's data: coins, items and their restock products (economy package), gem kinds, monster kinds, heroes,
    /// healers, the balance config and the roster. References (sprites, animators, lists) are written on every run; the
    /// numbers are written only when an asset is created, so tuning done in the Inspector survives a rebuild. Texts are
    /// localization keys.
    /// </summary>
    internal static class GameDataBuilder
    {
        private const string Economy = BuildPaths.Data + "/economy";
        private const string Gameplay = BuildPaths.Data + "/gameplay";
        private const string Roster = BuildPaths.Data + "/roster";
        private const long StartingCoins = 100;
        private const int StartingItems = 2;

        /// <summary>Gem kinds in board order: id, sprite, effect, power, colour.</summary>
        private static readonly (string id, string sprite, string effect, float power, string colour)[] Gems =
        {
            ("attack", "screens/gameplay/gem-attack", "Attack", 5f, "#FF6B5B"),
            ("heal", "screens/gameplay/gem-heal", "Heal", 3f, "#6BE07A"),
            ("defense", "screens/gameplay/gem-defense", "Defense", 4f, "#6FA8FF"),
            ("magic", "screens/gameplay/gem-magic", "Magic", 8f, "#C59BFF"),
        };

        /// <summary>Monster kinds: id, tint, scale, health, damage, attack interval, coins, score, boss, unlock index, weight.</summary>
        private static readonly (string id, string tint, float scale, float health, float damage, float interval, int coins, int score,
            bool boss, int unlockAt, float weight)[] Monsters =
        {
            ("goblin", "#FFFFFF", 1f, 30f, 6f, 1.8f, 3, 100, false, 0, 3f),
            ("mushroom", "#FFFFFF", 1f, 40f, 7f, 2.2f, 4, 120, false, 2, 2f),
            ("skeleton", "#FFFFFF", 1f, 55f, 9f, 2.4f, 5, 150, false, 4, 2f),
            ("flying-eye", "#FFFFFF", 1f, 25f, 5f, 1.2f, 4, 130, false, 6, 2f),
            ("fire-worm", "#FFFFFF", 1f, 45f, 10f, 2f, 6, 170, false, 8, 2f),
            ("fallen-king", "#FFFFFF", 1f, 140f, 14f, 2.2f, 25, 600, true, 0, 1f),
        };

        /// <summary>Items in slot order: id, icon, effect, amount, radius, restock price in coins.</summary>
        private static readonly (string id, string icon, string effect, float amount, int radius, int price)[] Items =
        {
            ("potion", "screens/gameplay/item-potion", "Heal", 40f, 0, 30),
            ("bomb", "screens/gameplay/item-bomb", "Bomb", 0f, 1, 40),
            ("shuffle", "screens/gameplay/item-shuffle", "Shuffle", 0f, 0, 25),
            ("armor", "screens/gameplay/item-armor", "Armor", 40f, 0, 30),
        };

        /// <summary>Heroes: id, sheet, tint, health, attack damage, attack interval, strike multiplier.</summary>
        private static readonly (string id, string sheet, string tint, float health, float damage, float interval, float strike)[] Heroes =
        {
            ("martial-hero", "martial-hero", "#FFFFFF", 120f, 8f, 1.1f, 1f),
            ("fantasy-warrior", "fantasy-warrior", "#FFFFFF", 140f, 7f, 1.2f, 0.9f),
            ("hero-knight", "hero-knight", "#FFFFFF", 160f, 6f, 1.3f, 1f),
            ("huntress", "huntress", "#FFFFFF", 100f, 9f, 0.9f, 1.3f),
        };

        /// <summary>Healers: id, sheet, tint, magic capacity, skill damage, skill heal.</summary>
        private static readonly (string id, string sheet, string tint, float capacity, float damage, float heal)[] Healers =
        {
            ("evil-wizard-2", "evil-wizard-2", "#FFFFFF", 100f, 50f, 10f),
            ("evil-wizard-3", "evil-wizard-3", "#FFFFFF", 80f, 35f, 20f),
        };

        internal static void Build()
        {
            var coin = BuildUtility.Asset<CurrencyDefinition>(Economy + "/Currency_Coin.asset");
            Fields.Set(coin, ("_id", "coin"), ("_displayName", "Coins"), ("_icon", BuildUtility.Sprite("ui-common/coin-icon")));

            Object[] items = BuildItems(coin);
            Object[] gems = Gems.Select(BuildGem).ToArray();
            Object[] monsters = Monsters.Select(BuildMonster).ToArray();

            string balancePath = Gameplay + "/BalanceConfig.asset";
            ScriptableObject balance = BuildUtility.Asset(GameTypes.BalanceConfig, balancePath);
            Fields.Set(balance, ("_gems", gems), ("_monsters", monsters), ("_items", items), ("_coin", coin));

            HeroDefinition[] heroes = Heroes.Select(BuildHero).ToArray();
            HealerDefinition[] healers = Healers.Select(BuildHealer).ToArray();
            Fields.Set(BuildUtility.Asset(GameTypes.RosterConfig, BuildPaths.RosterConfig), ("_heroes", heroes), ("_healers", healers));
            AssetDatabase.SaveAssets();
        }

        /// <summary>The balance config (built by <see cref="Build"/>).</summary>
        internal static ScriptableObject Balance => BuildUtility.Load<ScriptableObject>(Gameplay + "/BalanceConfig.asset");

        private static Object[] BuildItems(CurrencyDefinition coin)
        {
            var items = new Object[Items.Length];
            var products = new Object[Items.Length];
            for (int i = 0; i < Items.Length; i++)
            {
                (string id, string icon, string effect, float amount, int radius, int price) = Items[i];
                string itemPath = $"{Economy}/Item_{id}.asset";
                bool created = Missing(itemPath);
                ScriptableObject item = BuildUtility.Asset(GameTypes.ItemDefinition, itemPath);
                Fields.Set(item, ("_id", id), ("_displayName", id), ("_icon", BuildUtility.Sprite(icon)),
                    ("_effect", new EnumName(effect)), ("_nameKey", $"item.{id}.name"));
                if (created)
                {
                    Fields.Set(item, ("_amount", amount), ("_radius", radius));
                }

                var reward = BuildUtility.Asset<InventoryItemReward>($"{Economy}/Reward_{id}.asset");
                Fields.Set(reward, ("_item", item), ("_count", 1));
                string productPath = $"{Economy}/Product_{id}.asset";
                bool newProduct = Missing(productPath);
                var product = BuildUtility.Asset<ShopProduct>(productPath);
                Fields.Set(product, ("_id", $"item-{id}"), ("_displayName", id), ("_category", "items"), ("_icon", BuildUtility.Sprite(icon)),
                    ("_price._kind", new EnumName(nameof(PriceKind.Currency))), ("_price._currency", coin), ("_rewards", new Object[] { reward }));
                if (newProduct)
                {
                    Fields.Set(product, ("_price._amount", price));
                }

                Fields.Set(item, ("_restock", product));
                items[i] = item;
                products[i] = product;
            }

            Fields.Set(BuildUtility.Asset<ShopCatalog>(BuildPaths.ShopCatalog), ("_products", products));
            string settingsPath = BuildPaths.EconomySettings;
            if (Missing(settingsPath))
            {
                ScriptableObject settings = BuildUtility.Asset<EconomySettings>(settingsPath);
                Fields.SetList(settings, "_startingBalances", 1, (element, _) =>
                {
                    Fields.Relative(element, "_currency", coin);
                    Fields.Relative(element, "_amount", (int)StartingCoins);
                });
                Fields.SetList(settings, "_startingItems", items.Length, (element, i) =>
                {
                    Fields.Relative(element, "_item", items[i]);
                    Fields.Relative(element, "_count", StartingItems);
                });
            }

            return items;
        }

        private static Object BuildGem((string id, string sprite, string effect, float power, string colour) gem)
        {
            string path = $"{Gameplay}/gems/Gem_{gem.id}.asset";
            bool created = Missing(path);
            ScriptableObject asset = BuildUtility.Asset(GameTypes.GemDefinition, path);
            Fields.Set(asset, ("_id", gem.id), ("_nameKey", $"gem.{gem.id}.name"), ("_showcaseKey", $"gameplay.showcase.{gem.id}"),
                ("_sprite", BuildUtility.Sprite(gem.sprite)), ("_color", BuildUtility.Hex(gem.colour)), ("_effect", new EnumName(gem.effect)));
            if (created)
            {
                Fields.Set(asset, ("_power", gem.power), ("_spawnWeight", 1f));
            }

            return asset;
        }

        private static Object BuildMonster((string id, string tint, float scale, float health, float damage, float interval, int coins,
            int score, bool boss, int unlockAt, float weight) monster)
        {
            string path = $"{Gameplay}/monsters/Monster_{monster.id}.asset";
            bool created = Missing(path);
            ScriptableObject asset = BuildUtility.Asset(GameTypes.MonsterDefinition, path);
            Fields.Set(asset, ("_id", monster.id), ("_nameKey", $"monster.{monster.id}.name"), ("_controller", Controller(monster.id)),
                ("_height", SheetHeight(monster.id)), ("_isBoss", monster.boss));
            if (created)
            {
                Fields.Set(asset, ("_tint", BuildUtility.Hex(monster.tint)), ("_scale", monster.scale), ("_health", monster.health),
                    ("_damage", monster.damage), ("_attackInterval", monster.interval), ("_coins", monster.coins), ("_score", monster.score),
                    ("_unlockAt", monster.unlockAt), ("_weight", monster.weight));
            }

            return asset;
        }

        private static HeroDefinition BuildHero((string id, string sheet, string tint, float health, float damage, float interval,
            float strike) hero)
        {
            string path = $"{Roster}/Hero_{hero.id}.asset";
            bool created = Missing(path);
            var asset = BuildUtility.Asset<HeroDefinition>(path);
            Character(asset, "hero", hero.id, hero.sheet);
            if (created)
            {
                Fields.Set(asset, ("_tint", BuildUtility.Hex(hero.tint)), ("_maxHealth", hero.health), ("_attackDamage", hero.damage),
                    ("_attackInterval", hero.interval), ("_strikeMultiplier", hero.strike));
            }

            return asset;
        }

        private static HealerDefinition BuildHealer((string id, string sheet, string tint, float capacity, float damage, float heal) healer)
        {
            string path = $"{Roster}/Healer_{healer.id}.asset";
            bool created = Missing(path);
            var asset = BuildUtility.Asset<HealerDefinition>(path);
            Character(asset, "healer", healer.id, healer.sheet);
            if (created)
            {
                Fields.Set(asset, ("_tint", BuildUtility.Hex(healer.tint)), ("_magicCapacity", healer.capacity),
                    ("_skillDamage", healer.damage), ("_skillHeal", healer.heal));
            }

            return asset;
        }

        // Id, texts, the sheet's animator, its idle frames for menu previews and its idle height.
        private static void Character(CharacterDefinition asset, string kind, string id, string sheet)
        {
            Fields.Set(asset, ("_id", id), ("_nameKey", $"{kind}.{id}.name"), ("_descriptionKey", $"{kind}.{id}.description"),
                ("_controller", Controller(sheet)), ("_previewFrames", IdleFrames(sheet)), ("_height", SheetHeight(sheet)));
        }

        private static RuntimeAnimatorController Controller(string sheet)
        {
            return BuildUtility.Load<RuntimeAnimatorController>($"{BuildPaths.Animations}/{sheet}/{sheet}.controller");
        }

        // The sheet's idle sprites in frame order (<sheet>_idle_<n>).
        private static Object[] IdleFrames(string sheet)
        {
            string prefix = sheet + "_idle_";
            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath($"{BuildPaths.Sprites}/{SheetPath(sheet)}.png").OfType<Sprite>()
                .Where(sprite => sprite.name.StartsWith(prefix, StringComparison.Ordinal))
                .OrderBy(sprite => int.Parse(sprite.name.Substring(prefix.Length)))
                .ToArray();
            if (frames.Length == 0)
            {
                throw new InvalidOperationException($"Sheet {sheet} has no idle frames; run the project settings step first.");
            }

            return frames.Cast<Object>().ToArray();
        }

        private static string SheetPath(string sheet)
        {
            return ArtSpec.Load().sheets.Single(spec => spec.Name == sheet).path;
        }

        private static float SheetHeight(string sheet)
        {
            float height = ArtSpec.Load().sheets.Single(spec => spec.Name == sheet).height;
            return height > 0f ? height : throw new InvalidOperationException($"Sheet {sheet} has no height in the art spec.");
        }

        private static bool Missing(string path)
        {
            return AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) == null;
        }
    }
}
