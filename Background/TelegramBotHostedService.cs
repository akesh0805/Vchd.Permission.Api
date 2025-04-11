using Vchd.Permission.Api.Services;

namespace Vchd.Permission.api.Background;

public class TelegramBotHostedService : BackgroundService
{
    private readonly TelegramBotService _botService;

    public TelegramBotHostedService(TelegramBotService botService)
    {
        _botService = botService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _botService.StartAsync();
    }
}