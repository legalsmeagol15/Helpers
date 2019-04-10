using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    /// <summary>Helper functions related to time.</summary>
    public static class Time
    {
        /// <summary>Rounds down to the nearest ticks count.</summary>    
        public static DateTime Trim(this DateTime date, long ticks) => new DateTime(date.Ticks - (date.Ticks % ticks), date.Kind);

        /// <summary>Rounds down to the nearest second.</summary>        
        public static DateTime TrimToSecond(this DateTime date) => Trim(date, TimeSpan.TicksPerSecond);
    }
}
