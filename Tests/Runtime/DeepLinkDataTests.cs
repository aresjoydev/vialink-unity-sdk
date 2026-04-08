using System.Collections.Generic;
using NUnit.Framework;

namespace ViaLink.Tests
{
    [TestFixture]
    public class DeepLinkDataTests
    {
        [Test]
        public void 생성_및_필드_확인()
        {
            var data = new DeepLinkData(
                "/product/123",
                new Dictionary<string, string>
                {
                    { "id", "123" },
                    { "promo", "SUMMER" }
                },
                "aB3xK"
            );

            Assert.AreEqual("/product/123", data.Path);
            Assert.AreEqual("aB3xK", data.ShortCode);
            Assert.AreEqual("123", data.Params["id"]);
            Assert.AreEqual("SUMMER", data.Params["promo"]);
        }

        [Test]
        public void 기본값_확인()
        {
            var data = new DeepLinkData("/home");
            Assert.AreEqual(0, data.Params.Count);
            Assert.IsNull(data.ShortCode);
        }

        [Test]
        public void 기본_생성자()
        {
            var data = new DeepLinkData();
            Assert.AreEqual("/", data.Path);
            Assert.IsNotNull(data.Params);
            Assert.IsNull(data.ShortCode);
        }

        [Test]
        public void ToString_형식()
        {
            var data = new DeepLinkData("/test", shortCode: "abc");
            string str = data.ToString();
            Assert.IsTrue(str.Contains("/test"));
            Assert.IsTrue(str.Contains("abc"));
        }
    }
}
