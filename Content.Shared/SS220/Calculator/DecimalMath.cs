// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Shared.SS220.Calculator
{
    public static class DecimalMath
    {
        public static int CastToIntOrDefault(decimal n, int defaultValue = 0)
        {
            if (TryCastToInt(n, out var result))
                return result;
            return defaultValue;
        }

        public static bool TryCastToInt(decimal n, out int result)
        {
            try
            {
                result = (int)n;
                return true;
            }
            catch (Exception)
            {
                // The reason why this is like that is because
                // these casts to int can throw OverflowException
                // and it can not be handled via "unchecked" keyword
                // (special case for decimal type), and guess what?
                // OverflowException is prohibited for use, so
                // we catching all exceptions but whatever.
                result = 0;
                return false;
            }
        }

        public static int GetPowerOfTen(int n)
        {
            var r = 1;
            // Lets spin the processor for a bit, why the fuck not
            for (var i = 0; i < n; i++)
            {
                r *= 10;
            }
            return r;
        }

        public static int GetDecimalLength(int n)
        {
            if (n == 0) return 1;
            var i = 0;
            while (n > 0)
            {
                n /= 10;
                i++;
            }
            return i;
        }
    }
}
