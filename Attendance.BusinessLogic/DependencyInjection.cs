using Attendance.BusinessLogic.Interfaces;
using Attendance.BusinessLogic.Processors;
using Attendance.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Attendance.BusinessLogic
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBusinessLogicDI(this IServiceCollection services)
        {
            services.AddDataAccessDI();
            services.AddSingleton<IMachineProcessor, MachineProcessor>();
            return services;
        }
    }
}
