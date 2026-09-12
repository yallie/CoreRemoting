using CoreRemoting.Channels;
using CoreRemoting.Channels.Null;

namespace CoreRemoting.Tests;

public class SessionResumeTestsNullChannel : SessionResumeTests
{
    protected override IServerChannel ServerChannel => new NullServerChannel();

    protected override IClientChannel ClientChannel => new NullClientChannel();

    public class SessionResumeTestsNullNoEncryption : SessionResumeTestsNullChannel
    {
        protected override bool MessageEncryption => false;

        protected override bool AuthenticationRequiredForResumeTests => false;
    }
}
