using System.Collections.Generic;
using NUnit.Framework;

namespace ViaLink.Tests
{
    [TestFixture]
    public class EventPayloadTests
    {
        [Test]
        public void ToDictionary_이벤트_딕셔너리_변환()
        {
            var payload = new EventPayload("purchase",
                new Dictionary<string, object> { { "revenue", "29900" } },
                linkId: 1);

            var dict = payload.ToDictionary();
            Assert.AreEqual("purchase", dict["event_name"]);
            Assert.AreEqual(1, dict["link_id"]);
        }

        [Test]
        public void ToDictionary_LinkId_null일때_기본값_0()
        {
            var payload = new EventPayload("view");
            var dict = payload.ToDictionary();
            Assert.AreEqual(0, dict["link_id"]);
            Assert.AreEqual("view", dict["event_name"]);
        }

        [Test]
        public void ToBatchItem_배치_딕셔너리_변환()
        {
            var payload = new EventPayload("view");
            var batch = payload.ToBatchItem();
            Assert.AreEqual("view", batch["event_name"]);
            Assert.AreEqual(0, batch["link_id"]);
        }

        [Test]
        public void ToDictionary_EventData_null일때_빈_딕셔너리()
        {
            var payload = new EventPayload("test");
            var dict = payload.ToDictionary();
            Assert.IsInstanceOf<Dictionary<string, object>>(dict["event_data"]);
            Assert.AreEqual(0, ((Dictionary<string, object>)dict["event_data"]).Count);
        }

        [Test]
        public void Timestamp_자동_설정()
        {
            var payload = new EventPayload("test");
            Assert.Greater(payload.Timestamp, 0);
        }
    }
}
