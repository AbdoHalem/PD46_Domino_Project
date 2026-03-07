using System;

namespace Connection.Engine.Router
{
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