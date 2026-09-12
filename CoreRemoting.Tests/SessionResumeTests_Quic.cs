using CoreRemoting.Channels;
using CoreRemoting.Channels.Quic;
using Xunit;

namespace CoreRemoting.Tests;

public class SessionResumeTestsQuic : SessionResumeTests
{
    protected override IServerChannel ServerChannel => new QuicServerChannel();

    protected override IClientChannel ClientChannel => new QuicClientChannel();

    public class SessionResumeTestsQuicNoEncryption : SessionResumeTestsQuic
    {
        protected override bool MessageEncryption => false;

        protected override bool AuthenticationRequiredForResumeTests => false;
    }
}
