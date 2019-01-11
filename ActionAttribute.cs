using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimerAndClock
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ActionAttribute : Attribute
    {
        public string ActionName { get; private set; }

        public ActionAttribute(string actionName)
        {
            ActionName = actionName;
        }
    }
}
