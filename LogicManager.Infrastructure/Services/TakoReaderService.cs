﻿using LogicManager.Domain.Entities;
using LogicManager.Infrastructure.Interfaces;
using LogicManager.Shared.DTOs;
using LogicManager.Shared.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LogicManager.Infrastructure.Services;

public class TakoReaderService : ITakoReaderService
{
    public readonly IConfiguration _configuration;
    private readonly string _takoConnectionString = ""; // URL düzeltildi
    private readonly HttpClient _httpClient;
    private readonly LoggerHelper _logService;
    private readonly ILogger<TakoReaderService> _logger;

    public TakoReaderService(HttpClient httpClient, IConfiguration configuration, LoggerHelper logService, ILogger<TakoReaderService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _takoConnectionString = _configuration.GetConnectionString("TakoConnection")!;// "!"   işareti null gelemeyecegini belirtiyor
        _logService = logService;
        _logger = logger;

    }

    public async Task<int> ReadTakoPulseAsync()
    {
        try
        {
            // HTTP GET isteği
            var request = new HttpRequestMessage(HttpMethod.Get, _takoConnectionString);

            // Sadece Accept başlığı ekleniyor (GET isteğinde Content-Type gerekmez)
            request.Headers.TryAddWithoutValidation("Accept", "vdn.dac.v1");
            request.Headers.TryAddWithoutValidation("Content-Type", "application/json");

            // İstek gönderiliyor
            var response = await _httpClient.SendAsync(request);


            if (response.IsSuccessStatusCode)
            {
                // JSON yanıtını string olarak al
                var jsonResponse = await response.Content.ReadAsStringAsync();


                // JSON'u DiTakoDataInfo nesnesine dönüştür
                var ioData = JsonSerializer.Deserialize<TakoData>(jsonResponse);

                if (ioData?.Io == null || ioData.Io.Di == null || !ioData.Io.Di.Any())
                {
                    throw new Exception("Io veya Di listesi boş veya null.");
                }

                /*var diStatus =Convert.ToInt32( ioData?.Io?.Di?[0]?.DiStatus);*/
                var diStatus = ioData?.Io?.Di?.FirstOrDefault()?.DiStatus ?? 0;


                ////// RabbitMQ'ya gönder
                ////RabbitMQHelper.SendMessageToExchange(RabbitMQConstants.TakoReadExchangeName, diStatus);
                ////Console.WriteLine("Tako datası okundu.", diStatus);
                return diStatus;
            }
            else
            {
                throw new Exception($"İstek başarısız oldu. Durum kodu: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Tako servisinden veri alınamadı.");

            await _logService.ErrorSendLogAsync(new ErrorLogDto
            {
                MessageSource = "LogicManager",
                MessageContent = "Tako dan veri alma servisine ulaşılamıyor...",
                MessageType = LogType.Error.ToString(),
                DateTime = DateTime.Now,
                ErrorType = LogType.Error.ToString(),
                HardwareIP = "10.3.156.224"
            });

            throw new Exception($"Bir hata oluştu: {ex.Message}", ex);
        }

    }


    public Task<bool> ReadDoorStatusAsync()
    {
        throw new NotImplementedException();
    }

    public Task<double> ReadSpeedStatusAsync()
    {
        throw new NotImplementedException();
    }

    public Task<double> ReadTakoValueAsync()
    {
        throw new NotImplementedException();
    }




}
