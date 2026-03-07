using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Connection.Engine.Network;
using Connection.Engine.Router;
using Domino.Engine.State;

namespace Connection.Engine.Router
{
    public class MessageRouter
    {
        private readonly GroupManager _groupManager;
        private Func<Type, object> _handlerFactory;

        private readonly Dictionary<string, Func<PlayerConnection, JsonElement, Task>> _routes
            = new(StringComparer.OrdinalIgnoreCase);

        public MessageRouter(GroupManager groupManager)
        {
            _groupManager   = groupManager;
            _handlerFactory = type => Activator.CreateInstance(type, groupManager);
        }

        public void SetHandlerFactory(Func<Type, object> factory)
        {
            _handlerFactory = factory ?? throw new ArgumentNullException(nameof(factory));
            _routes.Clear();
            RegisterRoutesAutomatically();
        }

        private bool _routesRegistered = false;

        private void EnsureRoutes()
        {
            if (_routesRegistered) return;
            _routesRegistered = true;
            RegisterRoutesAutomatically();
        }

        private void RegisterRoutesAutomatically()
        {
            Assembly startupAssembly = Assembly.GetEntryAssembly();

            if (startupAssembly == null)
                throw new InvalidOperationException("[MessageRouter] Could not determine the entry assembly.");

            var handlerClasses = startupAssembly.GetTypes()
                .Where(t => typeof(IMessageHandler).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

            var handlerInstances = new Dictionary<Type, object>();

            foreach (Type classType in handlerClasses)
            {
                var routeMethods = classType
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.GetCustomAttribute<MessageRouteAttribute>() != null);

                foreach (MethodInfo method in routeMethods)
                {
                    var attribute = method.GetCustomAttribute<MessageRouteAttribute>();

                    if (!handlerInstances.TryGetValue(classType, out object handlerInstance))
                    {
                        handlerInstance = _handlerFactory(classType);
                        handlerInstances[classType] = handlerInstance;
                    }

                    var handlerDelegate = (Func<PlayerConnection, JsonElement, Task>)
                        Delegate.CreateDelegate(
                            typeof(Func<PlayerConnection, JsonElement, Task>),
                            handlerInstance,
                            method);

                    _routes[attribute.Action] = handlerDelegate;
                    Console.WriteLine($"[Router] Registered: {attribute.Action} -> {classType.Name}.{method.Name}");
                }
            }

            _routesRegistered = true;
        }

        public async Task RouteMessageAsync(PlayerConnection player, string jsonString)
        {
            EnsureRoutes();

            try
            {
                using JsonDocument doc = JsonDocument.Parse(jsonString);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("Action", out JsonElement actionElement))
                {
                    Console.WriteLine($"[Router] Invalid message from {player.ConnectionId}: Missing 'Action'.");
                    return;
                }

                string action = actionElement.GetString();

                JsonElement payload = root.TryGetProperty("Payload", out JsonElement payloadElement)
                                      ? payloadElement
                                      : default;

                if (_routes.TryGetValue(action, out var handler))
                    await handler(player, payload);
                else
                    Console.WriteLine($"[Router] Unknown route requested: '{action}'");
            }
            catch (JsonException)
            {
                Console.WriteLine($"[Router] Failed to parse JSON from {player.ConnectionId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Router] Error executing route for {player.ConnectionId}: {ex.Message}");
            }
        }
    }
}
