using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Budget
{
    class Report
    {
        // Properties
        public List<Payment> Budget { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }

        // Show the average amount of money required per paycheck?
        public bool ShowAveragePerCheck { get; set; }

        // Generate all payments on a per-pay period basis (1st - 15th, 16th - end of month)
        public bool ByPayPeriod
        {
            get { return byPayPeriod; }
            set { byPayPeriod = value; }
        }

        // Generate all payments on a per-month basis
        public bool ByMonth
        {
            get { return !byPayPeriod; }
            set { byPayPeriod = !value; }
        }


        // Variables
        private StreamWriter sw;
        private bool byPayPeriod = true;   // Default to writing out weekly report


        // Constructors
        public Report()
        {
            // Default constructor does nothing
        }

        public Report(List<Payment> budget,
                      DateTime startDt,
                      DateTime endDt)
        {
            Budget = budget;
            StartDt = startDt;
            EndDt = endDt;
        }


        // Run the report
        public void Run()
        {
            var outputFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                                          $"budget {StartDt.ToString("yyyy-MM").ToLower()}.txt");

            using (sw = new StreamWriter(outputFile))
            {
                if (ShowAveragePerCheck)
                {
                    WriteAverage();
                }

                if (byPayPeriod)
                {
                    RunByPayPeriod();
                }
                else
                {
                    RunByMonth();
                }
            }
        }

        public void RunByPayPeriod()
        {
            // Two periods per month; 1 - 15, 1 - end of month
            double total = 0;
            var periodEnd = StartDt.AddDays(14);        // 1st + 14 days = 15th

            foreach (Payment payment in Budget)
            {
                // Skip any budget items that are beyond the end of the period for this report
                if (payment.Date > EndDt)
                {
                    continue;
                }

                if (payment.Date > periodEnd)
                {
                    WriteFooter(total);     // write out period footer
                    total = 0;              // reset total for next period

                    // TODO: write out period header

                    if (periodEnd.Day == 15)
                    {
                        // Need to get end of month, so we revert to the 1st, add a month, and subtract a day
                        periodEnd = periodEnd.AddDays(-14).AddMonths(1).AddDays(-1);
                    }
                    else
                    {
                        periodEnd = periodEnd.AddDays(14);
                    }
                }

                total += payment.Amount;
                WritePayment(payment);
            }

            WriteFooter(total);     // Write out final period footer
        }

        public void RunByMonth()
        {

        }

        // Writes out the specified payment
        public void WritePayment(Payment payment)
        {
            sw.WriteLine($"  {payment.Date:MM/dd/yy}" +
                         $"  {payment.Name.PadRight(30)}" +
                         $"  {payment.Amount,8:N2}");       // Number, 2 decimal places; padded left 8 spaces
        }

        // Writes out the specified total
        public void WriteFooter(double total)
        {
            var spaces = new string(' ', 44);
            var dashes = new string('-', 8);

            sw.WriteLine(spaces + dashes);
            sw.WriteLine($"{spaces}{total,8:N2}\n");    // Extra CrLf at end
        }

        // Writes the average for the budget
        public void WriteAverage()
        {
            // Calculate total amount in budget
            var total = Budget.Sum(p => p.Amount);

            var startDt = Budget.Min(p => p.Date);
            var endDt = Budget.Max(p => p.Date); //.AddDays(1);     // endDt is end of month so put it on the 1st to compare better
            var months = ((endDt.Year - startDt.Year) * 12) + endDt.Month - startDt.Month + 1;

            // If budgeting by pay period, double the number of periods to calculate the average per period
            if (byPayPeriod)
            {
                months = months * 2;
            }


            /* Calcaulte average based on 2 paychecks
               TODO: If I modify this program to support multiple months, I need to add a month counter here */
            var avg = Math.Truncate(total / months);

            /* If we're less than $5 over the nearest multiple of 25, round down. Otherwise
               round up to the nearest $25. Missing a few dollars here and there is offset by the rounding up done elsewhere,
               plus the fact that Property Taxes aren't paid during June & July */
            var mod = avg % 25;

            if (mod < 5)
            {
                avg = avg - mod;
            }
            else
            {
                avg = avg + 25 - mod;
            }

            //avg = avg + 25 - (avg % 25);

            sw.WriteLine($"  Per Paycheck: {avg:c}\n");
        }

        public void WritePayPeriodHdr(DateTime periodEnd)
        {
            var startDt = periodEnd.AddDays(1);
            DateTime endDt;

            if (startDt.Day == 1)
            {
                endDt = startDt.AddDays(14);
            }
        }

    }
}
