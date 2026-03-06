using System;

namespace Connection.Engine.Router
{
    // AttributeTargets.Method ensures another developer can't accidentally put this on a Class or Property.
    // AllowMultiple = false ensures a single method only handles one specific action string.
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class MessageRouteAttribute : Attribute
    {
        public string Action { get; }

        public MessageRouteAttribute(string action)
        {
            Action = action;
        }
    }
}