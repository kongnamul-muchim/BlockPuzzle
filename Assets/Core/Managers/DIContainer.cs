using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlockPuzzle.Core.Managers
{
    public enum ServiceLifetime
    {
        Transient,
        Scoped,
        Singleton
    }

    public interface IDIContainer
    {
        void Register<TInterface, TImpl>(ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TImpl : TInterface;

        void Register<TImpl>(ServiceLifetime lifetime = ServiceLifetime.Transient);

        void RegisterInstance<T>(T instance, ServiceLifetime lifetime = ServiceLifetime.Singleton);

        T Resolve<T>();

        IDIContainer CreateScope();

        bool IsRegistered<T>();
    }

    public class DIContainer : IDIContainer
    {
        private class ServiceDescriptor
        {
            public Type ImplementationType { get; }
            public ServiceLifetime Lifetime { get; }
            public object Instance { get; set; }
            public bool IsInstanceRegistered { get; }

            public ServiceDescriptor(Type implementationType, ServiceLifetime lifetime)
            {
                ImplementationType = implementationType;
                Lifetime = lifetime;
                IsInstanceRegistered = false;
                Instance = null;
            }

            public ServiceDescriptor(object instance, ServiceLifetime lifetime)
            {
                ImplementationType = instance.GetType();
                Lifetime = lifetime;
                IsInstanceRegistered = true;
                Instance = instance;
            }
        }

        private readonly Dictionary<Type, ServiceDescriptor> _registrations = new();
        private readonly DIContainer _parent;

        public DIContainer() { }

        private DIContainer(DIContainer parent)
        {
            _parent = parent;
        }

        public void Register<TInterface, TImpl>(ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TImpl : TInterface
        {
            Type interfaceType = typeof(TInterface);
            Type implType = typeof(TImpl);

            if (_registrations.ContainsKey(interfaceType))
                throw new InvalidOperationException($"Service {interfaceType.Name} is already registered.");

            _registrations[interfaceType] = new ServiceDescriptor(implType, lifetime);
        }

        public void Register<TImpl>(ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            Type type = typeof(TImpl);

            if (_registrations.ContainsKey(type))
                throw new InvalidOperationException($"Service {type.Name} is already registered.");

            _registrations[type] = new ServiceDescriptor(type, lifetime);
        }

        public void RegisterInstance<T>(T instance, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            Type type = typeof(T);

            if (_registrations.ContainsKey(type))
                throw new InvalidOperationException($"Service {type.Name} is already registered.");

            _registrations[type] = new ServiceDescriptor(instance, lifetime);
        }

        public T Resolve<T>()
        {
            return (T)ResolveInternal(typeof(T));
        }

        private object ResolveInternal(Type type)
        {
            // Try current container
            if (_registrations.TryGetValue(type, out ServiceDescriptor descriptor))
            {
                return ResolveFromDescriptor(descriptor, type);
            }

            // Try parent container (for scoped resolution)
            if (_parent != null)
            {
                return _parent.ResolveInternal(type);
            }

            throw new InvalidOperationException($"Service {type.Name} is not registered.");
        }

        private object ResolveFromDescriptor(ServiceDescriptor descriptor, Type serviceType)
        {
            // If instance already exists (singleton or registered instance)
            if (descriptor.Instance != null)
            {
                return descriptor.Instance;
            }

            // If instance-registered but not created yet
            if (descriptor.IsInstanceRegistered)
            {
                return descriptor.Instance;
            }

            // Create new instance
            object instance = CreateInstance(descriptor.ImplementationType);

            // Cache if singleton
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                descriptor.Instance = instance;
            }

            return instance;
        }

        private object CreateInstance(Type type)
        {
            // Find the constructor with the most parameters (greediest constructor)
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            ConstructorInfo selected = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

            if (selected == null)
            {
                throw new InvalidOperationException($"No public constructor found for {type.Name}.");
            }

            ParameterInfo[] parameters = selected.GetParameters();
            object[] args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = ResolveInternal(parameters[i].ParameterType);
            }

            return Activator.CreateInstance(type, args);
        }

        public IDIContainer CreateScope()
        {
            return new DIContainer(this);
        }

        public bool IsRegistered<T>()
        {
            Type type = typeof(T);
            if (_registrations.ContainsKey(type))
                return true;

            if (_parent != null)
                return _parent.IsRegistered<T>();

            return false;
        }
    }
}
