namespace Vchd.Permission.Api.Services;

using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Vchd.Permission.Api.Data;
using Vchd.Permission.Api.Entities;
using System.Security.Permissions;

public class TelegramBotService
{
    private readonly ILogger<TelegramBotService> _logger;
    private readonly IServiceProvider _provider;
    private readonly IConfiguration _config;
    private TelegramBotClient _botClient;
    private readonly long _groupChatId;

    public TelegramBotService(ILogger<TelegramBotService> logger, IServiceProvider provider, IConfiguration config)
    {
        _logger = logger;
        _provider = provider;
        _config = config;

        var botToken = _config["TelegramBot:Token"];
        _botClient = new TelegramBotClient(botToken);
        _groupChatId = long.Parse(_config["TelegramBot:GroupChatId"]);

    }

    public async Task StartAsync()
    {
        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync
        );
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Type == UpdateType.CallbackQuery)
        {
            var callback = update.CallbackQuery;
            var parts = callback.Data.Split(":");
            if (parts.Length == 2 && int.TryParse(parts[1], out var permissionId))
            {
                var approved = parts[0] == "approve";

                using var scope = _provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var permission = await db.Permissions.FindAsync(permissionId);

                if (permission != null && permission.Status == EPermissionStatus.Pending)
                {
                    permission.Status = approved ? EPermissionStatus.Approved : EPermissionStatus.Rejected;
                    await db.SaveChangesAsync();

                    await bot.SendTextMessageAsync(callback.Message.Chat.Id, $"Ruxsat №{permission.Id} {(approved ? "tasdiqlandi ✅" : "bekor qilindi ❌")}.");

                    if (approved)
                    {
                        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tashkent");
                        var permissionFrom = TimeZoneInfo.ConvertTime(permission.FromAt, tz);
                        var permissionUntil = TimeZoneInfo.ConvertTime(permission.UntillAt, tz);

                        var htmlMessage =
                            $"✅ <b>YANGI TASDIQLANGAN RUXSAT</b>\n" +
                            $"👤 <b>Xodim:</b> {permission.FullName}\n" +
                            $"📝 <b>Sabab:</b> {permission.Description}\n" +
                            $"🕒 <b>Vaqt:</b> {permissionFrom} - {permissionUntil}";

                        await bot.SendTextMessageAsync(
                            chatId: _groupChatId,
                            text: htmlMessage,
                            parseMode: ParseMode.Html
                        );

                    }
                }
            }
        }

    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        _logger.LogError(ex, "Telegram bot error");
        return Task.CompletedTask;
    }

    public async Task SendPermissionRequestAsync(Permission permission, long chatId)
    {
        var buttons = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("✅ Tasdiqlash", $"approve:{permission.Id}"),
            InlineKeyboardButton.WithCallbackData("❌ Bekor Qilish", $"reject:{permission.Id}")
        });
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tashkent");
        var permissionFrom = TimeZoneInfo.ConvertTime(permission.FromAt, tz);
        var permissionUntil = TimeZoneInfo.ConvertTime(permission.UntillAt, tz);
        var text = $"YANGI RUXSAT\nXodim: {permission.FullName}\nSabab: {permission.Description}\n {permissionFrom} dan {permissionUntil} gacha";
        await _botClient.SendTextMessageAsync(chatId, text, replyMarkup: buttons);
    }
}