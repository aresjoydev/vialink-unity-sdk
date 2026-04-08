using NUnit.Framework;

namespace ViaLink.Tests
{
    [TestFixture]
    public class DeepLinkHandlerTests
    {
        private DeepLinkHandler _handler;

        [SetUp]
        public void SetUp()
        {
            // NetworkClient와 coroutineRunner 없이 ParseURL만 테스트
            _handler = new DeepLinkHandler(null, null);
        }

        [Test]
        public void ParseURL_정상_URL에서_ShortCode_추출()
        {
            string code = _handler.ParseURL("https://example.com/c/xYz12");
            Assert.AreEqual("xYz12", code);
        }

        [Test]
        public void ParseURL_잘못된_경로()
        {
            string code = _handler.ParseURL("https://example.com/other/path");
            Assert.IsNull(code);
        }

        [Test]
        public void ParseURL_루트_URL()
        {
            string code = _handler.ParseURL("https://example.com/");
            Assert.IsNull(code);
        }

        [Test]
        public void ParseURL_쿼리_파라미터_있는_URL()
        {
            string code = _handler.ParseURL("https://example.com/c/test123?ref=share");
            Assert.AreEqual("test123", code);
        }

        [Test]
        public void ParseURL_fp_파라미터_추출()
        {
            string code = _handler.ParseURL("https://example.com/c/aB3xK?fp=abc123def456");
            Assert.AreEqual("aB3xK", code);
            Assert.AreEqual("abc123def456", _handler.LastParsedFp);
        }

        [Test]
        public void ParseURL_fp와_다른_쿼리_파라미터_혼합()
        {
            string code = _handler.ParseURL("https://example.com/c/aB3xK?ref=share&fp=xyz789&utm=test");
            Assert.AreEqual("aB3xK", code);
            Assert.AreEqual("xyz789", _handler.LastParsedFp);
        }

        [Test]
        public void ParseURL_fp_없으면_LastParsedFp_null()
        {
            _handler.ParseURL("https://example.com/c/test123?ref=share");
            Assert.IsNull(_handler.LastParsedFp);
        }

        [Test]
        public void ParseURL_fp_빈값이면_무시()
        {
            _handler.ParseURL("https://example.com/c/test123?fp=&ref=share");
            Assert.IsNull(_handler.LastParsedFp);
        }

        [Test]
        public void ParseURL_호출마다_LastParsedFp_초기화()
        {
            // 첫 호출: fp 있음
            _handler.ParseURL("https://example.com/c/aB3xK?fp=first");
            Assert.AreEqual("first", _handler.LastParsedFp);

            // 두 번째 호출: fp 없음 - 이전 값이 남아있으면 안 됨
            _handler.ParseURL("https://example.com/c/xYz12");
            Assert.IsNull(_handler.LastParsedFp);
        }

        [Test]
        public void ParseURL_빈_문자열()
        {
            string code = _handler.ParseURL("");
            Assert.IsNull(code);
        }

        [Test]
        public void ParseURL_null()
        {
            string code = _handler.ParseURL(null);
            Assert.IsNull(code);
        }
    }
}
