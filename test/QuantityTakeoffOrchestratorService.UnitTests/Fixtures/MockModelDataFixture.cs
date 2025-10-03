using System.Reflection;

namespace QuantityTakeoffOrchestratorService.UnitTests.Fixtures
{
    public class MockModelDataFixture
    {
        /// <summary>
        /// Gets the mock TrimBIM model bytes from the embedded resource
        /// </summary>
        public static byte[] GetMockModelBytes()
        {
            byte[] mockBytes;
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "QuantityTakeoffOrchestratorService.UnitTests.Asset.MockModelData.txt";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                var splitResult = result.Split(',');
                mockBytes = Array.ConvertAll(splitResult, s => Convert.ToByte(s.Trim()));
            }

            return mockBytes;
        }

        /// <summary>
        /// Ensures the mock model file is properly included as an embedded resource
        /// </summary>
        public static bool IsResourceAvailable()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "QuantityTakeoffOrchestratorService.UnitTests.Asset.MockModelData.txt";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                return stream != null;
            }
        }
    }
}