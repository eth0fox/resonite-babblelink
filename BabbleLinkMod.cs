using System.Diagnostics;
using System.Reflection;
using Elements.Core;
using ResoniteModLoader;
using FrooxEngine;
using HarmonyLib;
using Renderite.Shared;

    
namespace BabbleLinkMod;



public class BabbleLinkMod : ResoniteMod {
    public override string Name => "BabbleFrameRuntime";
    public override string Author => "eth0fox";
    public override string Version => typeof(BabbleLinkMod).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
    public override string Link => "https://github.com/eth0fox/resonite-babblelink";
    private const string harmonyId = "net.eth0fox.BabbleLink";
    
    
    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<bool> CfgEnabled = new ("enabled", "Should the mod be enabled (restart required to fully disable)", () => true);
    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<bool> CfgBypassHeadsetCheck = new ("bypassHeadsetCheck", "Enable even when Steam Frame not detected", () => false);
    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<int> CfgPort = new ("port", "Port to listen on", () => 6767);

    private static ModConfiguration Config;

    protected static BabbleLinkDriver Driver;

    
    private bool IsInitialised = false;
    public override void OnEngineInit() {
        Msg("Initialising!");
        Harmony harmony = new Harmony(harmonyId);
        harmony.PatchAll();
        
        Config = GetConfiguration();
        Config.OnThisConfigurationChanged += (e) => HandleUpdate();
        Engine.Current.RunPostInit(HandleUpdate);
        
    }
    

    private static void HandleUpdate() {
        var shouldBeEnabled = CfgEnabled.Value && (CfgBypassHeadsetCheck.Value || HasDetectedRelevantHeadset);
        if (shouldBeEnabled) {
            if (Driver is null) {
                Driver = new BabbleLinkDriver();
                Engine.Current.InputInterface.RegisterInputDriver(Driver);
            } else {
                Driver.ChangePort(CfgPort.Value);
            }
        } else if (Driver is not null) {
            Driver.ChangePort(-1);
        }
    }
    
    

    /**
     * This is the same place where SteamLinkOSC_Driver eye/face tracking (and also ViveHandTrackingDriver) get registered
     * The primary Babble OSC driver is registered in engine InitializeInputDrivers, but by registering here we are able
     * to detect the active headset and only register if needed, instead of always claiming port 6767 no matter which
     * headset is currently in use.
     *
     * We can't detect headset properly outside this function because even though GeneralHeadset has HeadsetManufacturer
     * and HeadsetModel fields... they're never actually filled in.
     */
    private static bool HasDetectedRelevantHeadset = false;
    [HarmonyPatch(typeof(VR_Manager), "HandleHeadsetState")]
    class HandleHeadsetStatePatch {
        static void Prefix(ref VR_Manager __instance, HeadsetState state, float deltaTime) {
            try {
                // The real method uses _headset to ensure it doesn't try to register devices twice, let's do the same.
                var headsetField =
                    typeof(VR_Manager).GetField("_headset", BindingFlags.NonPublic | BindingFlags.Instance);
                if (headsetField == null) throw new Exception("Couldn't find _headset field");
                var _headset = (GeneralHeadset)headsetField.GetValue(__instance);
                if (_headset is not null) return;

                var isRelevantHeadset = (
                    // TODO: find a better way of identifying this
                    state.headsetModel == "oculus quest2"
                    && state.headsetManufacturer == "Oculus"
                    //&& state.connectionType == HeadsetConnection.WirelessGeneral
                );
                
                Msg($"headsetManufacturer:{state.headsetManufacturer} headsetModel:{state.headsetModel} connectionType:{state.connectionType}, isRelevantHeadset:{isRelevantHeadset}");
                
                if (isRelevantHeadset) {
                    HasDetectedRelevantHeadset = true;
                    HandleUpdate();
                }
            }
            catch (Exception e) {
                Error("Exception in HandleHeadsetState: " + e);
            }
        }
    }
}
