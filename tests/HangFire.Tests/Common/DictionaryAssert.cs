using System.Collections.Generic;
using HangFire.Core.Tests;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    public static class TableAssert
    {
        public static void ContainsFollowingItems(Table expected, IDictionary<string, string> actual)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var row in expected.Rows)
            {
                var name = row["Name"];
                var value = row["Value"];

                dictionary.Add(name, value);
            }

            DictionaryAssert.ContainsFollowingItems(dictionary, actual);
        }
    }
}
