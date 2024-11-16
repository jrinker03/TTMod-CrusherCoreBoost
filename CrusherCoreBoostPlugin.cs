using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using System.Collections.Generic;

namespace CrusherCoreBoost
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class CrusherCoreBoostPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.jrinker03.CrusherCoreBoost";
        private const string PluginName = "CrusherCoreBoost";
        private const string VersionString = "1.0.0";
        private const string UnlockDisplayName = "Core Boost (Crushing)";

        private static readonly Harmony Harmony = new Harmony(MyGUID);

        public static ConfigEntry<int> CoresToUnlock;
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        public static int UnlockId
        {
            get;
            private set;
        }

        public static CrusherCoreBoostPlugin Instance
        {
            get;
            private set;
        }

        internal ManualLogSource SharedLogger
        {
            get
            {
                return Logger;
            }
        }

        public CrusherCoreBoostPlugin()
        {
            Instance = this;
        }

        /// <summary>
        /// Initialise the configuration settings and patch methods
        /// </summary>
        private void Awake()
        {
            // Apply all of our patches
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();

            CreateConfigEntries();
            CreateUnlock();

            EMU.Events.GameDefinesLoaded += OnGameDefinesLoaded;

            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log = Logger;
        }

        private void CreateConfigEntries()
        {
            CoresToUnlock = Config.Bind("General", "Cores To Unlock", 250, new ConfigDescription("How many yellow cores required to unlock 'Core Boost (Crushing)' in Tech Tree."));
        }

        private void CreateUnlock()
        {
            // Add the new unlock to Sierra T8 [same as Core Boost (Assembly)] and above Core Boost (Threshing)
            EMUAdditions.AddNewUnlock(new NewUnlockDetails()
            {
                displayName = UnlockDisplayName,
                description = "Increase speed of all Crushers by 0.01% per Core Cluster.",
                category = Unlock.TechCategory.Science,
                requiredTier = TechTreeState.ResearchTier.Tier25,
                coreTypeNeeded = ResearchCoreDefinition.CoreType.Gold,
                coreCountNeeded = CoresToUnlock.Value,
                dependencyNames = new System.Collections.Generic.List<string>() { EMU.Names.Unlocks.CoreBoostThreshing },
            });
        }

        private void OnGameDefinesLoaded()
        {
            Unlock coreBoostCrusher = EMU.Unlocks.GetUnlockByName(UnlockDisplayName);
            coreBoostCrusher.requiredTier = EMU.Unlocks.GetUnlockByName(EMU.Names.Unlocks.CoreBoostAssembly).requiredTier;
            coreBoostCrusher.treePosition = (int)EMU.Unlocks.GetUnlockByName(EMU.Names.Unlocks.CoreBoostThreshing).treePosition;
            coreBoostCrusher.sprite = EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.Crusher).rawSprite;

            Unlock unlock = EMU.Unlocks.GetUnlockByName(UnlockDisplayName);
            UnlockId = unlock.uniqueId;
        }

#if DEBUG
        internal void LogIl(IEnumerable<CodeInstruction> instructions)
        {
            Logger.LogInfo("***** Logging IL START *****");
            foreach (CodeInstruction instruction in instructions)
            {
                Logger.LogInfo(instruction.ToString());
            }
            Logger.LogInfo("***** Logging IL END *****");
        }
#endif
    }
}