using HarmonyLib;
using NLog;
using SpaceEngineers.Game.Entities.Blocks;

namespace StalkR.GasProductionFix
{
    [HarmonyPatch(typeof(MyGasFueledPowerProducer), "UpdateCapacity")]
    internal class Patch_UpdateCapacity
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static int logs = 0;

        static bool Prefix(MyHydrogenEngine __instance)
        {
            var fuelId = __instance.BlockDefinition.Fuel.FuelId;
            var current = __instance.SinkComp.CurrentInputByType(fuelId);
            var required = __instance.SinkComp.RequiredInputByType(fuelId);
            if (current < required)
            {
                if (logs < 50)
                {
                    Log.Info($"GasProductionFix: hydrogen engine entity id {__instance.EntityId} (grid name {__instance.CubeGrid.Name}): current input {current} < {required} required input, skipping original method");
                    logs++;
                }
                // skip original method
                return false;
            }
            return true;
        }
    }
}
