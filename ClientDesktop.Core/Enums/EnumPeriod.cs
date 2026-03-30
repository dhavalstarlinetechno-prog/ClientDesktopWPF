using System.ComponentModel;

namespace ClientDesktop.Core.Enums
{
    public enum EnumPeriod
    {
        [Description("Today")]
        Today = 1,

        [Description("Last 3 Days")]
        Last3Days = 2,

        [Description("Last Week")]
        LastWeek = 3,

        [Description("Last Month")]
        LastMonth = 4,

        [Description("Last 3 Months")]
        Last3Months = 5,

        [Description("Last 6 Months")]
        Last6Months = 6,

        [Description("All History")]
        AllHistory = 7
    }
}