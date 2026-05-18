using System.Collections.Generic;
using NUnit.Framework;

namespace ViaLink.Tests
{
    /// <summary>
    /// PaymentInitiatedArgs / PaymentInitiatedResult 모델 및
    /// 입력 검증 콜백 동작에 대한 단위 테스트.
    ///
    /// ViaLinkSDK.PaymentInitiated() 자체는 MonoBehaviour 인스턴스가 필요하므로
    /// Unity 외부에서는 호출할 수 없다. 따라서 모델/검증 경계에 한정해 테스트한다.
    /// </summary>
    [TestFixture]
    public class PaymentTests
    {
        [Test]
        public void PaymentInitiatedArgs_기본_생성()
        {
            var args = new PaymentInitiatedArgs
            {
                OrderId = "ORD-2026-0001",
                Amount = 19900d,
                Currency = "KRW"
            };
            Assert.AreEqual("ORD-2026-0001", args.OrderId);
            Assert.AreEqual(19900d, args.Amount);
            Assert.AreEqual("KRW", args.Currency);
            Assert.IsNull(args.LinkId);
            Assert.IsNull(args.PaymentMethod);
            Assert.IsNull(args.Metadata);
        }

        [Test]
        public void PaymentInitiatedArgs_옵션_필드_설정()
        {
            var args = new PaymentInitiatedArgs
            {
                OrderId = "ORD-1",
                Amount = 100d,
                Currency = "USD",
                LinkId = 42,
                PaymentMethod = "card",
                Metadata = new Dictionary<string, object>
                {
                    { "product_id", "P-1" },
                    { "coupon", "WELCOME10" }
                }
            };
            Assert.AreEqual(42, args.LinkId);
            Assert.AreEqual("card", args.PaymentMethod);
            Assert.AreEqual(2, args.Metadata.Count);
            Assert.AreEqual("P-1", args.Metadata["product_id"]);
        }

        [Test]
        public void PaymentInitiatedResult_필드_확인()
        {
            var result = new PaymentInitiatedResult
            {
                Success = true,
                PaymentEventId = "12345"
            };
            Assert.IsTrue(result.Success);
            Assert.AreEqual("12345", result.PaymentEventId);
        }

        [Test]
        public void PaymentInitiatedResult_기본값_확인()
        {
            var result = new PaymentInitiatedResult();
            Assert.IsFalse(result.Success);
            Assert.IsNull(result.PaymentEventId);
        }
    }
}
