using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doppler.BillingUser.Weather;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Doppler.BillingUser.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly WeatherForecastService _service;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, WeatherForecastService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get() => _service.GetForecasts();
    }
}
