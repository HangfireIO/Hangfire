using System;
using Hangfire.Dashboard;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Dashboard
{
    public class HtmlHelperFacts
    {
        private readonly Mock<RazorPage> _page;

        public HtmlHelperFacts()
        {
            _page = new Mock<RazorPage>();
        }

        [Fact]
        public void ToHumanDuration_FormatsFractionalSeconds()
        {
            var helper = CreateHelper();
            var result = helper.ToHumanDuration(TimeSpan.FromSeconds(1.087));
            Assert.Equal("+1.087s", result);
        }

        private HtmlHelper CreateHelper()
        {
            return new HtmlHelper(_page.Object);
        }
    }
}
