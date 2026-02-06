using ClosedXML.Excel;
using ClosedXML.Graphic.SixLabors;
using NUnit.Framework;

namespace ClosedXML.Tests
{
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void Setup()
        {
            // Use SixLabors graphic engine for all tests to ensure accurate metrics
            // matching what was used previously.
            LoadOptions.DefaultGraphicEngine = SixLaborsGraphicEngine.Instance.Value;
        }
    }
}
