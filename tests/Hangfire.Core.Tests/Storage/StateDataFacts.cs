using Hangfire.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Hangfire.Core.Tests.Storage
{
    public class StateDataFacts
    {
        [Fact]
        public void DataSetterShouldAlsoSetResultWhenTheResultIsPresent()
        {
            var result = "{}";
            var data = new Dictionary<string, string> { { "Result", result } };
            Assert.Equal(result, new StateData { Data = data }.Result);
        }

        [Fact]
        public void DataSetterShouldNotSetResultWhenResultIsNotPresent()
        {
            var data = new Dictionary<string, string>();
            Assert.Equal(null, new StateData { Data = data }.Result);
        }

        [Fact]
        public void DataSetterShouldNotSetResultWhenDataIsNull()
        {
            Assert.Equal(null, new StateData { Data = null }.Result);
        }

        [Fact]
        public void ResultShouldBeNullWhenDataHasntBeenSetYet()
        {
            Assert.Equal(null, new StateData().Result);
        }
    }
}
