using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using CoreRemoting.RemoteDelegates;

namespace CoreRemoting
{
    public class ServiceProxy<TServiceInterface> : IInterceptor, IServiceProxy
    {
        private readonly string _serviceName;
        private RemotingClient _client;
        
        /// <summary>
        /// Creates a new instance of the ServiceProxy class.
        /// </summary>
        /// <param name="client">CoreRemoting client to be used for client/server communication</param>
        public ServiceProxy(RemotingClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            
            var serviceInterfaceType = typeof(TServiceInterface);
            _serviceName = serviceInterfaceType.FullName;
            
            var generator = new ProxyGenerator();
            Interface =
                (TServiceInterface) generator.CreateInterfaceProxyWithoutTarget(typeof (TServiceInterface), this);
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~ServiceProxy()
        {
            ((IServiceProxy)this).Shutdown();
        }

        /// <summary>
        /// Shutdown service proxy and free resources.
        /// </summary>
        void IServiceProxy.Shutdown()
        {
            if (_client != null)
            {
                _client.ClientDelegateRegistry.UnregisterClientDelegatesOfServiceProxy(this);
                _client = null;
            }
            
            GC.SuppressFinalize(this);
        }

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
        private TServiceInterface Interface { get; set; }

        void IInterceptor.Intercept(IInvocation invocation)
        {
           var method = invocation.Method;

           if (method == null)
               throw new RemotingException(
                   $"No match was found for method {invocation.Method.Name}.");
           
           var oneWay = method.GetCustomAttribute<OneWayAttribute>() != null;
                
            if (oneWay && method.ReturnType != typeof(void))
                throw new NotSupportedException("OneWay methods must not have a return type.");
                
            var arguments = MapDelegateArguments(invocation);
                
            var remoteMethodCallMessage =
                _client.MethodCallMessageBuilder.BuildMethodCallMessage(
                    remoteServiceName: _serviceName,
                    targetMethod: method,
                    args: arguments);

            List<Type> knownTypes = null;
                
            if (_client.Serializer.NeedsKnownTypes)
                knownTypes = _client.KnownTypeProvider.GetKnownTypesByTypeList(new[] { method.DeclaringType });

            var clientRpcContext = _client.InvokeRemoteMethod(remoteMethodCallMessage, oneWay, knownTypes);

            if (clientRpcContext.Error)
            {
                if (clientRpcContext.RemoteException == null)
                    throw new RemoteInvocationException();
                
                throw clientRpcContext.RemoteException;
            }

            var resultMessage = clientRpcContext.ResultMessage;

            if (resultMessage == null)
            {
                invocation.ReturnValue = null;
                return;
            }

            var parameterInfos = method.GetParameters();
                
            foreach (var outParameterValue in resultMessage.OutParameters)
            {
                var parameterInfo =
                    parameterInfos.First(p => p.Name == outParameterValue.ParameterName);

                invocation.Arguments[parameterInfo.Position] =
                    outParameterValue.IsOutValueNull
                        ? null
                        : outParameterValue.OutValue;
            }
                        
            invocation.ReturnValue = resultMessage.IsReturnValueNull ? null : resultMessage.ReturnValue;
            
            CallContext.RestoreFromSnapshot(resultMessage.CallContextSnapshot);
        }
        
        private object[] MapDelegateArguments(IInvocation invocation)
        {
            var arguments =
                invocation.Arguments.Select(argument =>
                {
                    var type = argument?.GetType();

                    if (type == null || !typeof(Delegate).IsAssignableFrom(type)) 
                        return argument;
                    
                    var delegateReturnType = type.GetMethod("Invoke")?.ReturnType;

                    if (delegateReturnType != typeof(void))
                        throw new NotSupportedException("Only void delegates are supported.");
                        
                    var remoteDelegateInfo =
                        new RemoteDelegateInfo(
                            handlerKey: _client.ClientDelegateRegistry.RegisterClientDelegate((Delegate)argument, this),
                            delegateTypeName: type.FullName);

                    return remoteDelegateInfo;

                }).ToArray();
            return arguments;
        }
    }
}