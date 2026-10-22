using System.Diagnostics;
using System.Reflection;
using Elements.Core;
using ResoniteModLoader;
using FrooxEngine;
using HarmonyLib;
using Renderite.Shared;

    
namespace BabbleLinkMod;



public class BabbleLinkMod : ResoniteMod {
    internal const string VERSION_CONSTANT = "1.0.0"; 
    public override string Name => "BabbleFrameRuntime";
    public override string Author => "eth0fox";
    public override string Version => VERSION_CONSTANT;
    public override string Link => "https://github.com/eth0fox/resonite-babblelink";
    private const string harmonyId = "net.eth0fox.BabbleLink";
    public override void OnEngineInit() {
        Msg("Initialising!");
        Harmony harmony = new Harmony(harmonyId);
        harmony.PatchAll();
    }
    
    /**
     * This is the same place where SteamLinkOSC_Driver eye/face tracking (and also ViveHandTrackingDriver) get registered
     * The primary Babble OSC driver is registered in engine InitializeInputDrivers, but by registering here we are able
     * to detect the active headset and only register if needed, instead of always claiming port 6767 no matter which
     * headset is currently in use.
     *
     * TODO: There's chance we'd want to enable this on other headsets (or like a Frame 2 or something idfk), but it's
     * not feasible too get to the ForceBabble launch option from here. I could have alternatively patched
     * InitializeInputDrivers but then that would have been moot because the check is to see which headset we're using
     * and I don't think you can see the headset information at that stage. (not sure)
     */
    [HarmonyPatch(typeof(VR_Manager), "HandleHeadsetState")]
    class InitializeInputDriversPatch {
        
        static void Prefix(ref VR_Manager __instance, HeadsetState state, float deltaTime) {
            var headsetField = typeof(VR_Manager).GetField("_headset", BindingFlags.NonPublic | BindingFlags.Instance);
            if (headsetField == null) {
                Error("Couldn't find headset field?");
                return;
            }
            var _headset = (GeneralHeadset)headsetField.GetValue(__instance);
            if (_headset is null) {
                Msg($"Registering BabbleLink input driver, headsetManufacturer:{state.headsetManufacturer} headsetModel:{state.headsetModel} connectionType:{state.connectionType} ");
                if ( // TODO: find a better way of identifying this
                    state.headsetModel == "oculus quest2"
                    && state.headsetManufacturer == "Oculus" 
                    //&& state.connectionType == HeadsetConnection.WirelessGeneral
                )
                    __instance.Input.RegisterInputDriver(new BabbleLinkDriver());
            }
        }
    }
}
