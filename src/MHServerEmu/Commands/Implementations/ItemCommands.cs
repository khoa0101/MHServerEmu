using System.Diagnostics;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("item")]
    [CommandGroupDescription("Commands for managing items.")]
    public class ItemCommands : CommandGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        [Command("drop")]
        [CommandDescription("Creates and drops the specified item from the current avatar.")]
        [CommandUsage("item drop [pattern] [count]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Drop(string[] @params, NetClient client)
        {
            PrototypeId itemProtoRef = CommandHelper.FindPrototype(HardcodedBlueprints.Item, @params[0], client);
            if (itemProtoRef == PrototypeId.Invalid) return string.Empty;

            if (@params.Length == 1 || int.TryParse(@params[1], out int count) == false)
                count = 1;

            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;
            Avatar avatar = player.CurrentAvatar;

            LootManager lootManager = playerConnection.Game.LootManager;

            for (int i = 0; i < count; i++)
            {
                lootManager.SpawnItem(itemProtoRef, LootContext.Drop, player, avatar);
                Logger.Debug($"DropItem(): {itemProtoRef.GetName()} from {avatar}");
            }

            return string.Empty;
        }

        [Command("give")]
        [CommandDescription("Creates and gives the specified item to the current player.")]
        [CommandUsage("item give [pattern] [count]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Give(string[] @params, NetClient client)
        {
            PrototypeId itemProtoRef = CommandHelper.FindPrototype(HardcodedBlueprints.Item, @params[0], client);
            if (itemProtoRef == PrototypeId.Invalid) return string.Empty;

            if (@params.Length == 1 || int.TryParse(@params[1], out int count) == false)
                count = 1;

            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;

            LootManager lootGenerator = playerConnection.Game.LootManager;

            for (int i = 0; i < count; i++)
                lootGenerator.GiveItem(itemProtoRef, LootContext.Drop, player);
            Logger.Debug($"GiveItem(): {itemProtoRef.GetName()}[{count}] to {player}");

            return string.Empty;
        }

        [Command("destroyindestructible")]
        [CommandDescription("Destroys indestructible items contained in the player's general inventory.")]
        [CommandUsage("item destroyindestructible")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string DestroyIndestructible(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;
            Inventory general = player.GetInventory(InventoryConvenienceLabel.General);

            List<Item> indestructibleItemList = new();
            foreach (var entry in general)
            {
                Item item = player.Game.EntityManager.GetEntity<Item>(entry.Id);
                if (item == null) continue;

                if (item.ItemPrototype.CanBeDestroyed == false)
                    indestructibleItemList.Add(item);
            }

            foreach (Item item in indestructibleItemList)
                item.Destroy();

            return $"Destroyed {indestructibleItemList.Count} indestructible items.";
        }

        [Command("sort")]
        [CommandDescription("Sorts the player's general inventory.")]
        [CommandUsage("item sort [rarity|type|name]")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Sort(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;

            Inventory general = player.GetInventory(InventoryConvenienceLabel.General);
            if (general == null)
                return "Could not find your general inventory.";

            // Collect all items from the inventory
            List<Item> items = new();
            foreach (var entry in general)
            {
                Item item = player.Game.EntityManager.GetEntity<Item>(entry.Id);
                if (item == null) continue;
                items.Add(item);
            }

            if (items.Count == 0)
                return "Your inventory is empty.";

            // Parse optional sort key, default to rarity
            string sortBy = @params.Length > 0 ? @params[0].ToLowerInvariant() : "rarity";

            List<Item> sorted = sortBy switch
            {
                "type" => items
                    .OrderBy(x => x.ItemPrototype?.GetType().Name ?? string.Empty)
                    .ThenByDescending(x => GetRarityTier(x))
                    .ThenBy(x => GameDatabase.GetPrototypeName(x.PrototypeDataRef))
                    .ToList(),

                "name" => items
                    .OrderBy(x => GameDatabase.GetPrototypeName(x.PrototypeDataRef))
                    .ToList(),

                _ => // rarity (default)
                    items
                    .OrderByDescending(x => GetRarityTier(x))
                    .ThenBy(x => x.ItemPrototype?.GetType().Name ?? string.Empty)
                    .ThenBy(x => GameDatabase.GetPrototypeName(x.PrototypeDataRef))
                    .ToList()
            };

            int n = sorted.Count;
            ulong? stackEntityId = null;

            // Pass 1 — stage all items into high slot numbers to vacate slots 0..n-1
            // This avoids collisions when MoveEntityTo tries to swap items into occupied slots
            for (int i = 0; i < n; i++)
            {
                Inventory.ChangeEntityInventoryLocation(sorted[i], general, (uint)(n + i), ref stackEntityId, false);
                stackEntityId = null;
            }

            // Pass 2 — move each item into its final sorted slot
            for (int i = 0; i < n; i++)
            {
                Inventory.ChangeEntityInventoryLocation(sorted[i], general, (uint)i, ref stackEntityId, false);
                stackEntityId = null;
            }

            Logger.Debug($"Sort(): sorted {n} items by {sortBy} for {player}");

            return $"Sorted {n} items by {sortBy}.";
        }

        [Command("roll")]
        [CommandDescription("Rolls the specified loot table.")]
        [CommandUsage("item roll [pattern]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string RollLootTable(string[] @params, NetClient client)
        {
            PrototypeId lootTableProtoRef = CommandHelper.FindPrototype(HardcodedBlueprints.LootTable, @params[0], client);
            if (lootTableProtoRef == PrototypeId.Invalid) return string.Empty;

            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;

            player.Game.LootManager.TestLootTable(lootTableProtoRef, player);

            return $"Finished rolling {lootTableProtoRef.GetName()}, see the server console for results.";
        }

        [Command("rollall")]
        [CommandDescription("Rolls all loot tables.")]
        [CommandUsage("item rollall")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string RollAllLootTables(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;

            int numLootTables = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();

            foreach (PrototypeId lootTableProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<LootTablePrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                player.Game.LootManager.TestLootTable(lootTableProtoRef, player);
                numLootTables++;
            }

            stopwatch.Stop();

            return $"Finished rolling {numLootTables} loot tables in {stopwatch.Elapsed.TotalMilliseconds} ms, see the server console for results.";
        }

        [Command("creditchest")]
        [CommandDescription("Converts credits to a sellable chest item.")]
        [CommandUsage("item creditchest")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string CreditChest(string[] @params, NetClient client)
        {
            const PrototypeId CreditItemProtoRef = (PrototypeId)13983056721138685632;
            const int CreditItemPrice = 500000;

            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;

            var options = player.Game.CustomGameOptions;

            if (options.EnableCreditChestConversion == false)
                return "Credit chest conversion is disabled by server settings";

            PropertyId creditsProperty = new(PropertyEnum.Currency, GameDatabase.CurrencyGlobalsPrototype.Credits);
            int price = (int)(CreditItemPrice * options.CreditChestConversionMultiplier);

            if (price <= 0)
                return "Failed to calculate credit chest price.";

            if (player.Properties[creditsProperty] < price)
                return $"You need at least {price} credits to use this command.";

            player.Properties.AdjustProperty(-price, creditsProperty);
            player.Game.LootManager.GiveItem(CreditItemProtoRef, LootContext.CashShop, player);

            Logger.Trace($"CreditChest(): {player}");

            return $"Converted {price} credits to a Credit Chest.";
        }

        [Command("cleardeliverybox")]
        [CommandDescription("Destroys all items contained in the delivery box inventory.")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string ClearDeliveryBox(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;

            Inventory deliveryBox = player.GetInventory(InventoryConvenienceLabel.DeliveryBox);
            if (deliveryBox == null)
                return "Delivery box inventory not found.";

            int count = deliveryBox.Count;
            deliveryBox.DestroyContained();

            return $"Destroyed {count} items contained in the delivery box inventory.";
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static int GetRarityTier(Item item)
        {
            if (item?.ItemPrototype == null) return 0;
            PrototypeId rarityRef = item.ItemPrototype.Rarity;
            if (rarityRef == PrototypeId.Invalid) return 0;
            RarityPrototype rarityProto = GameDatabase.GetPrototype<RarityPrototype>(rarityRef);
            return rarityProto != null ? (int)rarityProto.Tier : 0;
        }
    }
}