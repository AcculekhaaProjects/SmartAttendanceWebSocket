using Attendance.BusinessLogic;
using Attendance.DataAccess;

namespace Attendance.WS
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddWebSocketDI(this IServiceCollection services)
        {
            services.AddBusinessLogicDI();
            return services;
        }
    }
}
