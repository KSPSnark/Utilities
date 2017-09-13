namespace GliderStats
{
    /// <summary>
    /// Simple class that remembers the minimum and maximum from a set of provided sample values.
    /// </summary>
    class MinMax
    {
        private double min;
        private double max;

        public MinMax()
        {
            Reset();
        }

        /// <summary>
        /// Gets whether we have any minimum or maximum value to work with.
        /// If this returns false, the min and max are NaN.
        /// </summary>
        public bool HasValue
        {
            get { return !double.IsNaN(min); }
        }

        /// <summary>
        /// Update with the provided value.
        /// </summary>
        /// <param name="value"></param>
        public void Update(double value)
        {
            if (double.IsNaN(value)) return;
            if (double.IsNaN(min) || (value < min)) min = value;
            if (double.IsNaN(max) || (value > max)) max = value;
        }

        /// <summary>
        /// Resets to empty values.
        /// </summary>
        public void Reset()
        {
            min = double.NaN;
            max = double.NaN;
        }

        /// <summary>
        /// Get the minimum value received thus far, or NaN if no values have been provided.
        /// </summary>
        public double Min
        {
            get { return min; }
        }

        /// <summary>
        /// Get the maxim value received thus far, or NaN if no values have been provided.
        /// </summary>
        public double Max
        {
            get { return max; }
        }
    }
}
