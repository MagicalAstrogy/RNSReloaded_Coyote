using System.Drawing;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.CoyoteRabbit.Config;

namespace RNSReloaded.CoyoteRabbit;

public unsafe class Mod : IMod {
    private WeakReference<IRNSReloaded>? rnsReloadedRef;
    private WeakReference<IReloadedHooks>? hooksRef;
    private ILoggerV1 logger = null!;

    private Configurator configurator = null!;
    private Config.Config config = null!;

    private IHook<ScriptDelegate>? encounterHook;

    public void StartEx(IModLoaderV1 loader, IModConfigV1 modConfig) {
        this.rnsReloadedRef = loader.GetController<IRNSReloaded>()!;
        this.hooksRef = loader.GetController<IReloadedHooks>()!;
        this.logger = loader.GetLogger();

        if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)) {
            rnsReloaded.OnReady += this.Ready;
        }

        this.configurator = new Configurator(((IModLoader) loader).GetModConfigDirectory(modConfig.ModId));
        this.config = this.configurator.GetConfiguration<Config.Config>(0);
        this.config.ConfigurationUpdated += this.ConfigurationUpdated;
        CoyoteHttpClient.Init(this.config.BaseUrl, this.config.ClientId, this.logger);
        this.logger.PrintMessage("Inited.", Color.Red);
    }

    private void ConfigurationUpdated(IUpdatableConfigurable newConfig) {
        this.config = (Config.Config) newConfig;
    }

    public void Ready() {
        if (
            this.rnsReloadedRef != null
            && this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)
            && this.hooksRef != null
            && this.hooksRef.TryGetTarget(out var hooks)
        ) {
            rnsReloaded.LimitOnlinePlay();

            var id = rnsReloaded.ScriptFindId("scr_pattern_deal_damage_ally");
            var script = rnsReloaded.GetScriptData(id - 100000);

            this.encounterHook =
                hooks.CreateHook<ScriptDelegate>(this.EncounterDetour, script->Functions->Function);
            this.encounterHook.Activate();
            this.encounterHook.Enable();
            this.logger.PrintMessage("Hook enabled.", Color.Red);
        }
    }

    private RValue* EncounterDetour(
        CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
    ) {
        if (this.rnsReloadedRef != null && this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)) {
            //var encounterName = Enum.GetName(this.config.ForcedEncounter);
            //rnsReloaded.CreateString(argv[0], encounterName!);
        }
        //1st arg player id.

        returnValue = this.encounterHook!.OriginalFunction(self, other, returnValue, argc, argv);
        if (returnValue->Real != 0)
        {
            //实际受到了伤害
            CoyoteHttpClient.Fire(this.config.Strength, this.config.Duration);
            this.logger.PrintMessage("Damage sent.", Color.Red);
        }
        return returnValue;
    }

    public void Suspend() => this.encounterHook?.Disable();
    public void Resume() => this.encounterHook?.Enable();
    public bool CanSuspend() => true;

    public void Unload() { }
    public bool CanUnload() => false;

    public Action Disposing => () => { };
}
