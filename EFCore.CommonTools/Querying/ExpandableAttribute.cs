using System;

#if EF_CORE
namespace EntityFrameworkCore.CommonTools
#elif EF_6
namespace EntityFramework.CommonTools
#else
namespace System.Linq.CommonTools
#endif
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ExpandableAttribute : Attribute
    {
    }
}
