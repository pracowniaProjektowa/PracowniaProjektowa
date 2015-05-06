using System;
using FileHelpers;

namespace PracowniaProjektowa
{
    [DelimitedRecord(";")]
    [IgnoreEmptyLines()]
    [IgnoreFirst()]
    public class Node
    {
        private const float MAX_PERCENTAGE_DELTA = 10;
        private const float TEMEPERATURE_TZ_MAX = 150f;
        private const float TEMEPERATURE_TP_MAX = 100f;
        private const float TEMEPERATURE_TZ_MIN = 15f;
        private const float POWER_MAX = 1500;
        private const float POWER_MIN = 10;
        private const float FLOW_MAX = 15;
        private const float TOLERANCE = 0.01f;
        private const float cp = 4.1899f;
        public int Location { get; set; }
        public string Address { get; set; }
        public string Counter { get; set; }
        public int Hour { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public float Tz { get; set; }
        public float Tp { get; set; }
        public float PowerkW { get; set; }
        public float FlowM3h { get; set; }
        public float MaxPowerkW { get; set; }
        public float MaxFlowM3h { get; set; }

        public bool IsNodeComplete()
        {

            bool minMax = IsFLowCorrect() && IsTpCorrect() && IsTzCorrect() && IsPowerCorrect();
            if (minMax)
            {
                bool percentages = CountPercentageDeltaG() < MAX_PERCENTAGE_DELTA && CountPercentageDeltaQ() < MAX_PERCENTAGE_DELTA &&
                               CountPercentageDeltaTp() < MAX_PERCENTAGE_DELTA && CountPercentageDeltaTz() < MAX_PERCENTAGE_DELTA;
                return percentages;
            }
            return false;
        }

        public float CountDeltaQ()
        {
            return Math.Abs(PowerkW - FlowM3h * cp * (Tz - Tp) * 5f / 18f); // : 3,6
        }

        public float CountDeltaG()
        {
            return Math.Abs(FlowM3h - PowerkW * 3.6f / (cp * (Tz - Tp)));
        }

        public float CountDeltaTp()
        {
            return Math.Abs(-PowerkW * 3.6f / (FlowM3h * cp) + Tz - Tp);
        }

        public float CountDeltaTz()
        {
            return Math.Abs(PowerkW * 3.6f / (FlowM3h * cp) + Tp - Tz);
        }

        public float CountPercentageDeltaQ()
        {
            float expected = FlowM3h * cp * (Tz - Tp) * 5f / 18f;
            return Math.Abs(PowerkW - expected) / PowerkW; // : 3,6
        }

        public float CountPercentageDeltaG()
        {
            float expected = PowerkW * 3.6f / (cp * (Tz - Tp));
            return Math.Abs(FlowM3h - expected) / FlowM3h;
        }

        public float CountPercentageDeltaTp()
        {
            float expected = -PowerkW * 3.6f / (FlowM3h * cp) + Tz;
            return Math.Abs(expected - Tp) / Tp;
        }

        public float CountPercentageDeltaTz()
        {
            float expected = PowerkW * 3.6f / (FlowM3h * cp) + Tp;
            return Math.Abs(expected - Tz) / Tz;
        }

        public bool IsPowerCorrect()
        {
            return PowerkW < POWER_MAX && PowerkW > POWER_MIN;
        }

        public bool IsFLowCorrect()
        {
            return FlowM3h < FLOW_MAX && FlowM3h > TOLERANCE;
        }

        public bool IsTzCorrect()
        {
            return Tz < TEMEPERATURE_TZ_MAX && Tz > TEMEPERATURE_TZ_MIN && Tz > Tp;
        }

        public bool IsTpCorrect()
        {
            return Tp < TEMEPERATURE_TP_MAX && Tp > TOLERANCE && Tz > Tp;
        }

        public DateTime GetDate()
        {
            return new DateTime(Year, Month, Day, Hour, 0, 0);
        }
    }

}
