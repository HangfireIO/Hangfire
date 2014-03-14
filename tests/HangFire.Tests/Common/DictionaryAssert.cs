using System;
using System.Collections.Generic;
using HangFire.Common;
using TechTalk.SpecFlow;
using Xunit;

namespace HangFire.Tests
{
    public static class DictionaryAssert
    {
        public static void ContainsFollowingItems(Table expected, IDictionary<string, string> actual)
        {
            foreach (var row in expected.Rows)
            {
                var name = row["Name"];
                var value = row["Value"];

                Assert.True(actual.ContainsKey(name));
                if (value == "<UtcNow timestamp>")
                {
                    var timestamp = JobHelper.FromStringTimestamp(actual[name]);

                    Assert.True(
                        (timestamp > DateTime.UtcNow.AddSeconds(-1))
                        && (timestamp < DateTime.UtcNow.AddSeconds(1)));
                }
                else if (value == "<Tomorrow timestamp>")
                {
                    var timestamp = JobHelper.FromStringTimestamp(actual[name]);
                    Assert.True(
                        timestamp >= DateTime.UtcNow.Date.AddDays(1)
                        && timestamp < DateTime.UtcNow.Date.AddDays(2));
                }
                else if (value == "<Non-empty>")
                {
                    Assert.False(String.IsNullOrEmpty(actual[name]));
                }
                else if (value.StartsWith("<Assembly qualified name of "))
                {
                    var splitted = value.Split('\'');
                    Assert.Equal(Type.GetType(splitted[1]).AssemblyQualifiedName, actual[name]);
                }
                else
                {
                    Assert.Equal(value, actual[name]);
                }
            }
        }
    }
}
