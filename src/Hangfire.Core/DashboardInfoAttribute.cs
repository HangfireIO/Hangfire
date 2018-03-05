using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire
{
    /// <summary>
    /// Used to provide dashboard information for job methods.
    /// </summary>
    public class DashboardInfoAttribute : Attribute
    {
        #region Properties.
        /// <summary>
        /// Define the display name for the job on the dashboard. The method
        /// arguments will be used as the positional arguments for formatting.
        /// </summary>
        public string DisplayName { get; set; }
        #endregion

        #region Constructors.
        public DashboardInfoAttribute()
            : this(string.Empty)
        {
        }

        public DashboardInfoAttribute(string displayName)
        {
            DisplayName = displayName;
        }
        #endregion
    }
}
