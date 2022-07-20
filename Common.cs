namespace TNHBGLoader
{
	public static class Common
	{
		/// <summary>
		/// Wraps an int to a minimum and maximum, rolling over on overflow/underflow.
		/// </summary>
		/// <returns>The wrapped integer.</returns>
		/// <param name="min">Minimum before wrap. Inclusive.</param>
		/// <param name="max">Maximum before wrap. Inclusive.</param>>
		public static int Wrap(this int self, int min, int max)
		{
			if (self < min)
				return max - (self + min + 1);
			if (self > max)
				return min + (self - max - 1);
			return self;
		}
		
		/// <summary>
		/// Wraps an int to a minimum and maximum, rolling over on overflow/underflow.
		/// </summary>
		/// <returns>The wrapped integer.</returns>
		/// <param name="range">A set of two numbers of minimum and maximum. Both inclusive.</param>
		public static int Wrap(this int self, int[] range)
		{
			return self.Wrap(range[0], range[1]);
		}
	}
}