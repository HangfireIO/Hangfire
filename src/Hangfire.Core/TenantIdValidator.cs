// This file is part of Hangfire.
// Copyright © 2026 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.

using System;
using Hangfire.Annotations;

namespace Hangfire
{
    public static class TenantIdValidator
    {
        public const int MaxLength = 100;

        public static void Validate([InvokerParameterName] string parameterName, [NotNull] string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(parameterName);
            }

            if (value.Length > MaxLength)
            {
                throw new ArgumentException($"Tenant id must be {MaxLength} characters or fewer.", parameterName);
            }

            foreach (var ch in value)
            {
                if (!((ch >= 'a' && ch <= 'z') ||
                      (ch >= '0' && ch <= '9') ||
                      ch == '_' ||
                      ch == '-' ||
                      ch == '.'))
                {
                    throw new ArgumentException(
                        $"Tenant id must consist of lowercase letters, digits, underscore, dash, and dot characters only. Given: '{value}'.",
                        parameterName);
                }
            }
        }
    }
}
