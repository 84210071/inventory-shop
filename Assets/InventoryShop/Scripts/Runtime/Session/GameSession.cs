using System;
using System.IO;
using UnityEngine;

namespace InventoryShop
{
    public enum BusinessPhase { Trading, Settlement, Result }
    public enum ChallengeResult { Pending, Won, Lost }

    [Serializable]
    public sealed class ShopSave
    {
        [Serializable] public struct Stack { public string id; public int quantity; }
        [Serializable] public struct Order { public string customer; public string id; public int quantity; public long total; public OrderState state; }
        public int version, day, gold;
        public long purchases, sales, openingPurchases, openingSales;
        public BusinessPhase phase;
        public ChallengeResult result;
        public Stack[] backpack, shelf;
        public Order[] orders;
    }

    public static class GameSession
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath,
#if INVENTORYSHOP_SMOKE
            "inventory-shop-smoke.json");
#else
            "inventory-shop-v1.json");
#endif
        public static ShopEconomy Economy { get; private set; }
        public static bool HasSave => File.Exists(SavePath);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Economy = null;

        public static ShopEconomy GetOrStart(StartingStock initial)
            => Economy ??= new ShopEconomy(initial.Create());

        public static void NewGame() => Economy = null;

        public static bool TryRead(WholesaleCatalog catalog, CustomerRoster roster, out ShopEconomy restored)
        {
            restored = null;
            if (!HasSave) return false;
            try
            {
                restored = ShopEconomy.Restore(JsonUtility.FromJson<ShopSave>(File.ReadAllText(SavePath)), catalog, roster);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Save could not be read: " + error.Message);
                return false;
            }
        }

        public static void Resume(ShopEconomy economy) => Economy = economy;

        public static bool Save()
        {
            if (Economy?.CustomerDay == null) return false;
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temporary = SavePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(Economy.Capture(), true));
                if (File.Exists(SavePath)) File.Replace(temporary, SavePath, null);
                else File.Move(temporary, SavePath);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Save could not be written: " + error.Message);
                return false;
            }
        }
    }
}
