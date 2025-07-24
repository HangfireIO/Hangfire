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
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire
{
    public sealed class CaptureCultureAttribute : JobFilterAttribute, IClientFilter, IServerFilter
    {
        private readonly ILog _logger = LogProvider.GetLogger(typeof(CaptureCultureAttribute));

        public CaptureCultureAttribute() : this(null)
        {
        }

        public CaptureCultureAttribute([CanBeNull] string? defaultCultureName, bool captureDefault = true)
            : this(defaultCultureName, defaultCultureName, captureDefault)
        {
        }

        public CaptureCultureAttribute(
            [CanBeNull] string? defaultCultureName,
            [CanBeNull] string? defaultUICultureName,
            bool captureDefault = true)
        {
            DefaultCultureName = defaultCultureName;
            DefaultUICultureName = defaultUICultureName;
            CaptureDefault = captureDefault;

#if !NETSTANDARD1_3
            // For backward compatibility, the cached method does not respect user-overridden values.
            // https://blog.codeinside.eu/2018/05/28/cultureinfo-getculture-vs-new-cultureinfo/
            // https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.-ctor#system-globalization-cultureinfo-ctor(system-string)
            CachedCulture = false;
#endif
        }

        [CanBeNull]
        public string? DefaultCultureName { get; }

        [CanBeNull]
        public string? DefaultUICultureName { get; }

        public bool CaptureDefault { get; }

#if !NETSTANDARD1_3
        /// <summary>
        /// Gets or sets whether to use the <see cref="GetCultureInfo"/> method when getting
        /// a culture by its name, or create a <see cref="CultureInfo"/> instance using its
        /// constructor instead. Cached method does not respect user-overridden values associated
        /// with the current culture specified on the OS level.
        /// </summary>
        public bool CachedCulture { get; set; }
#endif

        public void OnCreating(CreatingContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var currentCulture = CultureInfo.CurrentCulture;
            var currentUICulture = CultureInfo.CurrentUICulture;

            if (CaptureDefault == false && currentCulture.Name.Equals(DefaultCultureName, StringComparison.Ordinal))
            {
                // Don't set the 'CurrentCulture' job parameter when it's equal to the default one
            }
            else
            {
                context.SetJobParameter("CurrentCulture", currentCulture.Name);
            }

            if (CaptureDefault == false && currentUICulture.Name.Equals(DefaultUICultureName, StringComparison.Ordinal))
            {
                // Don't set the 'CurrentUICulture' job parameter when it's equal to the default one
            }
            else if (GlobalConfiguration.HasCompatibilityLevel(CompatibilityLevel.Version_180) &&
                     currentUICulture.Equals(currentCulture))
            {
                // Don't set the 'CurrentUICulture' when it's the same as 'CurrentCulture' under
                // CompatibilityLevel.Version_180
            }
            else
            {
                context.SetJobParameter("CurrentUICulture", currentUICulture.Name);
            }
        }

        public void OnCreated(CreatedContext context)
        {
        }

        public void OnPerforming(PerformingContext context)
        {
            var cultureName = context.GetJobParameter<string>("CurrentCulture", allowStale: true);
            var uiCultureName = context.GetJobParameter<string>("CurrentUICulture", allowStale: true) ?? cultureName;

            cultureName = cultureName ?? DefaultCultureName;
            uiCultureName = uiCultureName ?? DefaultUICultureName;

            try
            {
                if (cultureName != null)
                {
                    context.Items["PreviousCulture"] = CultureInfo.CurrentCulture;
                    SetCurrentCulture(GetCultureInfo(cultureName));
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
                    SetCurrentUICulture(GetCultureInfo(uiCultureName));
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
                SetCurrentCulture((CultureInfo)culture!);
            }
            if (context.Items.TryGetValue("PreviousUICulture", out var uiCulture))
            {
                SetCurrentUICulture((CultureInfo)uiCulture!);
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
        private CultureInfo GetCultureInfo(string cultureName)
        {
#if !NETSTANDARD1_3
            if (CachedCulture)
            {
                return CultureInfo.GetCultureInfo(cultureName);
            }
#endif

            return new CultureInfo(cultureName);
        }
    }
}
