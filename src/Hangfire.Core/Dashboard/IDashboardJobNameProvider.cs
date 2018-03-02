using Hangfire.Annotations;
using Hangfire.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Dashboard
{
    public interface IDashboardJobNameProvider
    {
        string GetName([NotNull] DashboardContext context, [NotNull] Job job);
    }
}
