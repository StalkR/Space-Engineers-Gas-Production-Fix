using HarmonyLib;
using NLog;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Reflection;
using VRage.Game;
using VRageMath;

namespace StalkR.GasProductionFix
{
    [HarmonyPatch(typeof(MyGasFueledPowerProducer), "UpdateCapacity")]
    internal class Patch_UpdateCapacity
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static int logs = 0;

        static MethodInfo disableUpdate;
        static MethodInfo updateDisplay;
        static void Prepare()
        {
            disableUpdate= AccessTools.Method(typeof(MyGasFueledPowerProducer), "DisableUpdate");
            updateDisplay = AccessTools.Method(typeof(MyGasFueledPowerProducer), "UpdateDisplay");
        }

        static bool Prefix(MyGasFueledPowerProducer __instance)
        {
            MyGasFueledPowerProducerDefinition blockDefinition = __instance.BlockDefinition;
            MyDefinitionId fuelId = blockDefinition.Fuel.FuelId;
            float currentOutput = __instance.SourceComp.CurrentOutput;
            float currentInput = __instance.SinkComp.CurrentInputByType(fuelId);
            float requiredInput = __instance.SinkComp.RequiredInputByType(fuelId);
            float gasUsed = __instance.SourceComp.CurrentOutput / blockDefinition.FuelProductionToCapacityMultiplier / 60f;
            float gasInput= __instance.SinkComp.CurrentInputByType(fuelId) / 60f / MyFueledPowerProducer.FUEL_CONSUMPTION_MULTIPLIER;
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
                updateDisplay.Invoke(__instance, null);
            }
            float fillingOffset = GetFillingOffset(__instance);
            if (!hasExtraGas && (noGasInputButWeNeed || (fillingOffset == 0f && gasUsed == 0f)))
            {
                disableUpdate.Invoke(__instance, null);
            }
            float requiredRate = gasUsed + fillingOffset * MyFueledPowerProducer.FUEL_CONSUMPTION_MULTIPLIER;
            __instance.SinkComp.SetRequiredInputByType(fuelId, requiredRate * 60f);
            __instance.CheckEmissiveState();
            SlowLog($"UpdateCapacity" +
                $" - fuelId={fuelId}" + // MyObjectBuilder_GasProperties/Hydrogen
                $" - currentInput={currentInput}" + // 125
                $" - requiredInput={requiredInput}" +
                $" - gasUsed={gasUsed}" + // 2.083333
                $" - gasInput={gasInput}" + // 2.083333
                $" - noGasInputButWeNeed={noGasInputButWeNeed}" + // False
                $" - extraGas={extraGas}" + // 0
                $" - capacity={capacity} now {__instance.Capacity}" + // 2.083334 now 2.083334
                $" - fillingOffset={fillingOffset}" + // 5000
                $" - requiredRate={requiredRate} and *60 = {requiredRate * 60}" + // 5002.083 and *60 = 300125
                $" - currentOutput={currentOutput} now {__instance.SourceComp.CurrentOutput}" + // 2.5 now 2.5
                $" - FuelProductionToCapacityMultiplier={blockDefinition.FuelProductionToCapacityMultiplier}" + // 0.02
                $" - FUEL_CONSUMPTION_MULTIPLIER={MyFueledPowerProducer.FUEL_CONSUMPTION_MULTIPLIER}"); // 1
            return false; // skip original method
        }

        static float GetFillingOffset(MyGasFueledPowerProducer __instance)
        {
            if (__instance.Enabled && __instance.IsFunctional)
            {
                float capacity = __instance.Capacity;
                float fuelCapacity = __instance.BlockDefinition.FuelCapacity;
                return MathHelper.Clamp(fuelCapacity - capacity, 0f, fuelCapacity / 20f);
            }
            return 0f;
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

        /*
        static bool Prefix(MyGasFueledPowerProducer __instance)
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
