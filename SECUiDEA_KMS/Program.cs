using CryptoManager;
using SECUiDEA_KMS.Middleware;
using SECUiDEA_KMS.Models;
using SECUiDEA_KMS.Models.Settings;
using SECUiDEA_KMS.Repositories;
using SECUiDEA_KMS.Services;
using SECUiDEACryptoManager.Services;
using System.Reflection;
using System.Runtime.Versioning;

namespace SECUiDEA_KMS
{
    [SupportedOSPlatform("windows")]
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpContextAccessor();

            // 1. 설정 파일 추가
            builder.Services.AddSingleton<AppSettingsService>();

            // 2. 마스터 키 서비스 초기화
            builder.Services.AddSingleton<MasterKeyService>();

            // 2-1. 암호화 서비스 (키 없이 먼저 생성, 나중에 동적으로 설정)
            builder.Services.AddSingleton<ICryptoManager>(sp =>
            {
                var masterKeyService = sp.GetRequiredService<MasterKeyService>();

                // MasterKey가 초기화되어 있으면 키 설정, 아니면 키 없이 생성
                if (masterKeyService.IsInitialized)
                {
                    var keyString = Convert.ToHexString(masterKeyService.MasterKey);
                    return new SafetyAES256(keyString);
                }
                else
                {
                    // 키 없이 생성 (나중에 SetKey로 설정)
                    return new SafetyAES256();
                }
            });

            // 2-2. 데이터베이스 설정 서비스
            builder.Services.AddSingleton<DatabaseSetupService>();

            // 3. 데이터 베이스 설정
            builder.Services.AddOptions<MsSqlDbSettings>(Consts.SECUiDEA)
                .Bind(builder.Configuration.GetSection(Consts.Key_DB_SECUiDEA))
                .PostConfigure<IServiceProvider>((settings, sp) =>
                {
                    var masterKeyService = sp.GetRequiredService<MasterKeyService>();

                    // 마스터 키가 초기화되어 있을 때만 복호화 시도
                    if (masterKeyService.IsInitialized)
                    {
                        var cryptoManager = sp.GetRequiredService<ICryptoManager>();

                        // ICryptoManager에 키가 설정되지 않았다면 설정
                        if (cryptoManager is SafetyAES256 safetyAES && !safetyAES.IsKeySetted)
                        {
                            var keyString = Convert.ToHexString(masterKeyService.MasterKey);
                            safetyAES.SetKey(keyString);
                        }

                        DecryptEncryptedProperties(settings, cryptoManager);
                    }
                });

            // 4. 의존성 주입
            builder.Services.AddSingleton<HttpContextHelper>();

            // 5. Repository 등록 (Scoped - 요청당 인스턴스)
            builder.Services.AddScoped<IClientRepository, ClientRepository>();
            builder.Services.AddScoped<IKeyRepository, KeyRepository>();

            // 6. Service 등록 (Scoped - 요청당 인스턴스)
            builder.Services.AddScoped<ClientService>();
            builder.Services.AddScoped<KeyService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // 마스터 키 초기화 확인 미들웨어 (라우팅 후, 인증 전)
            app.UseMasterKeyInitializationCheck();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        private static void DecryptEncryptedProperties(MsSqlDbSettings settings, ICryptoManager cryptoManager)
        {
            foreach (var property in settings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || !property.CanWrite)
                    continue;

                var encryptedAttribute = property.GetCustomAttribute<EncryptedSettingAttribute>();
                if (encryptedAttribute == null)
                    continue;

                if (property.PropertyType != typeof(string))
                {
                    throw new InvalidOperationException($"Property {property.Name} is not a string");
                }

                var value = property.GetValue(settings);
                if (value == null)
                    continue;

                var encryptedValue = value as string;
                if (string.IsNullOrEmpty(encryptedValue))
                    continue;

                try
                {
                    var decryptedValue = cryptoManager.Decrypt(encryptedValue);
                    property.SetValue(settings, decryptedValue);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to decrypt property {property.Name}", ex);
                }
            }
        }
    }
}
