using HarmonyLib;
using NLog;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace StalkR.GasProductionFix
{
    [HarmonyPatch(typeof(MyGasFueledPowerProducer), "UpdateCapacity")]
    internal class Patch_UpdateCapacity
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static int logs = 0;

        static bool Prefix(MyHydrogenEngine __instance)
        {
            MyGasFueledPowerProducerDefinition blockDefinition = __instance.BlockDefinition;
            MyDefinitionId fuelId = blockDefinition.Fuel.FuelId;
            float currentOutput = __instance.SourceComp.CurrentOutput;
            float currentInput = __instance.SinkComp.CurrentInputByType(fuelId);
            float gasUsed = __instance.SourceComp.CurrentOutput / blockDefinition.FuelProductionToCapacityMultiplier / 60f;
            float gasInputStart = __instance.SinkComp.CurrentInputByType(fuelId) / 60f / MyFueledPowerProducer.FUEL_CONSUMPTION_MULTIPLIER;
            float gasInput = gasInputStart;
            if (gasInput == 0f && __instance.IsCreativeModeEnabled)
            {
                gasInput = gasUsed + GetFillingOffset(__instance);
            }
            bool noGasInputButWeNeed = gasInput == 0f && __instance.SinkComp.RequiredInputByType(fuelId) > 0f;
            float extraGas = gasInput - gasUsed;
            bool hasExtraGas = extraGas != 0f;
            var capacity = __instance.Capacity;
            if (hasExtraGas)
            {
                if (Sync.IsServer)
                {
                    __instance.Capacity += extraGas;
                }
                //__instance.UpdateDisplay(); // not available but it does just 2 things:
                __instance.SetDetailedInfoDirty();
                __instance.RaisePropertiesChanged();
            }
            float fillingOffset = GetFillingOffset(__instance);
            if (!hasExtraGas && (noGasInputButWeNeed || (fillingOffset == 0f && gasUsed == 0f)))
            {
                //__instance.DisableUpdate(); // not available, we'll just update all the time
            }
            float requiredRate = gasUsed + fillingOffset * MyFueledPowerProducer.FUEL_CONSUMPTION_MULTIPLIER;
            float requiredInput = requiredRate * 60f;
            while (requiredInput > fillingOffset) requiredInput--; // gently lower if we happen to be above
            __instance.SinkComp.SetRequiredInputByType(fuelId, requiredInput);
            __instance.CheckEmissiveState();
            SlowLog($"UpdateCapacity" +
                $" - fuelId={fuelId}" + // MyObjectBuilder_GasProperties/Hydrogen
                $" - currentInput={currentInput}" + // 125
                $" - gasUsed={gasUsed}" + // 2.083333
                $" - gasInput={gasInputStart} now {gasInput}" + // 2.083333 now 2.083333
                $" - creative={__instance.IsCreativeModeEnabled}" + // False
                $" - noGasInputButWeNeed={noGasInputButWeNeed}" + // False
                $" - extraGas={extraGas}" + // 0
                $" - hasExtraGas={hasExtraGas}" + // False
                $" - isServer={Sync.IsServer}" + // True
                $" - capacity={capacity} now {__instance.Capacity}" + // 2.083334 now 2.083334
                $" - fillingOffset={fillingOffset}" + // 5000
                $" - requiredRate={requiredRate} and *60 = {requiredRate * 60} but lowered to {requiredInput}" + // 5002.083 and *60 = 300125 but lowered to 
                $" - currentOutput={currentOutput} now {__instance.SourceComp.CurrentOutput}" + // 2.5 now 2.5
                $" - FuelProductionToCapacityMultiplier={blockDefinition.FuelProductionToCapacityMultiplier}" + // 0.02
                $" - FUEL_CONSUMPTION_MULTIPLIER={MyFueledPowerProducer.FUEL_CONSUMPTION_MULTIPLIER}"); // 1
            return false; // skip original method
        }

        static DateTime lastLog = DateTime.MinValue;
        static readonly TimeSpan logEvery = new TimeSpan(0, 0, 1); // 1 second
        static void SlowLog(string msg)
        {
            if (lastLog.Add(logEvery) < DateTime.Now)
            {
                Log.Info(msg);
                lastLog = DateTime.Now;
            }
        }

        static float GetFillingOffset(MyHydrogenEngine __instance)
        {
            if (__instance.Enabled && __instance.IsFunctional)
            {
                float capacity = __instance.Capacity;
                float fuelCapacity = __instance.BlockDefinition.FuelCapacity;
                return MathHelper.Clamp(fuelCapacity - capacity, 0f, fuelCapacity / 20f);
            }
            return 0f;
        }

        /*
        static bool Prefix(MyHydrogenEngine __instance)
        {
            var fuelId = __instance.BlockDefinition.Fuel.FuelId;
            var current = __instance.SinkComp.CurrentInputByType(fuelId);
            var required = __instance.SinkComp.RequiredInputByType(fuelId);
            if (current < required)
            {
                if (logs < 50)
                {
                    Log.Info($"hydrogen engine entity id {__instance.EntityId} (grid name {__instance.CubeGrid.DisplayName}): current input {current} < {required} required input, skipping original method");
                    logs++;
                }
                // skip original method
                return false;
            }
            return true;
        }
        */
    }
}
