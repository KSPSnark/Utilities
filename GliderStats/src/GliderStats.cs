using System;
using UnityEngine;

namespace GliderStats
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GliderStats : MonoBehaviour
    {
        private readonly TimeSpan STATUS_UPDATE_INTERVAL = TimeSpan.FromSeconds(0.25);
        private const double SAMPLING_INTERVAL_SECONDS = 0.25;
        private readonly int NUM_SAMPLES = (int)(Configuration.samplingWindowSeconds / SAMPLING_INTERVAL_SECONDS);


        private bool wasValidSituation;
        private bool wasTracking;
        private DateTime nextStatusUpdate;
        private double trackingStartUTC;
        private double nextSampleUTC;
        private MinMax minmaxGlideRatio;
        private MinMax minmaxDescentSpeed;
        private MinMax minmaxSpeed;
        private MinMaxHistory historyGlideRatio;
        private MinMaxHistory historyDescentSpeed;
        private MinMaxHistory historySpeed;

        public GliderStats()
        {
            minmaxGlideRatio = new MinMax();
            minmaxDescentSpeed = new MinMax();
            minmaxSpeed = new MinMax();
            historyGlideRatio = new MinMaxHistory(NUM_SAMPLES);
            historyDescentSpeed = new MinMaxHistory(NUM_SAMPLES);
            historySpeed = new MinMaxHistory(NUM_SAMPLES);
            ResetAll();
        }

        public void Start()
        {
            Logging.Log("Starting up");
            ResetAll();
        }

        /// <summary>
        /// Called on each frame. Evaluates status and displays appropriate information.
        /// </summary>
        public void LateUpdate()
        {
            // Can we display anytihng?
            if (!StatusDisplay.IsInitialized) return; // can't do anything
            if (!IsValidSituation)
            {
                StatusDisplay.Enabled = false;
                SetTrackingStatus(false);
                return;
            }
            StatusDisplay.Enabled = true;
            if (!wasValidSituation)
            {
                Logging.Log("Flying!");
                wasValidSituation = true;
            }

            // Make sure there's no forbidden control input. Display info, if there is.
            if (!CheckControlState())
            {
                SetTrackingStatus(false);
                return;
            }

            // We have to be descending, otherwise it doesn't count as a "glide".
            double verticalSpeed = FlightGlobals.ActiveVessel.verticalSpeed;
            if (verticalSpeed > -0.001)
            {
                StatusDisplay.Value = "Not descending";
                SetTrackingStatus(false);
                return;
            }
            double descentSpeed = Math.Abs(verticalSpeed);

            // Okay, looks like we're good to go!
            double utc = Planetarium.GetUniversalTime();
            SetTrackingStatus(true);

            double currentGlideRatio = FlightGlobals.ActiveVessel.horizontalSrfSpeed / descentSpeed;
            minmaxGlideRatio.Update(currentGlideRatio);
            minmaxDescentSpeed.Update(descentSpeed);
            minmaxSpeed.Update(FlightGlobals.ActiveVessel.srfSpeed);
            if (utc > nextSampleUTC)
            {
                nextSampleUTC = utc + SAMPLING_INTERVAL_SECONDS;

                historyGlideRatio.Record(minmaxGlideRatio);
                minmaxGlideRatio.Reset();

                historyDescentSpeed.Record(minmaxDescentSpeed);
                minmaxDescentSpeed.Reset();

                historySpeed.Record(minmaxSpeed);
                minmaxSpeed.Reset();
            }

            // Update the status display, but only if the appropriate interval has passed.
            // (We do this to avoid doing a string format on every single frame, which would
            // spam GC.)
            DateTime now = DateTime.Now;
            if (now > nextStatusUpdate)
            {
                nextStatusUpdate = now + STATUS_UPDATE_INTERVAL;
                double fraction = Math.Min(historyGlideRatio.FractionComplete, historyDescentSpeed.FractionComplete);
                if (fraction < 1.0)
                {
                    StatusDisplay.Value = string.Format("Gliding ({0:0}%)...", 100.0 * fraction);
                }
                else
                {
                    string controlLabel = FlightGlobals.ActiveVessel.ctrlState.isIdle ? " (free)" : " (controlled)";
                    double speedDelta = (historySpeed.Max / historySpeed.Min) - 1;
                    if (speedDelta > Configuration.stabilizationThreshold)
                    {
                        // The speed hasn't settled down yet.
                        StatusDisplay.Value = string.Format(
                            "Gliding{0}: stabilizing (±{1:0.00}%), ratio {2:0.000}, descent {3:0.000} m/s",
                            controlLabel,
                            speedDelta * 100.0,
                            historyGlideRatio.Min,
                            historyDescentSpeed.Max);
                    }
                    else
                    {
                        StatusDisplay.Value = string.Format(
                            "Gliding{0}: ratio {1:0.000}, descent {2:0.000} m/s",
                            controlLabel,
                            historyGlideRatio.Min,
                            historyDescentSpeed.Max);
                    }
                }
            }
        }

        void SetTrackingStatus(bool value)
        {
            if (value == wasTracking) return;
            if (value)
            {
                Logging.Log("Start tracking");
                trackingStartUTC = Planetarium.GetUniversalTime();
            }
            else
            {
                Logging.Log("Stop tracking");
                trackingStartUTC = double.NaN;
            }
            wasTracking = value;
            ResetFlightStats();
        }

        private void ResetAll()
        {
            wasValidSituation = false;
            wasTracking = false;
            trackingStartUTC = double.NaN;
            nextSampleUTC = double.NegativeInfinity;
            nextStatusUpdate = DateTime.MinValue;
            ResetFlightStats();
        }

        private void ResetFlightStats()
        {
            minmaxGlideRatio.Reset();
            minmaxDescentSpeed.Reset();
            minmaxSpeed.Reset();
            historyGlideRatio.Reset();
            historyDescentSpeed.Reset();
            historySpeed.Reset();
        }

        /// <summary>
        /// Gets whether the current vessel is in a valid situation for displaying glider stats.
        /// </summary>
        private bool IsValidSituation
        {
            get
            {
                // We have to have a vessel
                Vessel vessel = FlightGlobals.ActiveVessel;
                if (vessel == null) return false;

                // It has to be airborne
                return vessel.situation == Vessel.Situations.FLYING;
            }
        }

        /// <summary>
        /// Gets whether the vessel is in a completely neutral control state. This means no
        /// player input, but also no throttle, no wheel torque, no trim, etc.
        /// </summary>
        private bool CheckControlState()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            FlightCtrlState controlState = vessel.ctrlState;
            if (controlState.mainThrottle != 0)
            {
                StatusDisplay.Value = "Throttle > 0";
                return false;
            }
            if (!Configuration.allowTrim && HasTrim(controlState))
            {
                StatusDisplay.Value = "Trim detected";
                return false;
            }
            if (!Configuration.allowControlInput)
            {
                if (vessel.Autopilot.Enabled)
                {
                    StatusDisplay.Value = "SAS is active";
                    return false;
                }
                if (!controlState.isNeutral)
                {
                    StatusDisplay.Value = "Control input detected";
                    return false;
                }
            }

            // Looks like we're good to go!
            return true;
        }

        /// <summary>
        /// Is there any trim input?
        /// </summary>
        /// <param name="controlState"></param>
        /// <returns></returns>
        private static bool HasTrim(FlightCtrlState controlState)
        {
            return (controlState.rollTrim != 0) || (controlState.pitchTrim != 0) || (controlState.yawTrim != 0);
        }
    }
}
