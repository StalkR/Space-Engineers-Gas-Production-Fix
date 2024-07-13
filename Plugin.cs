using HarmonyLib;
using NLog;
using Torch;
using Torch.API;

namespace StalkR.GasProductionFix
{
    public class Plugin : TorchPluginBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static Harmony harmony;

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            harmony = new Harmony(typeof(Plugin).Namespace);
            harmony.PatchAll();
            Log.Info($"Gas Production Fix loaded");
        }

        public override void Dispose()
        {
            harmony.UnpatchAll(typeof(Plugin).Namespace);
        }
    }
}
