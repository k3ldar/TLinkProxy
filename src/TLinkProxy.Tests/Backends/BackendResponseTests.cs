using TLinkProxy.Clients;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;

namespace TLinkProxy.Tests.Backends
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class BackendResponseTests
    {
        [TestMethod]
        public void Constructor_ValidMessage_ReturnsBackendResponse()
        {
            using HttpResponseMessage message = new(HttpStatusCode.OK);

            using BackendResponse sut = new(message);

            Assert.IsNotNull(sut);
        }

        [TestMethod]
        public void Constructor_NullMessage_ThrowsArgumentNullException()
        {
            ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() => new BackendResponse(null));

            Assert.AreEqual("message", exception.ParamName);
        }

        [TestMethod]
        public void Message_ValidMessage_ReturnsSameInstance()
        {
            using HttpResponseMessage message = new(HttpStatusCode.OK);
            using BackendResponse sut = new(message);

            Assert.AreSame(message, sut.Message);
        }

        [TestMethod]
        public void StatusCode_OkMessage_ReturnsOk()
        {
            using HttpResponseMessage message = new(HttpStatusCode.OK);
            using BackendResponse sut = new(message);

            Assert.AreEqual(HttpStatusCode.OK, sut.StatusCode);
        }

        [TestMethod]
        public void StatusCode_NonSuccessMessage_ReturnsThatCode()
        {
            using HttpResponseMessage message = new(HttpStatusCode.BadGateway);
            using BackendResponse sut = new(message);

            Assert.AreEqual(HttpStatusCode.BadGateway, sut.StatusCode);
        }

        [TestMethod]
        public void Headers_MessageWithHeader_ReturnsHeadersCollection()
        {
            using HttpResponseMessage message = new(HttpStatusCode.OK);
            message.Headers.Add("X-Test", "value");
            using BackendResponse sut = new(message);

            HttpResponseHeaders headers = sut.Headers;

            Assert.IsNotNull(headers);
            Assert.AreEqual("value", headers.GetValues("X-Test").Single());
        }

        [TestMethod]
        public void Content_MessageWithContent_ReturnsContent()
        {
            using HttpResponseMessage message = new(HttpStatusCode.OK)
            {
                Content = new StringContent("body"),
            };
            using BackendResponse sut = new(message);

            Assert.IsNotNull(sut.Content);
            Assert.AreEqual("body", sut.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        }

        [TestMethod]
        public void Content_DefaultMessage_ReturnsMessagesContent()
        {
            using HttpResponseMessage message = new(HttpStatusCode.OK);
            using BackendResponse sut = new(message);

            Assert.AreSame(message.Content, sut.Content);
        }

        [TestMethod]
        public void Dispose_MessageWithDisposableContent_DisposesContent()
        {
            TrackingHttpContent content = new("body");
            HttpResponseMessage message = new(HttpStatusCode.OK) { Content = content };
            BackendResponse sut = new(message);

            sut.Dispose();

            Assert.IsTrue(content.DisposeCalled);
        }

        [TestMethod]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            using HttpResponseMessage message = new(HttpStatusCode.OK);
            BackendResponse sut = new(message);

            sut.Dispose();
            sut.Dispose();
        }

        [TestMethod]
        public void Dispose_MessageAlreadyDisposed_DoesNotThrow()
        {
            HttpResponseMessage message = new(HttpStatusCode.OK);
            message.Dispose();
            BackendResponse sut = new(message);

            sut.Dispose();
        }

        [TestMethod]
        public void StatusCode_NoContentMessage_ReturnsNoContent()
        {
            using HttpResponseMessage message = new(HttpStatusCode.NoContent);
            using BackendResponse sut = new(message);

            Assert.AreEqual(HttpStatusCode.NoContent, sut.StatusCode);
        }

        private sealed class TrackingHttpContent : HttpContent
        {
            private readonly string _content;

            public TrackingHttpContent(string content)
            {
                _content = content;
            }

            public bool DisposeCalled { get; private set; }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(_content);
                return stream.WriteAsync(bytes).AsTask();
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _content.Length;
                return true;
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCalled = true;
                base.Dispose(disposing);
            }
        }
    }
}
