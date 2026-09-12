using CoreRemoting.Channels;
using CoreRemoting.Channels.NamedPipe;

namespace CoreRemoting.Tests;

public class SessionResumeTestsNamedPipeChannel : SessionResumeTests
{
    protected override IServerChannel ServerChannel => new NamedPipeServerChannel();

    protected override IClientChannel ClientChannel => new NamedPipeClientChannel();

    public class SessionResumeTestsNamedPipeNoEncryption : SessionResumeTestsNamedPipeChannel
    {
        protected override bool MessageEncryption => false;

        protected override bool AuthenticationRequiredForResumeTests => false;
    }
}
