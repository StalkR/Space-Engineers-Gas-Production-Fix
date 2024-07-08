using NLog;
using System.IO;
using Torch;
using Torch.API;
using Torch.API.Plugins;

namespace StalkR.HydrogenEngineFix
{
    public class HydrogenEngineFixPlugin : TorchPluginBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            Log.Info($"Hydrogen Engine Fix loaded");
        }
    }
}
