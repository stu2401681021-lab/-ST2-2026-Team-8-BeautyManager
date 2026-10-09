using BeautyManager.Data;

namespace BeautyManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();


            var connectionString = builder.Configuration.GetConnectionString("BeautyDb")
                ?? "Data Source=App_Data/beauty.db";
            var dataSource = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString).DataSource;
            if (!Path.IsPathRooted(dataSource))
            {
                connectionString = $"Data Source={Path.Combine(builder.Environment.ContentRootPath, dataSource)}";
            }


            builder.Services.AddSingleton<IDatabase>(_ => Database.Initialize(connectionString));



            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }


            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Clients}/{action=Create}/{id?}");


            app.Services.GetRequiredService<IDatabase>();

            app.Run();
        }
    }
}
