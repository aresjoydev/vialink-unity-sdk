using System.Collections.Generic;
using NUnit.Framework;

namespace ViaLink.Tests
{
    [TestFixture]
    public class JsonHelperTests
    {
        [Test]
        public void ToJson_기본_딕셔너리()
        {
            var dict = new Dictionary<string, object>
            {
                { "name", "test" },
                { "count", 42 }
            };

            string json = JsonHelper.ToJson(dict);
            Assert.IsTrue(json.Contains("\"name\":\"test\""));
            Assert.IsTrue(json.Contains("\"count\":42"));
        }

        [Test]
        public void ToJson_boolean_값()
        {
            var dict = new Dictionary<string, object>
            {
                { "active", true },
                { "deleted", false }
            };

            string json = JsonHelper.ToJson(dict);
            Assert.IsTrue(json.Contains("\"active\":true"));
            Assert.IsTrue(json.Contains("\"deleted\":false"));
        }

        [Test]
        public void ToJson_null_값()
        {
            var dict = new Dictionary<string, object>
            {
                { "value", null }
            };

            string json = JsonHelper.ToJson(dict);
            Assert.IsTrue(json.Contains("\"value\":null"));
        }

        [Test]
        public void ParseJson_기본_파싱()
        {
            string json = "{\"name\":\"test\",\"count\":42}";
            var result = JsonHelper.ParseJson(json);

            Assert.AreEqual("test", result["name"]);
            Assert.AreEqual(42L, result["count"]);
        }

        [Test]
        public void ParseJson_boolean_파싱()
        {
            string json = "{\"matched\":true}";
            var result = JsonHelper.ParseJson(json);
            Assert.AreEqual(true, result["matched"]);
        }

        [Test]
        public void ParseJson_빈_문자열()
        {
            var result = JsonHelper.ParseJson("");
            Assert.AreEqual(0, result.Count);
        }
    }
}
