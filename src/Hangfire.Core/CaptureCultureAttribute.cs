// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Globalization;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire
{
    public sealed class CaptureCultureAttribute : JobFilterAttribute, IClientFilter, IServerFilter
    {
        private readonly ILog _logger = LogProvider.GetLogger(typeof(CaptureCultureAttribute));

        public void OnCreating(CreatingContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            context.SetJobParameter("CurrentCulture", CultureInfo.CurrentCulture.Name);
            context.SetJobParameter("CurrentUICulture", CultureInfo.CurrentUICulture.Name);
        }

        public void OnCreated(CreatedContext context)
        {
        }

        public void OnPerforming(PerformingContext context)
        {
            var cultureName = context.GetJobParameter<string>("CurrentCulture", allowStale: true);
            var uiCultureName = context.GetJobParameter<string>("CurrentUICulture", allowStale: true) ?? cultureName;

            try
            {
                if (cultureName != null)
                {
                    context.Items["PreviousCulture"] = CultureInfo.CurrentCulture;
                    SetCurrentCulture(new CultureInfo(cultureName));
                }
            }
            catch (CultureNotFoundException ex)
            {
                // TODO: Make this overridable, and start with throwing an exception
                _logger.WarnException($"Unable to set CurrentCulture for job {context.BackgroundJob.Id} due to an exception", ex);
            }

            try
            {
                if (uiCultureName != null)
                {
                    context.Items["PreviousUICulture"] = CultureInfo.CurrentUICulture;
                    SetCurrentUICulture(new CultureInfo(uiCultureName));
                }
            }
            catch (CultureNotFoundException ex)
            {
                // TODO: Make this overridable, and start with throwing an exception
                _logger.WarnException($"Unable to set CurrentUICulture for job {context.BackgroundJob.Id} due to an exception", ex);
            }
        }

        public void OnPerformed(PerformedContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Items.TryGetValue("PreviousCulture", out var culture))
            {
                SetCurrentCulture((CultureInfo)culture);
            }
            if (context.Items.TryGetValue("PreviousUICulture", out var uiCulture))
            {
                SetCurrentUICulture((CultureInfo)uiCulture);
            }
        }
        
        private static void SetCurrentCulture(CultureInfo value)
        {
#if !NETSTANDARD1_3
            System.Threading.Thread.CurrentThread.CurrentCulture = value;
#else
            CultureInfo.CurrentCulture = value;
#endif
        }

        // ReSharper disable once InconsistentNaming
        private static void SetCurrentUICulture(CultureInfo value)
        {
#if !NETSTANDARD1_3
            System.Threading.Thread.CurrentThread.CurrentUICulture = value;
#else
            CultureInfo.CurrentUICulture = value;
#endif
        }
    }
}
