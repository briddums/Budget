using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Budget
{
    class Data
    {
        public enum Recurrence
        {
            Unknown,
            Weekly,
            Biweekly,
            Monthly
        }

        public int Month { get; private set; }
        public DateTime StartDt { get; private set; }
        public DateTime EndDt { get; private set; }

        private List<Payment> payments;
        private List<Payment> budget;

        public void Generate()
        {
            GetMonth();
            LoadPayments();
            GenerateBudget();

            var rpt = new Report(budget, StartDt, EndDt);
            rpt.ShowAveragePerCheck = true;
            rpt.Run();
        }

        // Choose month to generate budget for
        private void GetMonth()
        {
            // Default month is current month;
            //int month = DateTime.Now.AddMonths(1).Month;
            int month = DateTime.Now.Month;

            // Default month is current month
            //string defaultMonth = DateTime.Now.AddMonths(1).ToString("MMM");
            string defaultMonth = DateTime.Now.ToString("MMM");

            while (true)
            {
                Console.Clear();
                Console.WriteLine("Jan:  1");
                Console.WriteLine("Feb:  2");
                Console.WriteLine("Mar:  3");
                Console.WriteLine("Apr:  4");
                Console.WriteLine("May:  5");
                Console.WriteLine("Jun:  6");
                Console.WriteLine("Jul:  7");
                Console.WriteLine("Aug:  8");
                Console.WriteLine("Sep:  9");
                Console.WriteLine("Oct: 10");
                Console.WriteLine("Nov: 11");
                Console.WriteLine("Dec: 12");
                Console.WriteLine("");
                Console.Write($"Select month (CrLf = {defaultMonth}): ");

                var ch = Console.ReadLine();

                // Default month was set earlier
                if (string.IsNullOrWhiteSpace(ch))
                {
                    break;
                }

                // Verify that a month of 1..12 was entered
                if (int.TryParse(ch, out month))
                {
                    if (month >= 1 && month <= 12)
                    {
                        break;
                    }
                }

                // TODO: Consider allowing to enter in multiple months at once??
            }

            Month = month;

            int year = DateTime.Now.Year;

            // If running for the first quarter and we're in thelast quarter assume we want to run this for next year
            if (Month <= 3 && DateTime.Now.Month >= 10) year += 1;

            StartDt = new DateTime(year, month, 1);

            // TODO: let user choose end date so report can be run over multiple months
            EndDt = StartDt.AddMonths(1).AddDays(-1);
        }

        // Read payment information from XML file
        public void LoadPayments()
        {
            payments = new List<Payment>();

            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Payments.xml");
            bool addPayment = false;

            using (XmlTextReader reader = new XmlTextReader(path))
            {
                // Define variables needed
                string name = string.Empty;
                double amount = 0;
                Recurrence recurrence = Recurrence.Unknown; ;
                DateTime payStartDt = new DateTime();

                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == "Payment")
                            {
                                name = reader.GetAttribute("Name");
                                payStartDt = new DateTime();     // default with an early date
                                Enum.TryParse(reader.GetAttribute("Recurrence"), true, out recurrence);
                                addPayment = false;    // Assume we're not adding this payment
                            }

                            if (reader.Name == "Amount")
                            {
                                DateTime tempDt;
                                DateTime.TryParse(reader.GetAttribute("Date"), out tempDt);

                                // StartDt is when the report Starts, payStartDt is the date when that payment started
                                // EndDt is when the report Ends -> because some payments may start mid-month for a report
                                if (tempDt <= EndDt && tempDt > payStartDt)
                                {
                                    payStartDt = tempDt;
                                    double.TryParse(reader.GetAttribute("Amount"), out amount);

                                    addPayment = true; // This payment starts before the date range
                                }
                            }

                            break;

                        case XmlNodeType.EndElement:
                            if (reader.Name == "Payment" && addPayment)
                            {
                                payments.Add(new Payment
                                {
                                    Name = name,
                                    Amount = amount,
                                    Recurrence = recurrence,
                                    Date = payStartDt
                                });
                            }

                            break;
                    }
                }
            }
        }

        public void GenerateBudget()
        {
            budget = new List<Payment>();

            // Generate a years worth of data; this will make the Average function in the report more accurate
            var budgetEndDt = StartDt.AddYears(2).AddDays(-1);

            // Loop through each payment item
            foreach (Payment payment in payments)
            {
                // Increment the dates on this payment until we land in the current budget period
                while (payment.Date < StartDt)
                {
                    payment.NextDate();
                }

                // Add this payment to the budget, increment the date and repeat if required
                while (payment.Date <= budgetEndDt)
                {
                    /* Only add if amount is non-negative.  Paid bills will have an amount of 0
                       Need to add clones because classes are reference objects */
                    if (payment.Amount > 0) budget.Add(payment.Clone());
                    payment.NextDate();
                }
            }

            // Sort budget by Date by Name
            budget = budget.OrderBy(p => p.Date).ThenBy(p => p.Name).ToList();
        }
    }
}
