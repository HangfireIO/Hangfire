using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class CommonBinding 
    {
        [BeforeScenario]
        public void ClearGlobalFilters()
        {
            GlobalJobFilters.Filters.Clear();
        }
    }
}
