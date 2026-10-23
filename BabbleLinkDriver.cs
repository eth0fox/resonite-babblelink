using System.Reflection;
using Elements.Core;
using FrooxEngine;
using Rug.Osc;
using VRCFaceTracking.Baballonia;

namespace BabbleLinkMod;

// I don't want to have to implement parsing for all the fields manually again! so we hack what's already there
public class SacrificialBabbleOSCDriver : BabbleOSC_Driver {

    public override void RegisterInputs(InputInterface inputInterface) {
        throw new NotSupportedException("SacrificialBabbleOSCDriver.RegisterInput");
    }
    
    public void SetEyes(Eyes eyes) {
        typeof(BabbleOSC_Driver).GetField("eyes", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(this, eyes);
    }
    public void SetMouth(Mouth mouth) {
        typeof(BabbleOSC_Driver).GetField("mouth", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(this, mouth);
    }

    public void HandleUpdate(string parameter, float value) {
        UpdateData(new OscMessage(parameter, [ value ]));
    }
}

public class BabbleLinkDriver : IInputDriver, IDisposable {
    public int UpdateOrder => 100;
    
    private ILogger logger = new() { Prefix = "[BFRLink] " };
    private int babbleLinkPort;
    public int BabbleLinkPort => babbleLinkPort;
    private BabbleLink link;
    
    private bool HasConnection;
    private bool HasEyeTracking;
    private bool HasMouthTracking;
    
    private SacrificialBabbleOSCDriver boscDriver = new();
    private Mouth mouth;
    private Eyes eyes;

    public BabbleLinkDriver(int port = 6767) {
        babbleLinkPort = port;
    }
    


    private void SetupBabbleLink() {
        // As I don't think FrooxEngine supports dynamically unloading InputDrivers, we use port -1 as a special value
        // to disable the BabbleLink interface dynamically at runtime.
        if (babbleLinkPort < 0) return; 
        BabbleLinkMod.Msg($"Setting up Babble Link listener on port {babbleLinkPort}");
        link = new BabbleLink(logger, babbleLinkPort);
        
        // maybe TODO: but for now just always say we have eye & face tracking;
        link.Mode = () => (true, true, false);
        link.ValueReceived += boscDriver.HandleUpdate;
        link.SessionEnded += LinkOnSessionEnded;
        link.FeaturesReceived += LinkOnFeaturesReceived;
    }

    private void DisposeLink() {
        link?.Teardown();
        HasConnection = false;
        HasEyeTracking = false;
        HasMouthTracking = false;
        link = null;
    }

    private void LinkOnFeaturesReceived(bool eye, bool face) {
        if (HasConnection == false || HasEyeTracking != eye || HasMouthTracking != face)
            BabbleLinkMod.Msg($"BFR advertises: Eye: {eye} Face: {face}");
        HasConnection = true;
        HasEyeTracking = eye;
        HasMouthTracking = face;
    }

    private void LinkOnSessionEnded() {
        BabbleLinkMod.Warn("BFR session ended.");
        HasConnection = false;
        HasEyeTracking = false;
        HasMouthTracking = false;
    }

    public void ChangePort(int port) {
        if (port == babbleLinkPort) return;
        BabbleLinkMod.Msg($"Switching to port {port}");
        DisposeLink();
        babbleLinkPort = port;
        SetupBabbleLink();
    }

    public void RegisterInputs(InputInterface inputInterface) {
        SetupBabbleLink();
        // We can't call BabbleOSC_Driver.RegisterInputs as it will start the OSC listener
        mouth = new Mouth(inputInterface, "Babble Frame Runtime", new MouthParameterGroup[14]
        {
            MouthParameterGroup.JawPose,
            MouthParameterGroup.JawOpen,
            MouthParameterGroup.TonguePose,
            MouthParameterGroup.TongueRoll,
            MouthParameterGroup.LipRaise,
            MouthParameterGroup.LipHorizontal,
            MouthParameterGroup.SmileFrown,
            MouthParameterGroup.MouthDimple,
            MouthParameterGroup.MouthPout,
            MouthParameterGroup.LipOverturn,
            MouthParameterGroup.LipStretchTighten,
            MouthParameterGroup.LipsPress,
            MouthParameterGroup.CheekPuffSuck,
            MouthParameterGroup.NoseWrinkle
        });
        boscDriver.SetMouth(mouth);
        
        eyes = new Eyes(inputInterface, "Babble Frame Runtime", false);
        boscDriver.SetEyes(eyes);
    }
    public void UpdateInputs(float deltaTime) {
        boscDriver.UpdateInputs(deltaTime);
        if (!HasConnection) {
            eyes.IsTracking = false;
            mouth.IsTracking = false;
        }
        eyes.IsEyeTrackingActive = HasEyeTracking;
        mouth.IsDeviceActive = HasMouthTracking;
    }
    public void CollectDeviceInfos(DataTreeList list) {
        list.Add(new DataTreeDictionary() {
            { "Name", "Babble Link" },
            { "Type", "Lip Tracking" },
            { "Model", "Babble Frame Runtime" }
        });
        list.Add(new DataTreeDictionary() {
            { "Name", "Babble Link" },
            { "Type", "Eye Tracking" },
            { "Model", "Babble Frame Runtime" }
        });
    }
    
    public void Dispose() {
        BabbleLinkMod.Msg("👀See👀 you next time!");
        DisposeLink();
    }
}