using Elements.Core;
using BabbleLinkMod;

namespace VRCFaceTracking.Baballonia;
/*
 * BabbleLink uses the Microsoft.Extensions.Logging ILogger interface, but Resonite uses its own logger (UniLog)
 * Instead of creating two separate log files (or doing it the proper way by installing Microsoft.Extensions.Logger,
 * which would require me to ship that DLL with the mod, which eeeh)
 * let us just create a fake ILogger that gives it the functions it needs and passes tehm .)
 */

public class ILogger {
    public string Prefix { get; set; }
    public void LogInformation(string info) { BabbleLinkMod.BabbleLinkMod.Msg(Prefix + info); }
    public void LogDebug(string info) { BabbleLinkMod.BabbleLinkMod.Debug(Prefix + info); }
    public void LogWarning(string info) { BabbleLinkMod.BabbleLinkMod.Warn(Prefix + info); }
}