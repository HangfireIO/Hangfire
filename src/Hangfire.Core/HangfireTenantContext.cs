// This file is part of Hangfire.
// Copyright © 2026 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.

using System;
using System.Threading;
using Hangfire.Annotations;

namespace Hangfire
{
    public static class HangfireTenantContext
    {
        private static readonly AsyncLocal<string> Current = new AsyncLocal<string>();

        public static string CurrentTenantId => Current.Value;

        public static IDisposable Use([NotNull] string tenantId)
        {
            TenantIdValidator.Validate(nameof(tenantId), tenantId);
            var previous = Current.Value;
            Current.Value = tenantId;
            return new Scope(previous);
        }

        private sealed class Scope : IDisposable
        {
            private readonly string _previous;
            private bool _disposed;

            public Scope(string previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                Current.Value = _previous;
            }
        }
    }
}
