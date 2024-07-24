using HarmonyLib;
using NLog;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using VRage.Game;
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
            __instance.SinkComp.SetRequiredInputByType(fuelId, requiredRate * 60f);
            __instance.CheckEmissiveState();
            SlowLog($"UpdateCapacity" +
                $" - fuelId={fuelId}" +
                $" - currentInput={currentInput}" +
                $" - gasUsed={gasUsed}" +
                $" - gasInput={gasInputStart} now {gasInput}" +
                $" - creative={__instance.IsCreativeModeEnabled}" +
                $" - noGasInputButWeNeed={noGasInputButWeNeed}" +
                $" - extraGas={extraGas}" +
                $" - hasExtraGas={hasExtraGas}" +
                $" - isServer={Sync.IsServer}" +
                $" - capacity={capacity} now {__instance.Capacity}" +
                $" - fillingOffset={fillingOffset}" +
                $" - requiredRate={requiredRate} and *60 = {requiredRate * 60}" +
                $" - currentOutput={currentOutput} now {__instance.SourceComp.CurrentOutput}" +
                $" - FuelProductionToCapacityMultiplier={blockDefinition.FuelProductionToCapacityMultiplier}" +
                $" - FUEL_CONSUMPTION_MULTIPLIER={MyFueledPowerProducer.FUEL_CONSUMPTION_MULTIPLIER}");
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
