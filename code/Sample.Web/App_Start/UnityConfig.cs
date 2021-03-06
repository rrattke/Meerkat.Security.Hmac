using System;

using Meerkat.Caching;
using Meerkat.Security;
using Meerkat.Security.Authentication;
using Meerkat.Security.Authentication.Hmac;
using Meerkat.Security.Authorization;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Sample.Web.Services;

namespace Sample.Web
{
    /// <summary>
    /// Specifies the Unity configuration for the main container.
    /// </summary>
    public class UnityConfig
    {
        private static object syslock = new object();
        private static Lazy<IUnityContainer> container = new Lazy<IUnityContainer>(() =>
        {
            return ConfigureContainer();
        });

        private static IUnityContainer c;
        
        /// <summary>
        /// Gets the configured Unity container.
        /// </summary>
        public static IUnityContainer GetConfiguredContainer()
        {
            return Container;
        }

        public static IUnityContainer Container
        {
            get
            {
                if (c != null)
                {
                    return c;
                }

                lock (syslock)
                {
                    return c ?? (c = ConfigureContainer());
                }
            }
            set
            {
                lock (syslock)
                {
                    c = value;
                }
            }
        }

        /// <summary>
        /// Get the service locator from the container
        /// </summary>
        public static IServiceLocator ServiceLocator
        {
            get { return GetConfiguredContainer().Resolve<IServiceLocator>(); }
        }


        public static void RegisterHmacCore(IUnityContainer container)
        {
            // Secret store as singleton
            container.RegisterType<SecretStore>(new ContainerControlledLifetimeManager());
            container.RegisterType<ISecretRepository, SecretStore>();
            container.RegisterType<ISecretStore, SecretStore>();

            container.RegisterType<IMessageRepresentationBuilder, MessageRepresentationBuilder>();

            container.RegisterType<ISignatureCalculator, HmacSignatureCalculator>();
        }

        /// <summary>Registers the type mappings with the Unity container.</summary>
        /// <param name="container">The unity container to configure.</param>
        /// <remarks>There is no need to register concrete types such as controllers or API controllers (unless you want to 
        /// change the defaults), as Unity allows resolving a concrete type even if it was not previously registered.</remarks>
        public static void RegisterTypes(IUnityContainer container)
        {
            RegisterHmacCore(container);

            container.RegisterType<ICache>(
                new InjectionFactory(x => MemoryObjectCacheFactory.Default()));

            container.RegisterType<ISignatureValidator, HmacSignatureValidator>(
                new InjectionConstructor(
                    new ResolvedParameter<ISignatureCalculator>(),
                    new ResolvedParameter<IMessageRepresentationBuilder>(),
                    new ResolvedParameter<ISecretRepository>(),
                    new ResolvedParameter<ICache>(),
                    // Clock drift in minutes
                    new InjectionParameter<int>(10),
                    // Cache duration in minutes
                    new InjectionParameter<int>(10)));

            container.RegisterType<IRequestClaimsProvider, RequestClaimsProvider>(
                new InjectionConstructor(
                   // Names of the user name claim type
                   new InjectionParameter("name")));

            container.RegisterType<IHmacAuthenticator, HmacAuthenticator>(
                new InjectionConstructor(
                    new ResolvedParameter<ISignatureValidator>(),
                    new ResolvedParameter<IRequestClaimsProvider>(),
                    // Names of the user name and role claim types
                    new InjectionParameter<string>("name"),
                    new InjectionParameter<string>("role")));
        }

        private static IUnityContainer ConfigureContainer()
        {
            var container = new UnityContainer();
            new MyUnityServiceLocator(container);

            RegisterTypes(container);
            return container;
        }
    }
}