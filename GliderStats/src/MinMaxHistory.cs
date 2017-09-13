namespace GliderStats
{
    /// <summary>
    /// Utility class for tracking how a number changes over time.  Keeps track
    /// of minimum/maximum values during a sliding window.
    /// </summary>
    class MinMaxHistory
    {
        // Circular arrays that hold our values.
        private readonly double[] minValues;
        private readonly double[] maxValues;

        // The number of values we have. Starts initially at zero. Increments by
        // one each time we record a value, until it rises to our array size,
        // then it stops growing (because at that point we've wrapped around
        // the circular array and we're overwriting a previous value).
        private int count;

        // The index of the last-recorded min/max. -1 when we have no data.
        private int currentIndex;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="size">The number of samples to keep track of.</param>
        public MinMaxHistory(int size)
        {
            minValues = new double[size];
            maxValues = new double[size];
            Reset();
        }

        /// <summary>
        /// Record a new sample.
        /// </summary>
        /// <param name="minmax"></param>
        public void Record(MinMax minmax)
        {
            if (!minmax.HasValue) return;

            if (count < minValues.Length)
            {
                // We haven't filled up yet.
                ++count;
                ++currentIndex;
            }
            else
            {
                // We're full, so we're in circular-wrapping mode.
                ++currentIndex;
                if (currentIndex >= minValues.Length) currentIndex = 0;
            }
            minValues[currentIndex] = minmax.Min;
            maxValues[currentIndex] = minmax.Max;
        }

        /// <summary>
        /// Reset the history to an empty state.
        /// </summary>
        public void Reset()
        {
            count = 0;
            currentIndex = -1;
            for (int i = 0; i < minValues.Length; ++i)
            {
                minValues[i] = double.NaN;
                maxValues[i] = double.NaN;
            }
        }

        /// <summary>
        /// Gets whether any data is available. If this is false, Min and Max will be NaN.
        /// </summary>
        public bool HasValues
        {
            get { return count > 0; }
        }

        /// <summary>
        /// Gets whether a "full" data set is available.
        /// </summary>
        public bool IsComplete
        {
            get { return count == minValues.Length; }
        }

        /// <summary>
        /// Returns a fraction from 0 to 1 indicating how close to full the history is.
        /// </summary>
        public double FractionComplete
        {
            get { return (double)count / (double)minValues.Length; }
        }

        /// <summary>
        /// Get the minimum value observed thus far.
        /// </summary>
        public double Min
        {
            get
            {
                if (count < 1) return double.NaN; // no data!
                double min = minValues[0];
                for (int i = 1; i < count; ++i)
                {
                    if (minValues[i] < min) min = minValues[i];
                }
                return min;
            }
        }

        /// <summary>
        /// Get the maximum value observed thus far.
        /// </summary>
        public double Max
        {
            get
            {
                if (count < 1) return double.NaN; // no data!
                double max = maxValues[0];
                for (int i = 1; i < count; ++i)
                {
                    if (maxValues[i] > max) max = maxValues[i];
                }
                return max;
            }
        }
    }
}
