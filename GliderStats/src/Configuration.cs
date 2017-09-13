using KSP.IO;

namespace GliderStats
{
    static class Configuration
    {
        /// <summary>
        /// The length of the sliding time window during which glide ratio and
        /// descent speed are sampled. In other words:  you have to wait at least
        /// this many seconds after turning off SAS, killing throttle, hands-off
        /// the controls, etc. before you start seeing stats.
        /// </summary>
        public static readonly double samplingWindowSeconds;

        /// <summary>
        /// This is a threshold for determining "is the glide stable yet or not."
        /// It's a fraction, defined as (max speed / min speed) - 1.  The glide
        /// is considered stable when that fraction drops below this threshold.
        /// </summary>
        public static readonly double stabilizationThreshold;

        /// <summary>
        /// Determines whether having any trim set on the craft disqualifies it
        /// from "gliding".
        /// </summary>
        public static readonly bool allowTrim;

        /// <summary>
        /// Determines whether having control input (either from the player or
        /// SAS) disqualifies the craft from "gliding".
        /// </summary>
        public static readonly bool allowControlInput;

        static Configuration()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<GliderStats>();
            config.load();

            samplingWindowSeconds  = config.GetValue("SamplingWindowSeconds",  10.0);
            stabilizationThreshold = config.GetValue("StabilizationThreshold", 0.01);
            allowTrim              = config.GetValue("AllowTrim",              true);
            allowControlInput      = config.GetValue("AllowControlInput",      true);

            Logging.Log("Sampling window: " + samplingWindowSeconds + " seconds");
            Logging.Log("Stabilization threshold: " + 100.0 * stabilizationThreshold + "%");
            Logging.Log("Trim allowed: " + (allowTrim ? "yes" : "no"));
            Logging.Log("Control input allowed: " + (allowControlInput ? "yes" : "no"));

            config.save();
        }
    }
}
