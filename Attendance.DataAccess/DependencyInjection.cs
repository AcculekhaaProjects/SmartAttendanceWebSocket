using Attendance.DataAccess.DataProcessor;
using Attendance.DataAccess.DataProcessors;
using Attendance.DataAccess.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Attendance.DataAccess
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDataAccessDI(this IServiceCollection services)
        {
            services.AddSingleton<IDbHelper, DbHelper>();
            services.AddSingleton<IMachineDataProcessor, MachineDataProcessor>();
            return services;
        }
    }
}
