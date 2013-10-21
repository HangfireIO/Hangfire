using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

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

                Assert.IsTrue(actual.ContainsKey(name));
                if (value == "<UtcNow timestamp>")
                {
                    var timestamp = JobHelper.FromStringTimestamp(actual[name]);

                    Assert.IsTrue(
                        (timestamp > DateTime.UtcNow.AddSeconds(-1))
                        && (timestamp < DateTime.UtcNow.AddSeconds(1)));
                }
                else if (value == "<Tomorrow timestamp>")
                {
                    var timestamp = JobHelper.FromStringTimestamp(actual[name]);
                    Assert.IsTrue(
                        timestamp >= DateTime.UtcNow.Date.AddDays(1)
                        && timestamp < DateTime.UtcNow.Date.AddDays(2));
                }
                else if (value == "<Non-empty>")
                {
                    Assert.IsFalse(String.IsNullOrEmpty(actual[name]));
                }
                else
                {
                    Assert.AreEqual(value, actual[name]);
                }
            }
        }
    }
}
