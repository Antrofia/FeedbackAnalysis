
namespace FeedbackAnalysis.WBService
{
    public static class MathUtilities
    {
        public static double Remap(double x, double inStart, double inEnd, double outStart, double outEnd)
        {
            return outStart + (x - inStart) * (outEnd - outStart) / (inEnd - inStart);
        }
    }
}
