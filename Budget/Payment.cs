using System;

namespace Budget
{
    class Payment
    {


        // Properties
        public string          Name        { get;  set; }
        public double          Amount      { get;  set; }
        public Data.Recurrence Recurrence  { get;  set; }
        public DateTime        Date        { get;  set; }

        // Constructor to set properties
        public Payment()
        {

        }

        public Payment (string          name,
                        double          amt,
                        Data.Recurrence recur,
                        DateTime        date)
        {
            Name       = name;
            Amount     = amt;
            Recurrence = recur;
            Date       = date;
        }

        public void NextDate()
        {
            switch (Recurrence)
            {
                case Data.Recurrence.Weekly:
                    Date = Date.AddDays(7);
                    break;

                case Data.Recurrence.Biweekly:
                    Date = Date.AddDays(14);
                    break;

                case Data.Recurrence.Monthly:
                    Date = Date.AddMonths(1);
                    break;
            }
        }

        public Payment Clone()
        {
            return (Payment)MemberwiseClone();
        }
    }
}
